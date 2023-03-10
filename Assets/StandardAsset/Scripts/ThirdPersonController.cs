using System;
using System.Collections;
using Cinemachine;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;
#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
using UnityEngine.InputSystem;
#endif

/* Note: animations are called via the controller for both the character and capsule using animator null checks
 */

namespace StarterAssets
{
    [RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
    [RequireComponent(typeof(PlayerInput))]
#endif
    public class ThirdPersonController : MonoBehaviour
    {
        [Header("Player")]
        [Tooltip("Move speed of the character in m/s")]
        public float MoveSpeed = 2.0f;

        [Tooltip("Sprint speed of the character in m/s")]
        public float SprintSpeed = 5.335f;

        [Tooltip("How fast the character turns to face movement direction")]
        [Range(0.0f, 0.3f)]
        public float RotationSmoothTime = 0.12f;

        [Tooltip("Acceleration and deceleration")]
        public float SpeedChangeRate = 10.0f;

        public AudioClip LandingAudioClip;
        public AudioClip[] FootstepAudioClips;
        [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

        [Space(10)]
        [Tooltip("The height the player can jump")]
        public float JumpHeight = 1.2f;

        [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
        public float Gravity = -15.0f;

        [Space(10)]
        [Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
        public float JumpTimeout = 0.50f;

        [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
        public float FallTimeout = 0.15f;

        [FormerlySerializedAs("Grounded")]
        [Header("Player Grounded")]
        [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
        private bool m_IsGrounded = true;

        [Tooltip("Useful for rough ground")]
        public float GroundedOffset = -0.14f;

        [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
        public float GroundedRadius = 0.28f;

        [Tooltip("What layers the character uses as ground")]
        public LayerMask GroundLayers;

        [Header("Cinemachine")]
        [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
        public GameObject CinemachineCameraTarget;
        public GameObject normalCamera;
        public GameObject dashCamera;
        [SerializeField]
        private GameObject weaponContainer;

        [Tooltip("How far in degrees can you move the camera up")]
        public float TopClamp = 70.0f;

        [Tooltip("How far in degrees can you move the camera down")]
        public float BottomClamp = -30.0f;

        [Tooltip("Additional degress to override the camera. Useful for fine tuning camera position when locked")]
        public float CameraAngleOverride = 0.0f;

        [Tooltip("For locking the camera position on all axis")]
        public bool LockCameraPosition = false;

        // cinemachine
        private float _cinemachineTargetYaw;
        private float _cinemachineTargetPitch;

        // player
        private float _speed;
        private float _animationBlend;
        private float _targetRotation = 0.0f;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;

        // timeout deltatime
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;

        // animation IDs
        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;

#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
        private PlayerInput _playerInput;
#endif
        private Animator _animator;
        private CharacterController _controller;
        private StarterAssetsInputs _input;
        private GameObject _mainCamera;

        private const float _threshold = 0.01f;

        private bool _hasAnimator;
        public ColliderEventListener[] colliderEventListeners;
        public CinemachineImpulseSource cinemachineImpulseSource;

        private bool IsCurrentDeviceMouse
        {
            get
            {
#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
                return true;
                // return _playerInput.currentControlScheme == "KeyboardMouse";
#else
				return false;
#endif
            }
        }

        public bool IsJumping
        {
            get => _animator.GetBool(_animIDJump);
            set
            {
                _animator.SetBool(_animIDJump, value);

            }
        }

        public bool IsAttacking
        {
            get => _animator.GetBool("isAttacking");
            set
            {
                _animator.SetBool("isAttacking", value);
                if (value)
                {
                    _animator.CrossFade("Attack", ComboNumber == 0 ? 0.0f : 0.1f, 0);
                }
            }
        }

        public bool IsDashing
        {
            get => _animator.GetBool("isDashing");
            set
            {
                _animator.SetBool("isDashing", value);
                if (value)
                {
                    _animator.CrossFade("Dash", 0.0f, 0);
                }
            }
        }

        public bool IsGotAttacked
        {
            get => _animator.GetBool("isGotAttacked");
            set
            {
                _animator.SetBool("isGotAttacked", value);
            }
        }

        public int ComboNumber
        {
            get => _animator.GetInteger("comboNumber");
            set => _animator.SetInteger("comboNumber", value);
        }

        public bool IsHeavyAttacking
        {
            get => _animator.GetBool("isHeavyAttacking");
            set => _animator.SetBool("isHeavyAttacking", value);
        }

        public bool IsInCombo { get; set; }

        public bool IsGrounded
        {
            get => m_IsGrounded;
            set
            {
                if (m_IsGrounded == value) return;
                m_IsGrounded = value;
                OnGrounded(value);
            }
        }

        private void Awake()
        {
            // get a reference to our main camera
            if (_mainCamera == null)
            {
                _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            }

            Array.ForEach(colliderEventListeners, x => x.OnTriggerEnterEvent += OnHitEnemy);
        }

        private void Start()
        {
            _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;
            
            _hasAnimator = TryGetComponent(out _animator);
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<StarterAssetsInputs>();
#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
            _playerInput = GetComponent<PlayerInput>();
#else
			Debug.LogError( "Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
#endif

            AssignAnimationIDs();

            // reset our timeouts on start
            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;
        }

        public void Recover()
        {
            IsGotAttacked = true;
            _controller.enabled = true;
        }

        private void Update()
        {
            _hasAnimator = TryGetComponent(out _animator);

            if (!IsAttacking && !IsDashing)
            {
                JumpAndGravity();
                GroundedCheck();
                Move();
            }
            Attack();
            Dash();
        }

        private void Dash()
        {
            if (_input.dash)
            {
                _input.dash = false;
                if (!IsDashing && !IsAttacking)
                {
                    IsDashing = true;
                    dashCamera.SetActive(true);
                    normalCamera.SetActive(false);
                }
            }
        }

        public void PerformADash()
        {
            _controller.enabled = false;
            weaponContainer.SetActive(true);
            transform.DOMove(transform.position + transform.forward * 10.0f, 0.2f)
                     .SetEase(Ease.InFlash)
                     .OnComplete(() =>
                                 {
                                     weaponContainer.SetActive(false);
                                     _controller.enabled = true;
                                     IsDashing = false;
                                     normalCamera.SetActive(true);
                                     dashCamera.SetActive(false);
                                 });
        }

        private void Attack()
        {
            if (_input.hit == true)
            {
                _input.hit = false;
                OnAttackAction();
            }
        }

        private void OnAttackAction()
        {
            if (IsGrounded && (!IsAttacking || IsInCombo))
            {
                IsAttacking = true;
                ComboNumber++;
                IsInCombo = false;
            }
        }

        private void LateUpdate()
        {
            CameraRotation();
        }

        private void AssignAnimationIDs()
        {
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDJump = Animator.StringToHash("jump");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
        }

        private void GroundedCheck()
        {
            // set sphere position, with offset
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
            IsGrounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);

            // update animator if using character
            if (_hasAnimator)
            {
                _animator.SetBool(_animIDGrounded, IsGrounded);
            }
        }

        private void CameraRotation()
        {
            // if there is an input and camera position is not fixed
            if (_input.look.sqrMagnitude >= _threshold && !LockCameraPosition)
            {
                //Don't multiply mouse input by Time.deltaTime;
                float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

                _cinemachineTargetYaw += _input.look.x * deltaTimeMultiplier;
                _cinemachineTargetPitch += _input.look.y * deltaTimeMultiplier;
            }

            // clamp our rotations so our values are limited 360 degrees
            _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

            // Cinemachine will follow this target
            CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride,
                _cinemachineTargetYaw, 0.0f);
        }

        private void Move()
        {
            // set target speed based on move speed, sprint speed and if sprint is pressed
            float targetSpeed = _input.sprint ? SprintSpeed : MoveSpeed;

            // a simplistic acceleration and deceleration designed to be easy to remove, replace, or iterate upon

            // note: Vector2's == operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            // if there is no input, set the target speed to 0
            if (_input.move == Vector2.zero) targetSpeed = 0.0f;

            // a reference to the players current horizontal velocity
            float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

            float speedOffset = 0.1f;
            float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

            // accelerate or decelerate to target speed
            if (currentHorizontalSpeed < targetSpeed - speedOffset ||
                currentHorizontalSpeed > targetSpeed + speedOffset)
            {
                // creates curved result rather than a linear one giving a more organic speed change
                // note T in Lerp is clamped, so we don't need to clamp our speed
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude,
                    Time.deltaTime * SpeedChangeRate);

                // round speed to 3 decimal places
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = targetSpeed;
            }

            _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
            if (_animationBlend < 0.01f) _animationBlend = 0f;

            // normalise input direction
            Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

            // note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            // if there is a move input rotate player when the player is moving
            if (_input.move != Vector2.zero)
            {
                _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
                                  _mainCamera.transform.eulerAngles.y;
                float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity,
                    RotationSmoothTime);

                // rotate to face input direction relative to camera position
                transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
            }


            Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

            // move the player
            _controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) +
                             new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

            // update animator if using character
            if (_hasAnimator)
            {
                _animator.SetFloat(_animIDSpeed, _animationBlend);
                _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
            }
        }

        private void JumpAndGravity()
        {
            ProcessJumpInput();
            CalculateVerticalVelocity();
        }

        private void ProcessJumpInput()
        {
            if (!_input.jump) return;

            _input.jump = false;

            if (IsGrounded && _verticalVelocity <= 0.0f)
            {
                IsJumping = true;
                _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
            }
            else if (IsJumping)
            {
                IsJumping = false;
                _animator.SetTrigger("doubleJump");
                _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
            }
        }

        private void CalculateVerticalVelocity()
        {
            _animator.SetBool(_animIDFreeFall, _verticalVelocity < 0.0f && !IsGrounded);
            // apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
            if (!IsGrounded)
            {
                if (_verticalVelocity < _terminalVelocity)
                {
                    _verticalVelocity += Gravity * Time.deltaTime;
                }
            }
            else
            {
                if (_verticalVelocity <= 0)
                {
                    _verticalVelocity = -2f;
                    IsJumping = false;
                }
            }
        }

        private void OnGrounded(bool value)
        {
        }

        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }

        private void OnDrawGizmosSelected()
        {
            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

            if (IsGrounded) Gizmos.color = transparentGreen;
            else Gizmos.color = transparentRed;

            // when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
            Gizmos.DrawSphere(
                new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z),
                GroundedRadius);
        }

        private void OnFootstep(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                if (FootstepAudioClips.Length > 0)
                {
                    var index = Random.Range(0, FootstepAudioClips.Length);
                    AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.TransformPoint(_controller.center), FootstepAudioVolume);
                }
            }
        }

        private void OnLand(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                AudioSource.PlayClipAtPoint(LandingAudioClip, transform.TransformPoint(_controller.center), FootstepAudioVolume);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            IsGotAttacked = true;
            IsAttacking = false;
            IsInCombo = false;
            ComboNumber = 0;
            IsHeavyAttacking = false;

            if (UnityEngine.Random.Range(0, 10) == 1)
            {
                _animator.CrossFade("HeavyHitReaction", 0.0f, 0);
                _controller.enabled = false;
            }
            else
            {
                _animator.CrossFade("LightHitReaction", 0.0f, 0);
            }
        }

        private void OnHitEnemy(Collider collider)
        {
            cinemachineImpulseSource.GenerateImpulse();
            StartCoroutine(HitPauseIEnumerator());
        }

        private IEnumerator HitPauseIEnumerator()
        {
            var time = 0.0f;
            Time.timeScale = 0.0f;
            while (true)
            {
                time += Time.unscaledDeltaTime;
                if (time >= 0.2f)
                {
                    Time.timeScale = 1.0f;
                    break;
                }
                yield return new WaitForEndOfFrame();
            }
        }

        private IEnumerator PerformADashIEnumerator()
        {
            var time = 0.0f;
            Time.timeScale = 0.0f;
            while (true)
            {
                Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;
                _controller.Move(targetDirection.normalized * (50 * Time.unscaledDeltaTime));

                time += Time.unscaledDeltaTime;
                if (time >= 0.5f)
                {
                    IsDashing = false;
                    break;
                }
                yield return new WaitForEndOfFrame();
            }
        }
    }
}

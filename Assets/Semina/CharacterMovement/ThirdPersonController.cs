using System;
using System.Collections;
using System.Collections.Generic;
using RPGCharacterAnims.Actions;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

namespace Semina
{
	public class ThirdPersonController : MonoBehaviour
	{
		[SerializeField] private float speed = 3.5f;
		[SerializeField] private float sprintSpeed = 7.0f;
		[SerializeField] private float currentSpeed;
		[SerializeField] private float rotationSpeed = 360.0f;
		[SerializeField] private float jumpSpeed = 2.0f;
		[SerializeField] private Transform playerTransform;
		[SerializeField] private PlayerAnimatorController playerAnimatorController;
		[SerializeField] private Rigidbody rigidBody;
		[SerializeField] private bool isGrounded = true;
		[SerializeField] private SphereCaster sphereCaster;
		[SerializeField] private Transform cameraFocusPoint;
		private Vector3 cameraRotation;

		public bool IsGrounded
		{
			get => isGrounded;
			set
			{
				if (isGrounded == value) return;
				isGrounded = value;
				playerAnimatorController.IsGrounded = isGrounded;
			}
		}

		private void Start()
		{
			currentSpeed = speed;
			cameraRotation = cameraFocusPoint.rotation.eulerAngles;
		}

		public void Jump()
		{
			if (IsGrounded)
			{
				rigidBody.velocity += jumpSpeed * Vector3.up;
				IsGrounded = false;
				playerAnimatorController.Jump();
			}
		}

		public void Move(Vector2 direction)
		{
			var inputSurfaceVelocity = new Vector3(direction.x, 0, direction.y) * currentSpeed;
			MoveCharacter(inputSurfaceVelocity);
			UpdateRunAnimation(inputSurfaceVelocity);
		}

		private void UpdateRunAnimation(Vector3 horizontalVelocity)
		{
			playerAnimatorController.Speed = Mathf.InverseLerp(0.0f, sprintSpeed, horizontalVelocity.magnitude);
		}

		private void Update()
		{
			IsGrounded = sphereCaster.IsIntersecting();
			playerAnimatorController.IsFreeFall = rigidBody.velocity.y < 0f && IsGrounded == false;
		}

		private void MoveCharacter(Vector3 inputSurfaceVelocity)
		{
			var rigidBodyVelocity = CalculateVelocity(inputSurfaceVelocity);
			rigidBody.velocity = rigidBodyVelocity;

			if (inputSurfaceVelocity == Vector3.zero) return;

			transform.rotation = Quaternion.RotateTowards(playerTransform.rotation, CalculateTargetRotation(inputSurfaceVelocity), rotationSpeed * Time.deltaTime);
		}

		private static Quaternion CalculateTargetRotation(Vector3 inputSurfaceVelocity)
		{
			// return GetCameraLookRotation() * Quaternion.LookRotation(inputSurfaceVelocity);
			return Quaternion.LookRotation(inputSurfaceVelocity);
		}

		// private static Quaternion GetCameraLookRotation()
		// {
		// 	var horizontalDirection = Camera.main.transform.forward;
		// 	horizontalDirection.y = 0;
		// 	return Quaternion.LookRotation(horizontalDirection);
		// }

		private Vector3 CalculateVelocity(Vector3 inputSurfaceVelocity)
		{
			// var cameraLookRotation = GetCameraLookRotation();
			// var rigidBodyVelocity = cameraLookRotation * new Vector3(inputSurfaceVelocity.x, 0, inputSurfaceVelocity.z);
			var rigidBodyVelocity = new Vector3(inputSurfaceVelocity.x, 0, inputSurfaceVelocity.z);
			rigidBodyVelocity.y = rigidBody.velocity.y;
			return rigidBodyVelocity;
		}

		public void Sprint(bool isSprinting)
		{
			currentSpeed = isSprinting ? sprintSpeed : speed;
		}

		private void Reset()
		{
			playerTransform = GetComponent<Transform>();
		}

		public void RotateCamera(Vector2 look)
		{
			cameraRotation += new Vector3(-look.y, look.x);
			var quaternion = Quaternion.Euler(cameraRotation);
			cameraFocusPoint.rotation = quaternion;
		}
	}
}

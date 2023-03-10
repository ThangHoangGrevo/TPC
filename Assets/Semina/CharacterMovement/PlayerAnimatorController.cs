using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Semina
{
	public class PlayerAnimatorController : MonoBehaviour
	{
		[SerializeField] private Animator animator;
		[SerializeField] private bool isGrounded;
		[SerializeField] private bool isFreeFall;
		[SerializeField] private float speed;

		private static readonly int IsGroundedHash = Animator.StringToHash("isGrounded");
		private static readonly int IsFreeFallHash = Animator.StringToHash("isFreeFall");
		private static readonly int SpeedHash = Animator.StringToHash("speed");
		private static readonly int JumpHash = Animator.StringToHash("jump");

		public float Speed
		{
			get => speed;
			set
			{
				if (speed == value) return;
				speed = value;
				animator.SetFloat(SpeedHash, Speed);
			}
		}

		public bool IsGrounded
		{
			get => isGrounded;
			set => isGrounded = value;
		}

		public bool IsFreeFall
		{
			get => isFreeFall;
			set => isFreeFall = value;
		}

		private void Update()
		{
			animator.SetBool(IsGroundedHash, IsGrounded);
			animator.SetBool(IsFreeFallHash, IsFreeFall);
			animator.SetFloat(SpeedHash, Speed);
		}
	}
}

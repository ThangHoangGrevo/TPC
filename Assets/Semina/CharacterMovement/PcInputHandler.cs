using System;
using System.Collections;
using System.Collections.Generic;
using StarterAssets;
using UnityEngine;

namespace Semina
{
	public class PcInputHandler : MonoBehaviour
	{
		[SerializeField] private Semina.ThirdPersonController thirdPersonController;
		private Vector3 lastMousePosition = Vector3.zero;

		private void Update()
		{
			if (Input.GetKeyDown(KeyCode.Space))
			{
				thirdPersonController.Jump();
			}

			var currentMoveDirection = GetCurrentMoveDirection();
			thirdPersonController.Move(currentMoveDirection);
			this.thirdPersonController.Sprint(Input.GetKey(KeyCode.LeftShift));

			var currentMouseDeltaPosition = GetCurrentMouseDeltaPosition();
			thirdPersonController.RotateCamera(currentMouseDeltaPosition);
		}

		private static Vector2 GetCurrentMouseDeltaPosition()
		{
			return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
		}

		private static Vector2 GetCurrentMoveDirection()
		{
			return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;
		}

		private void OnApplicationFocus(bool hasFocus)
		{
			if (hasFocus)
			{
				Cursor.lockState = CursorLockMode.Locked;
			}
		}
	}
}

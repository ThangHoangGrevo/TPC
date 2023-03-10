using System;
using System.Collections;
using System.Collections.Generic;
using Semina;
using UnityEngine;

namespace Semina
{
	public class PlayerInputHandler : MonoBehaviour
	{
		[SerializeField] private PlayerAnimatorController playerAnimatorController;
		private void Update()
		{
			if (Input.GetKeyUp(KeyCode.Space))
			{
				playerAnimatorController.Jump();
			}
		}
	}
}

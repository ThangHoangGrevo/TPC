using System;
using System.Collections;
using System.Collections.Generic;
using StarterAssets;
using UnityEngine;

public class CharacterAnimationEventReceiver : MonoBehaviour
{
	[SerializeField]
	private ThirdPersonController controller;
	[SerializeField]
	private GameObject weaponContainer;

	private void OnBeginCombo()
	{
		controller.IsInCombo = true;
		weaponContainer.SetActive(false);
	}

	private void OnEndCombo()
	{
		controller.IsInCombo = false;
		controller.ComboNumber = 0;
	}

	private void OnHitPose()
	{
		weaponContainer.SetActive(true);
	}

	private void OnRecoverFromHit()
	{
		controller.Recover();
	}

	private void OnEndAttack()
	{
		controller.IsAttacking = false;
		controller.IsHeavyAttacking = false;
		Debug.Log("Attack Done");
	}

	private void OnKnockedDown()
	{

	}

	private void OnDashPrepare()
	{
		controller.PerformADash();
	}
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EmptyConditionalState : StateMachineBehaviour
{
	public string variableName;
	public int numberOfAnimations;

	public override void OnStateEnter(Animator animator,
									  AnimatorStateInfo stateInfo,
									  int layerIndex)
	{
		base.OnStateEnter(animator, stateInfo, layerIndex);
		animator.SetInteger(variableName, UnityEngine.Random.Range(0, numberOfAnimations));
	}
}

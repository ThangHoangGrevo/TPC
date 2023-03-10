using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public class OnRandomToNextState : StateMachineBehaviour
{
    [SerializeField] RuntimeAnimatorController animatorController;
    [SerializeField] float crossfadeTime = .1f;
    [SerializeField] List<int> statesNames = new();
    bool isCrossFading;
    int isCrossFadingFromThis;


    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (isCrossFading) return;
        int randomNameHash = statesNames[Random.Range(0,statesNames.Count)];
        if (randomNameHash != stateInfo.shortNameHash)
        {
            animator.CrossFade(randomNameHash, crossfadeTime, layerIndex);
            isCrossFadingFromThis = stateInfo.fullPathHash;
        }

    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (isCrossFadingFromThis == stateInfo.shortNameHash)
        {
            isCrossFading = false;
            isCrossFadingFromThis = -1;
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Collect States")]
    void CollectStates()
    {
			if (!animatorController) return;
			var controller = animatorController as AnimatorController;
			AnimatorStateMachine machine = null;

			foreach (AnimatorControllerLayer layer in controller.layers)
			{
				machine = layer.stateMachine.stateMachines.ToList().Find(childMachine =>
				{
					if (childMachine.stateMachine.behaviours.ToList().Contains(this))
					{
						return true;
					}
					return false;
				}).stateMachine;
				if (machine) break;
			}
			statesNames.Clear();
			if (machine)
			{
				foreach (ChildAnimatorState childAnimatorState in machine.states)
				{
					statesNames.Add(childAnimatorState.state.nameHash);
				}
			}

			EditorUtility.SetDirty(this);
    }

	public void OnValidate()
	{
		CollectStates();
	}
#endif
}

using System;
using UnityEngine;


public class StateEnterBehaviour : StateMachineBehaviour
{
    public event Action<Animator, AnimatorStateInfo, int> OnStateEnterEvent = (_, __, ___) => { };
    public event Action<Animator, AnimatorStateInfo, int> OnStateExitEvent = (_, __, ___) => { };

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        OnStateExitEvent.Invoke(animator, stateInfo, layerIndex);
    }

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        OnStateEnterEvent.Invoke(animator, stateInfo, layerIndex);
    }
}

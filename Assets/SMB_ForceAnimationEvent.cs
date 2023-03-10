using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace WoofTools.StateMachineBehaviours
{
    /// <summary>
    /// This Class ensures that Animation Events are fired even during transitions and on 0 weight layers
    /// </summary>
    public class SMB_ForceAnimationEvent: StateMachineBehaviour
    {
        [SerializeField]public StateEvent[] OnEnterEvents;
        [SerializeField]public StateEvent[] OnUpdateEvents;
        [SerializeField]public StateEvent[] OnExitEvents;

        //ensure OnUnpdateEvents are sorted by order of normalizedTime incase multiple events fire during the same update
        protected virtual void OnEnable()
        {
            if (OnUpdateEvents != null)
                OnUpdateEvents = OnUpdateEvents.OrderBy(e=>e.NormalizedTime).ToArray();
        }

        //the class needs to track the last normalizedTime used so that it can cleanly issue events
        private Dictionary<Animator,float> NormalizedTimePerAnimator = new Dictionary<Animator, float>();

        //remove any Animators that were deleted while in this state and thus weren't cleaned up
        private void CleanDictionary()
        {
            NormalizedTimePerAnimator = NormalizedTimePerAnimator.Where(kvp=>(bool)kvp.Key).ToDictionary(kvp=>kvp.Key,KeyValuePair=>KeyValuePair.Value);
        }
        private float GetNormalizedTime(Animator animator)
        {
            float time=-1;
            if(!NormalizedTimePerAnimator.TryGetValue(animator,out time))
            {
                //if we couldn't find the animator assume its a good time to make sure any null keys are cleaned out
                CleanDictionary();

                //sets time to -1 to ensure any OnUpdateEvents with a time of 0 will fire
                NormalizedTimePerAnimator.Add(animator,time);
            }

            return time;
        }
        private void SetNormalizedTime(Animator animator, AnimatorStateInfo stateInfo)
        {
            NormalizedTimePerAnimator[animator] = stateInfo.normalizedTime;
        }
        private void SetNormalizedTime(Animator animator, float normalizedTime)
        {
            NormalizedTimePerAnimator[animator] = normalizedTime;
        }

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if(animator==null) return;

            SetNormalizedTime(animator,  -1);

            for(int i=0;i<OnEnterEvents.Length;i++)
            {
                OnEnterEvents[i].Invoke(animator);
            }
        }


        public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if(animator==null) return;

            float lastTime = GetNormalizedTime(animator);
            float currentTime = stateInfo.normalizedTime;

            //the variable name technically not accurate but more concise than "has passed the first animation iteration"
            // An animation that doesn't have an exit transition and doesn't loop will pause on the last frame, but the
            // normalized time will still rise past 1.0f. that distinction is important in this function
            bool looping = lastTime>1;

            if(looping)
            {
                //if the state doesn't loop and is considered looping then that means
                // this function should do nothing (all OnUpdateEvent should have already been called)
                if(!stateInfo.loop) return;

                //modulo the times in a space for the events' times
                lastTime = lastTime%1 - 1f;// this will make lastTime negative, which is important for the equality checks in the for loop
                currentTime = currentTime%1;
            }


            for(int i=0;i<OnUpdateEvents.Length;i++)
            {
                float eventTime = OnUpdateEvents[i].NormalizedTime;

                //if the event shouldn't fire in a looped animation, skip it
                if(looping && !OnUpdateEvents[i].RepeatOnLoop) continue;

                //if lastTime is greater than the eventTime then its assumed to have already been fired in some previous frame, so skip it
                if(lastTime>eventTime) continue;

                //if currentTime is less than the eventTime then the event triggers later, skip
                if(currentTime<eventTime) continue;

                OnUpdateEvents[i].Invoke(animator);
            }

            SetNormalizedTime(animator,  stateInfo);
        }



        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if(animator==null) return;

            #region call UpdateEvents that need to be fired regardless of exiting
            float lastTime = GetNormalizedTime(animator);
            bool looping = lastTime>1;

            if(looping)
                lastTime = (!stateInfo.loop)?2:lastTime%1 - 1f;

            for(int i=0;i<OnUpdateEvents.Length;i++)
            {
                if(!OnUpdateEvents[i].ForceCallOnExit) continue;
                if(looping && !OnUpdateEvents[i].RepeatOnLoop) continue;
                if(lastTime>OnUpdateEvents[i].NormalizedTime) continue;

                OnUpdateEvents[i].Invoke(animator);
            }
            #endregion


            for(int i=0;i<OnExitEvents.Length;i++)
            {
                OnExitEvents[i].Invoke(animator);
            }

            //we are leaving the state so this animator is no longer being used here, thus the state machine behaviour no longer needs to track it
            NormalizedTimePerAnimator.Remove(animator);
        }

        [System.Serializable]public class StateEvent
        {
            public enum ArgumentType{none, boolType, floatType,intType,stringType,UnityObjectType}



            [Tooltip("Should the event be fired? Useful for disabling events for debugging.")]
            [SerializeField]protected bool m_enabled = true;
            [Tooltip("At what point in an StateUpdate should the event fire, only used for OnUpdateEvents.")]
            [SerializeField][Range(0f,1f)]protected float m_normalizedTime = 0.5f;
            [Tooltip("Should the event be called regardless if the state is being exited? only used for OnUpdateEvents")]
            [SerializeField]protected bool m_ForceCallOnExit = true;
            [Tooltip("Should the event be called in each loop or just on the first iteration?")]
            [SerializeField]protected bool m_repeatOnLoop = true;
            [Tooltip("Which function to call when sending the event")]
            [SerializeField]protected string m_FunctionName = string.Empty;
            [Tooltip("the type of Parameter to send")]
            [SerializeField]protected ArgumentType m_parameterType = ArgumentType.stringType;
            [SerializeField]protected bool m_boolParameter = false;
            [SerializeField]protected float m_floatParameter = 0f;
            [SerializeField]protected int m_intParameter = 0;
            [SerializeField]protected string m_stringParameter = string.Empty;
            [SerializeField]protected UnityEngine.Object m_objectParameter = null;

            public float NormalizedTime{get{return m_normalizedTime;}}
            public bool RepeatOnLoop{get{return m_repeatOnLoop;}}
            public bool ForceCallOnExit{get{return m_ForceCallOnExit;}}
            public string FunctionName{get{return m_FunctionName;}}
            public ArgumentType ParameterType{get{return m_parameterType;}}
            public bool BoolParameter{get{return m_boolParameter;}}
            public float FloatParameter{get{return m_floatParameter;}}
            public int IntParameter{get{return m_intParameter;}}
            public string StringParameter{get{return m_stringParameter;}}
            public UnityEngine.Object ObjectParameter{get{return m_objectParameter;}}

            private static SendMessageOptions sendType = SendMessageOptions.DontRequireReceiver;

            public void Invoke(Animator animator)
            {
                if(!m_enabled) return;
                if(animator == null) return;
                if(string.IsNullOrEmpty(m_FunctionName)) return;

                switch(m_parameterType)
                {
                    case ArgumentType.boolType:
                        animator.SendMessage(m_FunctionName,m_boolParameter,sendType);
                        break;
                    case ArgumentType.floatType:
                        animator.SendMessage(m_FunctionName,m_floatParameter,sendType);
                        break;
                    case ArgumentType.intType:
                        animator.SendMessage(m_FunctionName,m_intParameter,sendType);
                        break;
                    case ArgumentType.stringType:
                        animator.SendMessage(m_FunctionName,m_stringParameter,sendType);
                        break;
                    case ArgumentType.UnityObjectType:
                        animator.SendMessage(m_FunctionName,m_objectParameter,sendType);
                        break;
                    case ArgumentType.none:
                    default:
                        animator.SendMessage(m_FunctionName,sendType);
                        break;
                }
            }
        }
    }
}

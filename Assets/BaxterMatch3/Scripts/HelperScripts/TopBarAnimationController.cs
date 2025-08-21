using System;
using UnityEngine;

namespace HelperScripts
{
    public class TopBarAnimationController : MonoBehaviour
    {
        public static Action<TopBarAnimationState> OnTopBarStateChange;

        [SerializeField] private Animator animator;

        private static TopBarAnimationState _state;

        private readonly int _happyAnimationHash = Animator.StringToHash("Happy");
        private readonly int _worriedAnimationHash = Animator.StringToHash("Worried");
        private readonly int _worriedAnimationHash2 = Animator.StringToHash("worried");


        private void ChangeState(TopBarAnimationState animationState)
        {
            Debug.LogError("ChangeState  " + animationState);
            _state = animationState;
            switch (_state)
            {
                case TopBarAnimationState.Idle:
                    animator.SetBool(_happyAnimationHash, false);
                    animator.SetBool(_worriedAnimationHash, false);
                    animator.Play("idle");
                    break;
                case TopBarAnimationState.Win:
                    animator.Play("chear up1");
                    animator.SetBool(_worriedAnimationHash, false);
                    animator.SetBool(_happyAnimationHash, true);
                    break;
                case TopBarAnimationState.Happy:
                    animator.Play("chear up1");
                    animator.SetBool(_happyAnimationHash, true);
                    break;
                case TopBarAnimationState.Worried:
                    animator.SetBool(_happyAnimationHash, false);
                    animator.SetBool(_worriedAnimationHash, true);
                    animator.SetTrigger(_worriedAnimationHash2);
                    break;
            }
        }


        private void OnEnable()
        {
            animator.SetBool(_happyAnimationHash, false);
            animator.SetBool(_worriedAnimationHash, false);
        }

        // For Top Bar Animations
        /*private void Awake()
        {
            OnTopBarStateChange = ChangeState;
        }

        private void OnDestroy()
        {
            OnTopBarStateChange = null;
        }*/

        /*public void IdleLoopDone()
        {
            animator.SetBool("IdleIndex", !animator.GetBool("IdleIndex"));
        }*/
    }

    public enum TopBarAnimationState
    {
        Idle,
        Happy,
        Win,
        Worried
    }
}
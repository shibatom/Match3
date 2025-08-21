using UnityEngine;

namespace HelperScripts
{
    public class ChangeIdleLoop : StateMachineBehaviour
    {
        private static int _idleIndex = 0;
        private static bool _randomFactor = false;

        // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
        override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (stateInfo.IsName("chear up1"))
            {
                animator.SetBool("Happy", false);
            }
            else if (stateInfo.IsName("look at cam idle3"))
            {
                _randomFactor = !_randomFactor;
                animator.SetBool("RandomFactor", _randomFactor);
            }
        }

        // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
        //override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        //{}

        // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
        override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (stateInfo.IsName("look at cam idle3") && _randomFactor)
                return;
            int temp = Random.Range(0, 8);
            if (_idleIndex == temp)
                _idleIndex++;
            else
                _idleIndex = temp;

            if (_idleIndex > 7)
                _idleIndex = 0;
            animator.SetInteger("IdleIndex", _idleIndex);
        }

        // OnStateMove is called right after Animator.OnAnimatorMove()
        //override public void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        //{
        //    // Implement code that processes and affects root motion
        //}

        // OnStateIK is called right after Animator.OnAnimatorIK()
        //override public void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        //{
        //    // Implement code that sets up animation IK (inverse kinematics)
        //}
    }
}
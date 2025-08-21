using UnityEngine;
using Internal.Scripts.Items;

namespace HelperScripts
{
    public class FlyBehaviour : StateMachineBehaviour
    {
        // Name of the layer to change to
        [SerializeField] private string layerName = "TargetLayer";
        private MeshRenderer skeletonRenderer;
        private ChopperFly _chopperComp;

        // Called when the animation state is entered
        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            // Get the SkeletonAnimation component if it hasn't been set
            if (_chopperComp == null)
            {
                _chopperComp = animator.GetComponentInParent<ChopperFly>();
            }

            if (_chopperComp != null)
            {
                _chopperComp.SetupFlightEffects();
            }
            else
            {
                Debug.LogWarning("ChopperFly component not found on object.");
            }
        }

        // Called when exiting the animation state
        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            // Reset to a default layer or handle any additional logic if necessary
        }
    }
}
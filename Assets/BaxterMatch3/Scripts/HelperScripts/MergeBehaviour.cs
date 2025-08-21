using UnityEngine;
using Spine.Unity;
using UnityEngine.Animations;
using Internal.Scripts.Items;

namespace HelperScripts
{
    public class MergeBehaviour : StateMachineBehaviour
    {
        // Name of the layer to change to
        [SerializeField] private string layerName = "TargetLayer";
        private MeshRenderer skeletonRenderer;
        private DiscoBallItem _comp;

        // Called when the animation state is entered
        public override void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            // Get the SkeletonAnimation component if it hasn't been set
            if (_comp == null)
            {
                Debug.Log($"multicolor parrent {animator.transform.parent} and {animator.transform.parent.parent}");
                _comp = animator.transform.parent.GetComponentInParent<DiscoBallItem>();
            }

            if (_comp != null)
            {
                _comp.SetUpMergeEffect();
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
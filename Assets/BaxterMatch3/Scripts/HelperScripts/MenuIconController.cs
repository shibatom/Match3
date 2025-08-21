using DG.Tweening;
using UnityEngine;


namespace HelperScripts
{
    public class MenuIconController : MonoBehaviour
    {
        [SerializeField] private Transform[] positionSetTransforms;
        [SerializeField] private Animator animator;

        private Vector3 _highlightedPosition;
        private Vector3 _normalPosition;
        private Vector3 _movedAsideLeftPosition;
        private Vector3 _movedAsideRightPosition;

        private void Start()
        {
            _highlightedPosition = positionSetTransforms[0].position;
            _normalPosition = positionSetTransforms[1].position;
            _movedAsideLeftPosition = positionSetTransforms[2].position;
            _movedAsideRightPosition = positionSetTransforms[3].position;
        }

        public void SetIconStatus(IconStatus iconStatus)
        {
            switch (iconStatus)
            {
                case IconStatus.Highlighted:
                    transform.DOMove(_highlightedPosition, 0.2f).OnComplete(HighlightAnimate);
                    BottomPanelController.StaticHighlightTransform.DOMoveX(_highlightedPosition.x, .15f);
                    break;
                case IconStatus.MovedAsideLeft:
                    transform.DOMove(_movedAsideLeftPosition, 0.2f).OnComplete(NormalAnimation);
                    break;
                case IconStatus.MovedAsideRight:
                    transform.DOMove(_movedAsideRightPosition, 0.2f).OnComplete(NormalAnimation);
                    break;
                case IconStatus.Normal:
                default:
                    transform.DOMove(_normalPosition, 0.2f).OnComplete(NormalAnimation);
                    break;
            }
        }

        private void HighlightAnimate()
        {
            animator.SetBool("IsSelected", true);
        }

        private void NormalAnimation()
        {
            animator.SetBool("IsSelected", false);
        }
    }

    public enum IconStatus
    {
        Highlighted,
        Normal,
        MovedAsideLeft,
        MovedAsideRight
    }
}
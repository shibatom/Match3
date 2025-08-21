using DG.Tweening;
using UnityEngine;

namespace Internal.Scripts.System.Orientation
{
    /// <summary>
    /// Holds game panels for appropriate orientation
    /// </summary>
    public class PanelOrientationHandler : MonoBehaviour
    {
        public RectTransform[] panels;
        public Transform movesTransform;

        private void OnEnable()
        {
        }

        public RectTransform topPanel;
        public RectTransform bottomPanel;
        public float animationDuration = 1f;

        private Vector2 topPanelHiddenPosition;
        private Vector2 bottomPanelHiddenPosition;
        private Vector2 topPanelVisiblePosition;
        private Vector2 bottomPanelVisiblePosition;

        private void Start()
        {
            // Store the visible positions
            topPanelVisiblePosition = topPanel.anchoredPosition;
            bottomPanelVisiblePosition = bottomPanel.anchoredPosition;

            // Calculate and store the hidden positions
            topPanelHiddenPosition = new Vector2(topPanelVisiblePosition.x, topPanelVisiblePosition.y + Screen.height);
            bottomPanelHiddenPosition =
                new Vector2(bottomPanelVisiblePosition.x, bottomPanelVisiblePosition.y - Screen.height);

            // Initially hide the panels
            topPanel.anchoredPosition = topPanelHiddenPosition;
            bottomPanel.anchoredPosition = bottomPanelHiddenPosition;

            ReferencerUI.Instance.OnToggleShop += HideTopBarPanel;
        }

        private void HideTopBarPanel(bool shouldShow)
        {
            Debug.Log("Hide Top Bar Panel: " + shouldShow);
            topPanel.gameObject.SetActive(shouldShow);
        }

        [ContextMenu("Show Menu")]
        public void ShowMenu()
        {
            AnimatePanel(topPanel, topPanelVisiblePosition, Ease.InBounce);
            AnimatePanel(bottomPanel, bottomPanelVisiblePosition, Ease.InBounce);
        }

        [ContextMenu("Hide Menu")]
        public void HideMenu()
        {
            AnimatePanel(topPanel, topPanelHiddenPosition, Ease.OutBounce);
            AnimatePanel(bottomPanel, bottomPanelHiddenPosition, Ease.OutBounce);
        }

        private void AnimatePanel(RectTransform panel, Vector2 targetPosition, Ease easeType)
        {
            float overshootAmount = 50f; // Adjust the overshoot amount here
            float overshootDuration = 0.2f; // Duration of the overshoot

            // Main animation with bounce effect
            panel.DOAnchorPos(targetPosition, animationDuration)
                .SetEase(easeType)
                .OnComplete(() =>
                {
                    // Bounce effect
                    panel.DOAnchorPos(targetPosition + new Vector2(0f, -overshootAmount), overshootDuration)
                        .SetEase(Ease.OutQuad)
                        .OnComplete(() =>
                        {
                            panel.DOAnchorPos(targetPosition, overshootDuration)
                                .SetEase(Ease.InQuad);
                        });
                });
        }
    }
}


using UnityEngine;
using Internal.Scripts.System.Orientation;

namespace Internal.Scripts.GUI
{
    public class SkipLevel : MonoBehaviour
    {
        public Vector2 verticalOrientationPosition;
        public Vector2 horizontalOrientationPosition;
        public RectTransform rectTransform;

        private void Start()
        {
            if (GameCameraOrientationHandler.currentOrientation == ScreenOrientation.Portrait)
                rectTransform.anchoredPosition = verticalOrientationPosition;
            else
                rectTransform.anchoredPosition = horizontalOrientationPosition;
        }
    }
}
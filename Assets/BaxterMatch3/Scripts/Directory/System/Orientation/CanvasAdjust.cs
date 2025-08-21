

using UnityEngine;
using UnityEngine.UI;

namespace Internal.Scripts.System.Orientation
{
    /// <summary>
    /// Canvas scaler depending from orientation
    /// </summary>
    public class CanvasAdjust : HandleOrientation
    {
        public override void OnOrientationChanged(ScreenOrientation orientation)
        {
            if (orientation == ScreenOrientation.Portrait)
                GetComponent<CanvasScaler>().matchWidthOrHeight = 1;
            else if (orientation == ScreenOrientation.LandscapeLeft)
                GetComponent<CanvasScaler>().matchWidthOrHeight = 0;
        }
    }
}
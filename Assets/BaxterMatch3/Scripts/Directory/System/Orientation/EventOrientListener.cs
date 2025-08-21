

using Internal.Scripts;
using UnityEngine;

namespace Internal.Scripts.System.Orientation
{
    /// <summary>
    /// Orientation listener. 
    /// </summary>
    [ExecuteInEditMode]
    public class EventOrientListener : MonoBehaviour
    {
        public delegate void OrientationChanged(ScreenOrientation orientation);
        public static event OrientationChanged OnOrientationChanged;

        private float previousAspect;
        public static ScreenOrientation previousOrientation;

        void OnEnable()
        {
            MainManager.OnMapState += OnStateChanged;
            MainManager.OnEnterGame += OnStateChanged;
        }


        void OnDisable()
        {
            MainManager.OnMapState -= OnStateChanged;
            MainManager.OnEnterGame -= OnStateChanged;

        }


        void OnStateChanged()
        {
            previousOrientation = ScreenOrientation.AutoRotation;
        }


        void Update()
        {
            // #if UNITY_EDITOR 
            var v = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, 0));
            v = new Vector3(Screen.width, Screen.height, 0);
            var aspect = Screen.height / (float)Screen.width;
            // #endif
            // if (Application.isPlaying)
            // {
            //     if (LevelManager.This.gameStatus == GameState.WaitForPopup) return;
            // }
            if (( v.x > v.y) && (ScreenOrientation.LandscapeLeft != previousOrientation) || aspect != previousAspect)
            {
                SetOrientation(ScreenOrientation.LandscapeLeft);
            }

            else if (( v.x > v.y) && (ScreenOrientation.LandscapeLeft != previousOrientation) || aspect != previousAspect)
            {
                SetOrientation(ScreenOrientation.LandscapeLeft);
            }

            else if (( v.x < v.y) && (ScreenOrientation.Portrait != previousOrientation) || aspect != previousAspect)
            {
                SetOrientation(ScreenOrientation.Portrait);
            }

            else if (( v.x < v.y) && (ScreenOrientation.Portrait != previousOrientation) || aspect != previousAspect)
            {
                SetOrientation(ScreenOrientation.Portrait);
            }

        }

        private void SetOrientation(ScreenOrientation orientation)
        {
            previousAspect = Screen.height / (float)Screen.width;
            previousOrientation = orientation;
            if (OnOrientationChanged != null)
                OnOrientationChanged(orientation);
        }
    }
}
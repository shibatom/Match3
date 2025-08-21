

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Internal.Scripts.GUI
{
    /// <summary>
    /// Safe area handler for iPhone X notch
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public class UICanvasHelper : MonoBehaviour
    {
        public static UnityEvent OnOrientationChange = new UnityEvent();
        public static UnityEvent OnResolutionChange = new UnityEvent();
        public static bool IsLandscape { get; private set; }

        private static List<UICanvasHelper> _helpers = new List<UICanvasHelper>();

        private static bool _screenChangeVarsInitialized = false;
        private static ScreenOrientation _lastOrientation = ScreenOrientation.Portrait;
        private static Vector2 _lastResolution = Vector2.zero;
        private static Rect _lastSafeArea = Rect.zero;

        private Canvas _canvas;
        private RectTransform _rectTransform;

        private RectTransform _safeAreaTransform;

        private void Awake()
        {
            if (!_helpers.Contains(this))
                _helpers.Add(this);

            _canvas = GetComponent<Canvas>();
            _rectTransform = GetComponent<RectTransform>();

            _safeAreaTransform = transform.Find("SafeArea") as RectTransform;

            if (!_screenChangeVarsInitialized)
            {
                _lastOrientation = Screen.orientation;
                _lastResolution.x = Screen.width;
                _lastResolution.y = Screen.height;
                _lastSafeArea = Screen.safeArea;

                _screenChangeVarsInitialized = true;
            }
        }

        void Update()
        {
            if (_helpers[0] != this)
                return;

            if (Application.isMobilePlatform)
            {
                if (Screen.orientation != _lastOrientation)
                    OrientationChanged();

                if (Screen.safeArea != _lastSafeArea)
                    SafeAreaChanged();
            }
            else
            {
                //resolution of mobile devices should stay the same always, right?
                // so this check should only happen everywhere else
                if (Screen.width != _lastResolution.x || Screen.height != _lastResolution.y)
                    ResolutionChanged();
            }
        }

        void ApplySafeArea()
        {
            if (_safeAreaTransform == null)
                return;

            var safeArea = Screen.safeArea;

            var anchorMin = safeArea.position;
            var anchorMax = safeArea.position + safeArea.size;
            anchorMin.x /= _canvas.pixelRect.width;
            anchorMin.y /= _canvas.pixelRect.height;
            anchorMax.x /= _canvas.pixelRect.width;
            anchorMax.y /= _canvas.pixelRect.height;

            _safeAreaTransform.anchorMin = anchorMin;
            _safeAreaTransform.anchorMax = anchorMax;
        }

        void OnDestroy()
        {
            if (_helpers != null && _helpers.Contains(this))
                _helpers.Remove(this);
        }

        private static void OrientationChanged()
        {
            _lastOrientation = Screen.orientation;
            _lastResolution.x = Screen.width;
            _lastResolution.y = Screen.height;

            IsLandscape = _lastOrientation == ScreenOrientation.LandscapeLeft || _lastOrientation == ScreenOrientation.LandscapeRight || _lastOrientation == ScreenOrientation.LandscapeLeft;
            OnOrientationChange.Invoke();
        }

        private static void ResolutionChanged()
        {
            if (_lastResolution.x == Screen.width && _lastResolution.y == Screen.height)
                return;

            _lastResolution.x = Screen.width;
            _lastResolution.y = Screen.height;

            IsLandscape = Screen.width > Screen.height;
            OnResolutionChange.Invoke();
        }

        private static void SafeAreaChanged()
        {
            if (_lastSafeArea == Screen.safeArea)
                return;

            _lastSafeArea = Screen.safeArea;

            for (int i = 0; i < _helpers.Count; i++)
            {
                _helpers[i].ApplySafeArea();
            }
        }

        public static Vector2 GetCanvasSize()
        {
            return _helpers[0]._rectTransform.sizeDelta;
        }

        public static Vector2 GetSafeAreaSize()
        {
            for (int i = 0; i < _helpers.Count; i++)
            {
                if (_helpers[i]._safeAreaTransform != null)
                {
                    return _helpers[i]._safeAreaTransform.sizeDelta;
                }
            }

            return GetCanvasSize();
        }
    }
}
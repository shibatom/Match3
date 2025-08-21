

using System.Collections;
using System.Linq;
using Internal.Scripts;
using UnityEngine;
using UnityEngine.Serialization;

namespace Internal.Scripts.System.Orientation
{
    /// <summary>
    /// Changes camera size depending from field size and orientation
    /// </summary>
    [ExecuteInEditMode]
    public class GameCameraOrientationHandler : MonoBehaviour
    {
        public Camera mainCamera;
        public RectTransform topPanel;
        public RectTransform bottomPanel;

        public RectTransform[] panelsVertical;
        public RectTransform[] panelsHorizontal;
        public RectTransform[] panelsHorizontalHD;

        [FormerlySerializedAs("orientationActivator")] public HandleOrientation handleOrientation;
        private Rect layoutRect;
        private Vector3 screenCenter;
        private bool levelLoaded;
        private float latestSize;
        private ScreenOrientation previousOrientation;
        public static ScreenOrientation currentOrientation;

        public Vector2 offsetFieldPosition;

        void OnEnable()
        {
            EventOrientListener.OnOrientationChanged += OnOrientationChanged;
            MainManager.OnWaitForTutorial += OnSublevelChanged;
            MainManager.OnLevelLoaded += OnLevelLoaded;
            MainManager.OnSublevelChanged += OnSublevelChanged;

        }

        private void OnSublevelChanged()
        {
            previousOrientation = ScreenOrientation.AutoRotation;
            // StartCoroutine(OrientationChangeDelay());

        }

        private void OnLevelLoaded()
        {
            scroll = MainManager.Instance.field.fieldData.scrollLevel;
            levelLoaded = true;
        }

        void OnDisable()
        {
            EventOrientListener.OnOrientationChanged -= OnOrientationChanged;
            MainManager.OnWaitForTutorial -= OnSublevelChanged;
            MainManager.OnLevelLoaded -= OnLevelLoaded;
            MainManager.OnSublevelChanged -= OnSublevelChanged;

        }

        public Vector3 GetCenter(bool vertical = false)
        {
            previousOrientation = currentOrientation;
            var transform1 = handleOrientation.GetCurrentPanels().First();
            var transform2 = handleOrientation.GetCurrentPanels()?.Last();
            var leftRect = RectTransformToWorldSpace(transform1);
            var rightRect = RectTransformToWorldSpace(transform2);
            var rect1 = leftRect;
            var rect2 = rightRect;
            var position = Vector2.zero;
            if (rightRect.size == Vector2.zero)
            {
                position = new Vector2(Screen.width, Screen.height);
                rect2 = new Rect(mainCamera.ScreenToWorldPoint(position).x, 0, 0, 0);
            }
            if (!vertical)
            {
                var v = mainCamera.WorldToScreenPoint(new Vector3(rect1.xMax, 0, 0));
                var v2 = mainCamera.WorldToScreenPoint(new Vector3(rect2.xMin, mainCamera.orthographicSize * 2, 0));
                layoutRect = new Rect(v.x, 0, v2.x - v.x, v2.y - v.y);
            }
            else
            {
                //Vector3 v = mainCamera.WorldToScreenPoint(new Vector3(0, rect1.yMax, 0));
                //Vector3 v2 = mainCamera.WorldToScreenPoint(new Vector3(0, rect2.xMax, 0));
                layoutRect = new Rect(rect1.xMax, rect1.yMax, rect2.yMin - rect1.xMax, rect1.width);
            }

            var screenToWorldPoint = mainCamera.ScreenToWorldPoint(layoutRect.center);
            if(scroll) screenToWorldPoint = mainCamera.ScreenToWorldPoint(layoutRect.min);
            return screenToWorldPoint + (Vector3)offsetFieldPosition;
        }

        private bool f;
        private bool scroll;
       [Range(0,1)] [SerializeField]private float portraitZoomOffset=0.45f;

        void OnOrientationChanged(ScreenOrientation orientation)
        {
            currentOrientation = orientation;
            // DefineLayoutRect();
            if (!Application.isPlaying) return;
            if (!levelLoaded) return;
            if ((int)MainManager.Instance.gameStatus < 2) return;
            if (orientation != previousOrientation)
            {
                f = true;
                StartCoroutine(OrientationChangeDelay());
            }
        }

        IEnumerator OrientationChangeDelay()
        {
            yield return new WaitForSeconds(0.3f);
            var cameraParameters = GetCameraParameters();
            StartCoroutine(SmoothZooming(cameraParameters.size));

            if (MainManager.Instance.gameStatus != GameState.ChangeSubLevel && MainManager.Instance.gameStatus != GameState.WaitForPopup)
                mainCamera.transform.position = cameraParameters.position;
            if(f)
            {
                yield return new WaitForEndOfFrame();
                f = false;
                StartCoroutine(OrientationChangeDelay());
            }
        }

        public struct CameraParameters
        {
            public float size;
            public Vector2 position;
        }

        public CameraParameters GetCameraParameters()
        {
            var cameraParameters = new CameraParameters();
            if (currentOrientation == ScreenOrientation.LandscapeLeft)
            {
                var fieldRect = MainManager.Instance.field.GetFieldRect();
                var h = fieldRect.height / 2 + 0.5f;
                // cameraParameters.size = Mathf.Clamp(v, 3, v);
                var topRectTransform = handleOrientation.GetCurrentPanels().FirstOrDefault();
                var topRect = GetWorldRect(topRectTransform);
                topRect = RectTransformToWorldSpace(topRectTransform);

                var w = fieldRect.width * Screen.height / Screen.width / 2 + 2f;
                // var w = (fieldRect.height + topRect.height * 2) / 2 + 0.5f;
                var maxLength = Mathf.Max(h, w);
                if (scroll) maxLength = Mathf.Clamp(maxLength, 3, 5);
                cameraParameters.size = Mathf.Clamp(maxLength, 3, maxLength) ;
                Vector2 offsetPosition = GetCenterOffset();
                cameraParameters.position = MainManager.Instance.field.GetPosition() + offsetPosition;
            }

            if (currentOrientation == ScreenOrientation.Portrait)
            {
                var topRectTransform = handleOrientation.GetCurrentPanels().FirstOrDefault();
                var transform2 = handleOrientation.GetCurrentPanels()?.Last();
                previousOrientation = currentOrientation;

                var topRect = GetWorldRect(topRectTransform);
                var bottomRect = GetWorldRect(transform2);

                var fieldRect = MainManager.Instance.field.GetFieldRect();
                var centerRect = new Rect(topRect.xMin, topRect.yMax, topRect.xMax, bottomRect.yMin - topRect.yMax);
                topRect = RectTransformToWorldSpace(topRectTransform);

                var h = fieldRect.width * Screen.height / Screen.width / 2 + 1.5f;
                var w = (fieldRect.height + topRect.height * 2) / 2 + 1.5f;
                var maxLength = Mathf.Max(h, w)-portraitZoomOffset;
                if (scroll) maxLength = Mathf.Clamp(maxLength, 3, 5);
                cameraParameters.size = Mathf.Clamp(maxLength, 3, maxLength);

                var offsetPosition = (Vector2)mainCamera.ScreenToWorldPoint(centerRect.center) -
                                     (Vector2)mainCamera.ScreenToWorldPoint(new Vector2(Screen.width / 2f,
                                         Screen.height / 2f));
                cameraParameters.position = MainManager.Instance.field.GetPosition() + offsetPosition - offsetFieldPosition;
            }
            return cameraParameters;
        }

        public Vector2 GetCenterOffset()
        {
            return mainCamera.ScreenToWorldPoint(new Vector2(Screen.width / 2f, Screen.height / 2f)) -
                   GetCenter();
        }

        IEnumerator SmoothZooming(float size)
        {
            if (latestSize == size) yield break;
            var firstSize = mainCamera.orthographicSize;
            var destSize = size;
            latestSize = size;
            var time = 0f;
            while (mainCamera.orthographicSize != destSize)
            {
                mainCamera.orthographicSize = Mathf.Lerp(firstSize, latestSize , time);
                time += 10f * Time.deltaTime;
                yield return new WaitForEndOfFrame();
            }
            var cameraParameters = GetCameraParameters();
            mainCamera.transform.position = cameraParameters.position;
        }

        public static Rect RectTransformToWorldSpace(RectTransform transform)
        {
            if (transform != null)
            {
                var size = Vector2.Scale(transform.rect.size, transform.lossyScale);
                var rect = new Rect(transform.position.x, Screen.height - transform.position.y, size.x, size.y);
                rect.x -= (transform.pivot.x * size.x);
                rect.y -= ((1.0f - transform.pivot.y) * size.y);
                return rect;
            }

            return new Rect();
        }


        //public Vector2 GetScreenPosition(RectTransform rect)
        //{
        //	Vector3[] corners = new Vector3[4];
        //	rect.GetWorldCorners(corners);
        //	Debug.Log("world " + corners[0]);
        //	Vector2 vector2 = RectTransformUtility.WorldToScreenPoint(mainCamera, corners[0]);
        //	Debug.Log("screen " + vector2);
        //	//Debug.Log("Screen point1: " + new Vector3(rect.rect.xMax, rect.rect.yMin, 0) + rect.position);
        //	//foreach (Vector3 corner in corners)
        //	//{
        //	//	Debug.Log("World point: " + corner);
        //	//	vector2 = RectTransformUtility.WorldToScreenPoint(null, corner);
        //	//	Debug.Log("Screen point: " + vector2);
        //	//	Debug.Log("Viewport: " + Camera.main.ScreenToViewportPoint(corner));
        //	//}
        //	return vector2;
        //}


        public Rect GetWorldRect(RectTransform uiElement)
        {
            var worldCorners = new Vector3[4];
            uiElement.GetWorldCorners(worldCorners);
            //Debug.Log("World Corners");
            for (var i = 0; i < 4; i++)
            {
                //Debug.Log("World Corner " + i + " : " + worldCorners[i]);
                worldCorners[i] =
                    RectTransformUtility.WorldToScreenPoint(mainCamera,
                        worldCorners[i]); //mainCamera.WorldToViewportPoint(worldCorners[i]);
                worldCorners[i] = new Vector3(worldCorners[i].x, Screen.height - worldCorners[i].y, worldCorners[i].z);
                //Debug.Log("Screen Corner " + i + " : " + worldCorners[i]);
            }

            var result = new Rect(
                worldCorners[0].x,
                worldCorners[1].y,
                worldCorners[2].x - worldCorners[0].x,
                Mathf.Abs(worldCorners[3].y - worldCorners[1].y));
            return result;
        }
    }
}
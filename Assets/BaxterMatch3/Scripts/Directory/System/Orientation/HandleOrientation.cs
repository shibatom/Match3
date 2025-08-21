

using System;
using System.Collections.Generic;
using Internal.Scripts;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Internal.Scripts.System.Orientation
{
    /// <summary>
    /// Activates object from the array depending from orientation
    /// </summary>
    [ExecuteInEditMode]
    public class HandleOrientation : MonoBehaviour
    {
        public List<Object> horrizontalObjects;
        public List<Object> horrizontalObjectsHD;
        public List<Object> verticalObjects;
        private RectTransform[] currentPanels;
        public static GameObject ActiveOrientationCanvas;

        public RectTransform[] GetCurrentPanels()
        {
            return currentPanels;
        }

        void OnEnable()
        {
            OnOrientationChanged(EventOrientListener.previousOrientation);
            EventOrientListener.OnOrientationChanged += OnOrientationChanged;
        }

        void OnDisable()
        {
            EventOrientListener.OnOrientationChanged -= OnOrientationChanged;
        }

        public virtual void OnOrientationChanged(ScreenOrientation orientation)
        {
            if (orientation == ScreenOrientation.Portrait)
            {
                var aspect = Screen.height / (float)Screen.width;
                aspect = (float)Math.Round(aspect, 2);

                SetActiveList(horrizontalObjects, false);
                SetActiveList(verticalObjects, true);
                SetActiveList(horrizontalObjectsHD, false);
            }

            else if (orientation == ScreenOrientation.LandscapeLeft)
            {
                var aspect = Screen.width / (float)Screen.height;
                aspect = (float)Math.Round(aspect, 2);
                if (aspect >= 1.6f)
                {
                    SetActiveList(horrizontalObjects, false);
                    SetActiveList(horrizontalObjectsHD, true);
                }

                if (aspect < 1.6f)
                {
                    SetActiveList(horrizontalObjectsHD, false);
                    SetActiveList(horrizontalObjects, true);
                }

                SetActiveList(verticalObjects, false);
            }
        }

        private void SetActiveList(List<Object> list, bool activate)
        {
            foreach (var item in list)
            {
                if (item is GameObject gameObj)
                {
                    gameObj.SetActive(activate);
                    if (activate)
                    {
                        PanelOrientationHandler panelOrientationHandler = gameObj.GetComponent<PanelOrientationHandler>();
                        currentPanels = panelOrientationHandler?.panels;
                        if (panelOrientationHandler)
                            MainManager.Instance.movesTransform = panelOrientationHandler.movesTransform;
                    }

                    if (activate) ActiveOrientationCanvas = gameObj;
                }
                else if (item is MonoBehaviour mono)
                {
                    mono.enabled = activate;

                    if (activate) ActiveOrientationCanvas = mono.gameObject;
                }
                else
                {
                    Debug.LogWarning($"Item in list is neither GameObject nor MonoBehaviour: {item}");
                }
            }
        }

    }
}


using UnityEngine;
#if UNITY_ANALYTICS
#endif

namespace Internal.Scripts.Integrations
{
    /// <summary>
    /// Game events listener.
    /// </summary>
    public class ObserverEventsListener : MonoBehaviour
    {
        public static bool isDragging = false;

        private void OnEnable()
        {
            MainManager.OnMapState += OnMapState;
            MainManager.OnEnterGame += OnEnterGame;
            MainManager.OnLevelLoaded += OnLevelLoaded;
            MainManager.OnMenuPlay += OnMenuPlay;
            MainManager.OnMenuComplete += OnMenuComplete;
            MainManager.OnStartPlay += OnStartPlay;
            MainManager.OnWin += OnWin;
            MainManager.OnLose += OnLose;

            MainManager.onClick += onClick;
            MainManager.onDragStart += onDragStart;
            MainManager.onDragEnd += onDragEnd;
        }

        private void OnDisable()
        {
            MainManager.OnMapState -= OnMapState;
            MainManager.OnEnterGame -= OnEnterGame;
            MainManager.OnLevelLoaded -= OnLevelLoaded;
            MainManager.OnMenuPlay -= OnMenuPlay;
            MainManager.OnMenuComplete -= OnMenuComplete;
            MainManager.OnStartPlay -= OnStartPlay;
            MainManager.OnWin -= OnWin;
            MainManager.OnLose -= OnLose;
        }

        #region GAME_EVENTS

        private static void OnMapState()
        {
        }

        private void OnEnterGame()
        {
            AnalyticsEvent("OnEnterGame", MainManager.Instance.currentLevel);
            Debug.Log("sallyas OnEnterGame()");
        }

        private static void OnLevelLoaded()
        {
        }

        void OnMenuPlay()
        {
        }

        void OnMenuComplete()
        {
        }

        void OnStartPlay()
        {
        }

        void OnWin()
        {
            AnalyticsEvent("OnWin", MainManager.Instance.currentLevel);
        }

        void OnLose()
        {
            AnalyticsEvent("OnLose", MainManager.Instance.currentLevel);
        }


        private void onDragEnd()
        {
            isDragging = false;
        }

        private void onDragStart()
        {
            if (!MainManager.Instance.dragBlocked)
            {
                MainManager.Instance.StopedAI();
                isDragging = true;
            }
        }

        private void onClick()
        {
            //Debug.Log("Sallog onClick");
        }

        #endregion

        void AnalyticsEvent(string _event, int level)
        {
#if UNITY_ANALYTICS
            Dictionary<string, object> dic = new Dictionary<string, object>();
            dic.Add(_event, level);
            Analytics.CustomEvent(_event, dic);

#endif
        }
    }
}
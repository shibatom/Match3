

using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using Internal.Scripts.System;
using Internal.Scripts.Level;
using BaxterMatch3.Animation.Directory.AnimationUI.Demo;

namespace Internal.Scripts.MapScripts
{
    public class LevelCampaign : MonoBehaviour
    {
        public static LevelCampaign Instance;
        public static IMapProgressManager MapProgressManager = new PlayerPrefsMapProgressManager();

        public bool IsGenerated;

        public MapLevel MapLevelPrefab;
        public Transform CharacterPrefab;
        public int Count = 10;

        public WaypointsMover WaypointsMover;
        public MapLevel CharacterLevel;
        public TranslationType TranslationType;

        public bool StarsEnabled;
        public StarsType StarsType;

        public bool ScrollingEnabled;
        public MapCamera MapCamera;
        public bool IsClickEnabled;
        public bool IsConfirmationEnabled;

        public void Awake()
        {
            Instance = this;
        }

        public void OnDestroy()
        {
            Instance = null;
        }

        public void OnEnable()
        {
            if (IsGenerated)
            {
                /* Reset();*/
            }
        }


        public static List<MapLevel> GetMapLevels()
        {
            List<MapLevel> MapLevels = new List<MapLevel>();
            if (MapLevels.Count == 0) //1.4.4
                MapLevels = FindObjectsOfType<MapLevel>().OrderBy(ml => ml.Number).WhereNotNull().ToList();

            return MapLevels;
        }

        public void Reset()
        {
            UpdateMapLevels();
            PlaceCharacterToLastUnlockedLevel();
            int number = GetLastReachedLevel();
            if (number > 1 && PersistantData.Win)
                WalkToLevelInternal(number);
            else TeleportToLevelInternal(number, true);
            SetCameraToCharacter();
        }

        private void UpdateMapLevels()
        {
            foreach (MapLevel mapLevel in GetMapLevels())
            {
                mapLevel.UpdateState(
                    MapProgressManager.LoadLevelStarsCount(mapLevel.Number),
                    IsLevelLocked(mapLevel.Number));
            }
        }

        private void PlaceCharacterToLastUnlockedLevel()
        {
            int lastUnlockedNumber = GetMapLevels().Where(l => !l.IsLocked).Select(l => l.Number).Max() - 1;
            lastUnlockedNumber = Mathf.Clamp(lastUnlockedNumber, 1, lastUnlockedNumber);
            TeleportToLevelInternal(lastUnlockedNumber, true);
        }


        public static int GetLastReachedLevel()
        {
            //1.3.3
            return GetMapLevels().Where(l => !l.IsLocked).Select(l => l.Number).Max();
        }

        public void PlayLastReachedLevel()
        {
            //Debug.LogError("PlayLastReachedLevel  " + GetIsClickEnabled());
            //if (GetIsClickEnabled())
            OnLevelSelected(GameManager.CurrentLevel + 1);
        }

        public void SetCameraToCharacter()
        {
            MapCamera mapCamera = FindObjectOfType<MapCamera>();
            if (mapCamera != null)
                mapCamera.SetPosition(WaypointsMover.transform.position);
        }

        public Vector3 getCharacterPosition()
        {
            return WaypointsMover.transform.position;
        }

        #region Events

        public static event EventHandler<GetLevelReachedNumberAndEventArgs> LevelSelected;
        public static event EventHandler<GetLevelReachedNumberAndEventArgs> LevelReached;

        #endregion

        #region Static API

        public static void CompleteLevel(int number)
        {
            CompleteLevelInternal(number, 1);
        }

        public static void CompleteLevel(int number, int starsCount)
        {
            CompleteLevelInternal(number, starsCount);
        }

        internal static void OnLevelSelected(int number)
        {
            Debug.Log("OnLevelSelected called with level number: " + number);

            if (LevelSelected != null && !IsLevelLocked(number))
            {
                Debug.Log("Level " + number + " is not locked. Triggering LevelSelected event.");
                LevelSelected(Instance, new GetLevelReachedNumberAndEventArgs(number));
            }
            else
            {
                if (LevelSelected == null)
                    Debug.LogWarning("LevelSelected event has no subscribers.");
                if (IsLevelLocked(number))
                    Debug.LogWarning("Level " + number + " is locked. Skipping event invocation.");
            }

            if (!Instance.IsConfirmationEnabled)
            {
                Debug.Log("Confirmation disabled. Navigating directly to level: " + number);
                GoToLevel(number);
            }
            else
            {
                Debug.Log("Confirmation enabled. Waiting for user confirmation to navigate to level: " + number);
            }
        }

        public static void GoToLevel(int number)
        {
            switch (Instance.TranslationType)
            {
                case TranslationType.Teleportation:
                    Instance.TeleportToLevelInternal(number, false);
                    break;
                case TranslationType.Walk:
                    Instance.WalkToLevelInternal(number);
                    break;
            }
        }

        public static bool IsLevelLocked(int number)
        {
            bool locked = number > 1 && MapProgressManager.LoadLevelStarsCount(number - 1) == 0;
            Debug.Log($"IsLevelLocked: Level {number} is {(locked ? "locked" : "unlocked")}, previous level stars: {MapProgressManager.LoadLevelStarsCount(number - 1)}");
            return false;
        }

        public static void OverrideMapProgressManager(IMapProgressManager mapProgressManager)
        {
            MapProgressManager = mapProgressManager;
        }

        public static void ClearAllProgress()
        {
            Instance.ClearAllProgressInternal();
        }

        public static bool IsStarsEnabled()
        {
            return Instance.StarsEnabled;
        }

        public static bool GetIsClickEnabled()
        {
            return Instance.IsClickEnabled;
        }

        public static bool GetIsConfirmationEnabled()
        {
            return Instance.IsConfirmationEnabled;
        }

        #endregion

        private static void CompleteLevelInternal(int number, int starsCount)
        {
            if (IsLevelLocked(number))
            {
                Debug.Log(string.Format("Can't complete locked level {0}.", number));
            }
            else if (starsCount < 1 || starsCount > 3)
            {
                Debug.Log(string.Format("Can't complete level {0}. Invalid stars count {1}.", number, starsCount));
            }
            else
            {
                int curStarsCount = MapProgressManager.LoadLevelStarsCount(number);
                int maxStarsCount = Mathf.Max(curStarsCount, starsCount);
                MapProgressManager.SaveLevelStarsCount(number, maxStarsCount);

                if (Instance != null)
                    Instance.UpdateMapLevels();
            }
        }

        private void TeleportToLevelInternal(int number, bool isQuietly)
        {
            /*MapLevel mapLevel = GetLevel(number);
            mapLevel.SetEffect();
            if (mapLevel.IsLocked)
            {
                Debug.Log(string.Format("Can't jump to locked level number {0}.", number));
            }
            else
            {
                WaypointsMover.transform.position = mapLevel.PathPivot.transform.position; //need to fix in the map plugin
                CharacterLevel = mapLevel;
                if (!isQuietly)
                    RaiseLevelReached(number);
            }*/
        }

        public delegate void ReachedLevelEvent();

        public static ReachedLevelEvent OnLevelReached;

        private void WalkToLevelInternal(int number)
        {
            MapLevel mapLevel = GetLevel(number);
            mapLevel.SetEffect();
            CharacterLevel = GetLevel(number - 1);
            if (mapLevel.IsLocked)
            {
                Debug.Log(string.Format("Can't go to locked level number {0}.", number));
            }
            else
            {
                WaypointsMover.Move(CharacterLevel.PathPivot, mapLevel.PathPivot,
                    () =>
                    {
                        RaiseLevelReached(number);
                        CharacterLevel = mapLevel;
                        OnLevelReached?.Invoke();
                    });
            }
        }

        private void RaiseLevelReached(int number)
        {
            MapLevel mapLevel = GetLevel(number);
            mapLevel.SetEffect();
            if (!string.IsNullOrEmpty(mapLevel.SceneName))
                SceneManager.LoadScene(mapLevel.SceneName);

            if (LevelReached != null)
                LevelReached(this, new GetLevelReachedNumberAndEventArgs(number));
        }

        public MapLevel GetLevel(int number)
        {
            return GetMapLevels().SingleOrDefault(ml => ml.Number == number);
        }

        private void ClearAllProgressInternal()
        {
            foreach (MapLevel mapLevel in GetMapLevels())
                MapProgressManager.ClearLevelProgress(mapLevel.Number);
            Reset();
        }

        public void SetStarsEnabled(bool bEnabled)
        {
            StarsEnabled = bEnabled;
            int starsCount = 0;
            foreach (MapLevel mapLevel in GetMapLevels().WhereNotNull())
            {
                mapLevel.UpdateStars(starsCount);
                starsCount = (starsCount + 1) % 4;
                mapLevel.StarsHoster.gameObject.SetActive(bEnabled);
                //mapLevel.SolidStarsHoster.gameObject.SetActive(bEnabled);
            }
        }

        public void SetStarsType(StarsType starsType)
        {
            StarsType = starsType;
            foreach (MapLevel mapLevel in GetMapLevels().WhereNotNull())
                mapLevel.UpdateStarsType(starsType);
        }
    }
}
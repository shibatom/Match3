using Internal.Scripts.System;
using UnityEngine;

namespace HelperScripts
{
    public class RoadmapItemsManager : MonoBehaviour
    {
        public static bool IsDoneShowingItem = false;

        [SerializeField] private LevelItemActionHandler itemActionPrefab;
        [SerializeField] private Transform[] targetLocations;

        private bool _hasShownAlready = false;

        private void OnDisable()
        {
            _hasShownAlready = false;
            IsDoneShowingItem = false;
        }

        public bool CheckForLevelItemActions()
        {
            gameObject.SetActive(true);
            Debug.LogError("CheckForLevelItemActions  ");
            //Debug.LogError("gameStatus  " + LevelManager.THIS.gameStatus);
            int currentLevel = GlobalValue.CurrentLevel;
            Debug.LogError("currentLevel  " + currentLevel);

            if (!HasLevelItemActionHappened(currentLevel))
            {
                ShowItemAction(currentLevel);
                return true;
            }

            return false;
        }

        private void ShowItemAction(int level)
        {
            _hasShownAlready = true;

            switch (level)
            {
                // Boat And River
                case 5:
                    var item = Instantiate(itemActionPrefab, ReferencerUI.Instance.transform);
                    item.SetItemInfo(0, targetLocations[0].position);
                    SetLevelItemActionHappened(level, true);
                    break;

                // Banner And Bee Hive
                case 10:
                    Instantiate(itemActionPrefab, ReferencerUI.Instance.transform);
                    break;

                // Mr Crocodile
                case 18:
                    Instantiate(itemActionPrefab, ReferencerUI.Instance.transform);
                    break;

                // Saving Private Luna
                case 27:
                    Instantiate(itemActionPrefab, ReferencerUI.Instance.transform);
                    break;
                default:
                    IsDoneShowingItem = true;
                    break;
            }
        }

        public static bool HasLevelItemActionHappened(int level)
        {
            return PlayerPrefs.GetInt($"RoadmapItemLevel_{level}", 0) == 1;
        }

        public static void SetLevelItemActionHappened(int level, bool happened)
        {
            PlayerPrefs.SetInt($"RoadmapItemLevel_{level}", happened ? 1 : 0);
        }
    }
}
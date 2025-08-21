

using System;
using UnityEngine;

namespace Internal.Scripts.MapScripts
{
    public class GetTestDataFromApi : MonoBehaviour, IMapProgressManager
    {
        private int _levelNumber = 1;
        private int _starsCount = 1;
        private bool _isShow;

        public EventSystemTestButton YesButton;
        public EventSystemTestButton NoButton;
        public GameObject ConfirmationView;
        public int SelectedLevelNumber;

        public void Awake()
        {
            //Uncomment to set this script as IMapProgressManager
            //LevelsMap.OverrideMapProgressManager(this);
        }

        #region Events

        public void OnEnable()
        {
            Debug.Log("Subscribe to events.");
            LevelCampaign.LevelSelected += OnLevelSelected;
            LevelCampaign.LevelReached += OnLevelReached;
            //            YesButton.Click += OnYesButtonClick;
            //            NoButton.Click += OnNoButtonClick;
        }

        public void OnDisable()
        {
            Debug.Log("Unsubscribe from events.");
            LevelCampaign.LevelSelected -= OnLevelSelected;
            LevelCampaign.LevelReached -= OnLevelReached;
            //           YesButton.Click -= OnYesButtonClick;
            //           NoButton.Click -= OnNoButtonClick;
        }

        private void OnLevelReached(object sender, GetLevelReachedNumberAndEventArgs e)
        {
            Debug.Log(string.Format("Level {0} reached.", e.Number));
        }

        #endregion

        #region Api test

        public void OnGUI()
        {
            GUILayout.BeginVertical();

            DrawToggleShowButton();

            if (_isShow)
            {
                DrawInputParameters();
                if (GUILayout.Button("Complete all  levels"))
                {
                    for (var i = 1; i < GameObject.Find("Levels").transform.childCount; i++)
                    {
                        // LevelsMap.CompleteLevel(i, _starsCount);
                        SaveLevelStarsCount(i, _starsCount);
                    }
                }

                if (GUILayout.Button("Complete level"))
                {
                    if (LevelCampaign.IsStarsEnabled())
                        LevelCampaign.CompleteLevel(_levelNumber, _starsCount);
                    else
                        LevelCampaign.CompleteLevel(_levelNumber);
                }

                if (GUILayout.Button("Go to level"))
                {
                    LevelCampaign.GoToLevel(_levelNumber);
                }

                if (GUILayout.Button("Is level locked"))
                {
                    var isLocked = LevelCampaign.IsLevelLocked(_levelNumber);
                    Debug.Log(string.Format("Level {0} is {1}",
                        _levelNumber,
                        isLocked ? "locked" : "not locked"));
                }

                if (GUILayout.Button("Clear all progress"))
                {
                    LevelCampaign.ClearAllProgress();
                }
            }

            GUILayout.EndVertical();
        }

        private void DrawToggleShowButton()
        {
            if (!_isShow)
            {
                if (GUILayout.Button("Show API tests"))
                {
                    _isShow = true;
                }
            }

            if (_isShow)
            {
                if (GUILayout.Button("Hide API tests"))
                {
                    _isShow = false;
                }
            }
        }

        private void DrawInputParameters()
        {
            GUILayout.BeginHorizontal();

            GUILayout.Label("Level number:");
            var strLevelNumber = GUILayout.TextField(_levelNumber.ToString(), 10, GUILayout.Width(80));
            int.TryParse(strLevelNumber, out _levelNumber);

            if (LevelCampaign.IsStarsEnabled())
            {
                GUILayout.Label("Stars count:");
                var strStarsCount = GUILayout.TextField(_starsCount.ToString(), 10, GUILayout.Width(80));
                int.TryParse(strStarsCount, out _starsCount);
            }

            GUILayout.EndHorizontal();
        }

        #endregion

        #region IMapProgressManager

        private string GetLevelKey(int number)
        {
            return string.Format("Level.{0:000}.StarsCount", number);
        }

        public string GetScoreKey(int number)
        {
            throw new NotImplementedException();
        }

        public void SaveLevelStarsCount(int level, int starsCount, int score)
        {
            throw new NotImplementedException();
        }

        public int LoadLevelStarsCount(int level)
        {
            return level > 10 ? 0 : (level % 3 + 1);
        }

        public void SaveLevelStarsCount(int level, int starsCount)
        {
            Debug.Log(string.Format("Stars count {0} of level {1} saved.", starsCount, level));
            PlayerPrefs.SetInt(GetLevelKey(level), starsCount);
        }

        public void ClearLevelProgress(int level)
        {
        }

        #endregion

        #region Confirmation demo

        private void OnLevelSelected(object sender, GetLevelReachedNumberAndEventArgs e)
        {
            if (LevelCampaign.GetIsConfirmationEnabled())
            {
                SelectedLevelNumber = e.Number;
                // ConfirmationView.SetActive(true);
            }
        }

        private void OnNoButtonClick(object sender, EventArgs e)
        {
            ConfirmationView.SetActive(false);
        }

        private void OnYesButtonClick(object sender, EventArgs e)
        {
            ConfirmationView.SetActive(false);
            LevelCampaign.GoToLevel(SelectedLevelNumber);
        }

        public int GetLastLevel()
        {
            return 0;
        }

        string IMapProgressManager.GetLevelKey(int number)
        {
            return GetLevelKey(number);
        }

        #endregion
    }
}
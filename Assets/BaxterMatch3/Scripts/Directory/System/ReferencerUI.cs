

using System;
using HelperScripts;
using Internal.Scripts;
using UnityEngine;

namespace Internal.Scripts.System
{
    public class ReferencerUI : MonoBehaviour
    {
        public Color GUIColor=Color.yellow;
        public Color GUIButtonColor=Color.green;
        public Color GUIHeaderColor=Color.cyan;
        public static ReferencerUI Instance;
        public GameObject PlayButton;
        public GameObject PrePlay;
        public GameObject MenuPlay;
        public GameObject PreCompleteBanner;
        public GameObject PreCompleteAnimations;

        public Canvas topPanelCanvas;

        //public MeshRenderer topPanelMiloMesh;
        public GameObject menuBottomPanelCanvas;
        public GameObject MenuComplete;
        public GameObject MenuFailed;
        public GameObject PreFailed;
        public GameObject BonusSpin;
        public BoosterOffer BoostShop;
        public GameObject LiveShop;
        public GameObject GemsShop;
        public GameObject inGameCoinShop;
        public GameObject Areas_Page;
        public GameObject Collections_Page;
        public GameObject Reward;
        public GameObject Daily;
        public GameObject Tutorials;
        public GameObject Settings;
        public GameObject StoryScene;
        public GameObject leaderbaord;
        public GameObject SettingsMenu;

        public Action<bool> OnToggleShop;

        private void Awake()
        {
            Instance = this;
            Instance.HideAll();
            //topPanelMiloMesh = topPanelCanvas.GetComponentInChildren<Spine.Unity.SkeletonMecanim>().GetComponent<MeshRenderer>();
            //topPanelMiloMesh.sortingOrder = 4;
        }

        private void Start()
        {
        }

        public void HideAll()
        {
            var canvas = Instance.transform;
            foreach (Transform item in canvas)
            {
                if (!((item.name is "SettingsButton" or "Tutorials" or "Orientations" or "TutorialManager" or "Play") || item.name.Contains("Rate")))
                    item.gameObject.SetActive(false);
            }
        }

        public void ShowDailyMenu()
        {
            int currentLevel = HelperScripts.GlobalValue.CurrentLevel;
            if (currentLevel > 3)
            {
                ShowDailyReward();
            }
        }

        private static void ShowDailyReward()
        {
            if (!OnlineTime.THIS.dateReceived)
            {
                OnlineTime.OnDateReceived += ShowDailyReward;
                return;
            }

            var DateReward = PlayerPrefs.GetString("DateReward", default(DateTime).ToString());
            var dateTimeReward = DateTime.Parse(DateReward);
            DateTime testDate = OnlineTime.THIS.serverTime;

            if (MainManager.GetGameStatus() == GameState.Map && !GameObject.Find("CanvasGlobal").transform.Find("storyScene").gameObject.activeSelf)
            {
                if (DateReward == "" || DateReward == default(DateTime).ToString())
                    Initiations.Instance.DailyMenu.SetActive(true);
                else
                {
                    var timePassedDaily = testDate.Subtract(dateTimeReward).TotalDays;
                    if (timePassedDaily >= 1)
                        Initiations.Instance.DailyMenu.SetActive(true);
                }
            }
        }

        public void OpenSettingsMenu()
        {
            SettingsMenu.SetActive(true);
        }

        public BoosterOffer GetBoostShop()
        {
            return BoostShop;
        }
    }
}
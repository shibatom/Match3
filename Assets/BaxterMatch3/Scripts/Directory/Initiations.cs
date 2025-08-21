

using System;
using System.Collections;
using HelperScripts;
using Internal.Scripts.GUI;
using Internal.Scripts.GUI.Boost;
using Internal.Scripts.Level;
using Internal.Scripts.MapScripts;
using Internal.Scripts.System;
using UnityEngine;

namespace Internal.Scripts
{
    /// <summary>
    /// class for main system variables, ads control and in-app purchasing
    /// </summary>
    public class Initiations : MonoBehaviour
    {
        public static Initiations Instance;

        /// opening level in Menu Play
        public static int openLevel;

        ///life gaining timer
        public static float RestLifeTimer;

        ///date of exit for life timer
        public static string DateOfExit;

        //reward which can be receive after watching rewarded ads
        public RewardsType currentReward;

        ///amount of life
        public static int lifes { get; set; }

        //EDITOR: max amount of life
        public int CapOfLife = 5;

        //EDITOR: time for rest life
        public float TotalTimeForRestLifeHours;

        //EDITOR: time for rest life
        public float TotalTimeForRestLifeMin = 15;

        //EDITOR: time for rest life
        public float TotalTimeForRestLifeSec = 60;

        //EDITOR: coins gifted in start
        public int FirstGems = 20;

        //amount of coins
        public static int Gems;

        //wait for purchasing of coins succeed
        public static int waitedPurchaseGems;

        //EDITOR: how often to show the "Rate us on the store" popup
        public int ShowRateEvery;

        //EDITOR: rate url
        public string RateURL;

        public string RateURLIOS;

        //story state
        public bool hasStoryFinished = false;

        //rate popup reference
        [SerializeField] private GameObject rate;

        //EDITOR: amount for rewarded ads
        public int rewardedGems = 5;

        //EDITOR: should player lose a life for every passed level
        public bool losingLifeEveryGame;

        //daily reward popup reference
        public GameObject DailyMenu;

        //cardholder popup
        public GameObject CardHolder;

        // Use this for initialization
        void Awake()
        {
            Application.targetFrameRate = 120;
            Instance = this;
            RestLifeTimer = PlayerPrefs.GetFloat("RestLifeTimer");
            DateOfExit = PlayerPrefs.GetString("DateOfExit", "");
            DebugLogManager.Init();
            Gems = PlayerPrefs.GetInt("Gems");
            lifes = GlobalValue.Life;
            if (PlayerPrefs.GetInt("Lauched") == 0)
            {
                //First lauching
                //ShowStoryForFisrtLunch();
                lifes = CapOfLife;
                PlayerPrefs.SetInt("Lifes", lifes);
                Gems = FirstGems;
                PlayerPrefs.SetInt("Gems", Gems);
                PlayerPrefs.SetInt("Music", 1);
                PlayerPrefs.SetInt("Sound", 1);

                PlayerPrefs.SetInt("Lauched", 1);
                PlayerPrefs.Save();
            }

            /*rate = Instantiate(Resources.Load("Prefabs/Rate")) as GameObject;
            rate.SetActive(false);
            rate.transform.SetParent(MenuReference.THIS.transform);
            rate.transform.localPosition = Vector3.zero;
            rate.GetComponent<RectTransform>().offsetMin = new Vector2(-5, -5);
            rate.GetComponent<RectTransform>().offsetMax = new Vector2(5, 5);
//        rate.GetComponent<RectTransform>().anchoredPosition = (Resources.Load("Prefabs/Rate") as GameObject).GetComponent<RectTransform>().anchoredPosition;
            rate.transform.localScale = Vector3.one;
            var g = MenuReference.THIS.Reward.gameObject;
            g.SetActive(true);
            g.SetActive(false);*/
            if (PersistantData.TotalLevels == 0)
                PersistantData.TotalLevels = LoadingController.GetLastLevelNum();
/*#if FACEBOOK
            FacebookManager fbManager = new GameObject("FacebookManager").AddComponent<FacebookManager>();
#endif*/
#if GOOGLE_MOBILE_ADS
            var obj = FindObjectOfType<RewAdmobManager>();
            if (obj == null)
            {
                GameObject gm = new GameObject("AdmobRewarded");
                gm.AddComponent<RewAdmobManager>();
            }
#endif
        }

        private void ShowStoryForFisrtLunch()
        {
            StartCoroutine(ShowLevelStory(1, () => { OpenMenuPlay(1); }));
        }

        public void SaveLevelStarsCount(int level, int starsCount)
        {
            Debug.Log(string.Format("Stars count {0} of level {1} saved.", starsCount, level));
            PlayerPrefs.SetInt(GetLevelKey(level), starsCount);
        }

        private string GetLevelKey(int number)
        {
            return string.Format("Level.{0:000}.StarsCount", number);
        }


        /*public void ShowRate()
        {
            rate.SetActive(true);
        }*/

        public void ShowRate()
        {
            Instantiate(rate, FindAnyObjectByType<BottomPanelController>().transform.parent);
        }

        public void ShowReward()
        {
            var reward = ReferencerUI.Instance.Reward.GetComponent<IconReward>();
            if (currentReward == RewardsType.GetGems)
            {
                ShowGemsReward(rewardedGems);
                ReferencerUI.Instance.GemsShop.GetComponent<CentralEventManager>().CloseMenu();
            }
            else if (currentReward == RewardsType.GetLifes)
            {
                reward.SetIconSprite(1);
                reward.gameObject.SetActive(true);
                RestoreLifes();
                ReferencerUI.Instance.LiveShop.GetComponent<CentralEventManager>().CloseMenu();
            }
            else if (currentReward == RewardsType.GetGoOn)
            {
                ReferencerUI.Instance.PreFailed.GetComponent<CentralEventManager>().GoOnFailed();
            }
            else if (currentReward == RewardsType.FreeAction)
            {
                Debug.LogError("Implement Spin Here  ");
                //MenuReference.THIS.BonusSpin.GetComponent<BonusSpin>().StartSpin();
            }
        }

        public void ShowGemsReward(int amount)
        {
            var reward = ReferencerUI.Instance.Reward.GetComponent<IconReward>();
            reward.SetIconSprite(0);
            reward.gameObject.SetActive(true);
            AddGems(amount);
        }

        public void SetGems(int count)
        {
            Gems = count;
            PlayerPrefs.SetInt("Gems", Gems);
            PlayerPrefs.Save();
        }


        public void AddGems(int count)
        {
            Gems += count;
            PlayerPrefs.SetInt("Gems", Gems);
            PlayerPrefs.Save();
#if PLAYFAB || GAMESPARKS || EPSILON
            NetworkManager.currencyManager.IncBalance(count);
#endif
        }

        public void SpendGems(int count)
        {
            CentralSoundManager.Instance.PlayOneShot(CentralSoundManager.Instance.cash);
            Gems -= count;
            PlayerPrefs.SetInt("Gems", Gems);
            PlayerPrefs.Save();
#if PLAYFAB || GAMESPARKS || EPSILON
            NetworkManager.currencyManager.DecBalance(count);
#endif
        }


        public void RestoreLifes()
        {
            lifes = CapOfLife;
            PlayerPrefs.SetInt("Lifes", lifes);
            PlayerPrefs.Save();

            FindObjectOfType<AddToLifeCount>()?.ResetTimer();
        }

        public void AddLife(int count)
        {
            lifes += count;
            if (lifes > CapOfLife)
                lifes = CapOfLife;
            //PlayerPrefs.SetInt("Lifes", lifes);
            //GlobalValue.Life = lifes;
            PlayerPrefs.Save();
        }

        public int GetLife()
        {
            if (lifes > CapOfLife)
            {
                lifes = CapOfLife;
                PlayerPrefs.SetInt("Lifes", lifes);
                PlayerPrefs.Save();
            }

            return lifes;
        }

        public void PurchaseSucceded()
        {
            CentralSoundManager.Instance.PlayOneShot(CentralSoundManager.Instance.cash);
            AddGems(waitedPurchaseGems);
            waitedPurchaseGems = 0;
        }

        public void SpendLife(int count)
        {
            if (lifes > 0)
            {
                lifes -= count;
                PlayerPrefs.SetInt("Lifes", lifes);
                PlayerPrefs.Save();
            }

            //else
            //{
            //    GameObject.Find("Canvas").transform.Find("RestoreLifes").gameObject.SetActive(true);
            //}
        }

        public void BuyBoost(BoostType boostType, int price, int count)
        {
            PlayerPrefs.SetInt("" + boostType, PlayerPrefs.GetInt("" + boostType) + count);
            PlayerPrefs.Save();
#if PLAYFAB || GAMESPARKS
            //NetworkManager.dataManager.SetBoosterData();
#endif
        }

        public void SpendBoost(BoostType boostType)
        {
            PlayerPrefs.SetInt("" + boostType, PlayerPrefs.GetInt("" + boostType) - 1);
            PlayerPrefs.Save();
#if PLAYFAB || GAMESPARKS
            //NetworkManager.dataManager.SetBoosterData();
#endif
        }

        void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                if (RestLifeTimer > 0)
                {
                    PlayerPrefs.SetFloat("RestLifeTimer", RestLifeTimer);
                }

                PlayerPrefs.SetInt("Lifes", lifes);
                PlayerPrefs.SetString("DateOfExit", OnlineTime.THIS.serverTime.ToString());
                PlayerPrefs.Save();
            }
        }

        void OnApplicationQuit()
        {
            if (RestLifeTimer > 0)
            {
                PlayerPrefs.SetFloat("RestLifeTimer", RestLifeTimer);
            }

            PlayerPrefs.SetInt("Lifes", lifes);
            PlayerPrefs.SetString("DateOfExit", OnlineTime.THIS.serverTime.ToString());
            PlayerPrefs.Save();
        }

        IEnumerator ShowLevelStory(int selectedLevel, Action callback)
        {
            yield return null;
            // ShowLevelStory (Boat)
            /*Debug.LogError(" ShowLevelStory ");
            if (MenuReference.THIS.roadmapItemsManager.CheckForLevelItemActions())
            {
                yield return new WaitUntil(() => RoadmapItemsManager.IsDoneShowingItem);
            }*/

            // Story Commented
            // Check if the story has been shown for this level
            /*if (!PlayerPrefs.HasKey("Level_" + selectedLevel + "_Story"))
            {
                var StoryCompnent = MenuReference.THIS.StoryScene.GetComponent<StoryManager>();
                if (StoryCompnent.HasLevelContainsStory(selectedLevel))
                {
                    StoryCompnent.hasStoryFinished = false;


                    StoryCompnent.gameObject.SetActive(true);
                    StoryCompnent.StartStory();
                    yield return new WaitUntil(() => StoryCompnent.hasStoryFinished == true);
                }

                PlayerPrefs.SetInt("Level_" + selectedLevel + "_Story", 1);
            }*/

            /*if (StoryManager.Instance.HasLEvelContainsCard(selectedLevel))
            {
                var CardMenu = MenuReference.THIS.cardHolder.GetComponent<CardMenu>();
                CardMenu.gameObject.SetActive(true);
                yield return new WaitUntil(() => !CardMenu.isActiveAndEnabled);
            }*/

            callback();
        }


        public void OnLevelClicked(object sender, GetLevelReachedNumberAndEventArgs args)
        {
            int currentLevel = GlobalValue.CurrentLevel;
            /*if (EventSystem.current.IsPointerOverGameObject(0) && (currentLevel > 1))
            {
                Debug.Log("sallog + EventSystem.current.IsPointerOverGameObject");
   
                return;
            }*/
            if (!GameObject.Find("CanvasGlobal").transform.Find("PlayPanel").gameObject.activeSelf &&
                !GameObject.Find("CanvasGlobal").transform.Find("ShopPanel").gameObject.activeSelf &&
                !GameObject.Find("CanvasGlobal").transform.Find("LifeRefillPopup").gameObject.activeSelf &&
                !GameObject.Find("CanvasGlobal").transform.Find("storyScene").gameObject.activeSelf)
            {
                CentralSoundManager.Instance.PlayOneShot(CentralSoundManager.Instance.click);

                StartCoroutine(ShowLevelStory(args.Number, () =>
                {
                    ReferencerUI.Instance.ShowDailyMenu();
                    OpenMenuPlay(args.Number);
                    ShowLeadboard(args.Number);
                }));
            }
        }

        void OnMouseDown()
        {
            // Log the selected object to the debug console.
            Debug.Log($"Selected object: {gameObject.name}");
        }

        public static void OpenMenuPlay(int num)
        {
            GlobalValue.CurrentLevel = num;
            PlayerPrefs.Save();
            MainManager.Instance.MenuPlayEvent();
            MainManager.Instance.LoadLevel();
            openLevel = num;
            PersistantData.OpenNextLevel = false;
            ReferencerUI.Instance.MenuPlay.gameObject.SetActive(true);
        }

        static void ShowLeadboard(int levelNumber)
        {
#if EPSILON
            var leadboardList = FindObjectsOfType<LeadboardManager>();
            foreach (var obj in leadboardList)
            {
                obj.levelNumber = levelNumber;
            }
#endif
        }

        void OnEnable()
        {
            LevelCampaign.LevelSelected += OnLevelClicked;
            LevelCampaign.OnLevelReached += OnLevelReached;
        }

        void OnDisable()
        {
            LevelCampaign.LevelSelected -= OnLevelClicked;
            LevelCampaign.OnLevelReached -= OnLevelReached;

            PlayerPrefs.SetFloat("RestLifeTimer", RestLifeTimer);
            PlayerPrefs.SetInt("Lifes", lifes);
            PlayerPrefs.SetString("DateOfExit", OnlineTime.THIS.serverTime.ToString());
            PlayerPrefs.Save();
        }

        void OnLevelReached()
        {
            var num = GlobalValue.CurrentLevel;
            if (PersistantData.OpenNextLevel && PersistantData.TotalLevels >= num)
            {
                Debug.Log(num + "reached");

                StartCoroutine(ShowLevelStory(num, () =>
                {
                    OpenMenuPlay(num);
                    // ShowLeadboard(args.Number);
                }));
            }
        }
    }

    /// moves or time is level limit type
    public enum LIMIT
    {
        MOVES,
        TIME
    }

    /// reward type for rewarded ads watching
    public enum RewardsType
    {
        GetLifes,
        GetGems,
        GetGoOn,
        FreeAction
    }
}


using System;
using System.Collections;
using System.Collections.Generic;
using HelperScripts;
using Internal.Scripts;
using Internal.Scripts.GUI.Boost;
using Internal.Scripts.Level;
using Internal.Scripts.MapScripts.StaticMap.Editor;
using Internal.Scripts.System;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_ADS
using UnityEngine.Advertisements;
#endif


namespace Internal.Scripts.GUI
{
    /// <summary>
    /// Popups animation event manager
    /// </summary>
    public class CentralEventManager : MonoBehaviour
    {
        public bool PlayOnEnable = true;
        bool WaitForPickupFriends;

        bool WaitForAksFriends;
        Dictionary<string, string> parameters;

        void OnEnable()
        {
            if (PlayOnEnable)
            {
                //            SoundBase.Instance.PlayOneShot(SoundBase.Instance.swish[0]);
            }

            if (name == "PlayPanel")
            {
            }

            if (name == "PrePlay")
            {
                // GameObject
            }

            if (name == "PreFailedPanel")
            {
                //            SoundBase.Instance.PlayOneShot(SoundBase.Instance.gameOver[0]);
                transform.Find("Banner/Buttons/Video").gameObject.SetActive(false);
                transform.Find("Banner/Buttons/Buy").GetComponent<Button>().interactable = true;

                GetComponent<Animation>().Play();
            }

            if (name == "Settings" || name == "MenuPause")
            {
                if (PlayerPrefs.GetInt("Sound") < 1)
                {
                    transform.Find("Sound/Sound/SoundOff").gameObject.SetActive(true);
                    //transform.Find("Sound/Sound").GetComponent<Image>().enabled = false;
                }
                else
                {
                    transform.Find("Sound/Sound/SoundOff").gameObject.SetActive(false);
                    //transform.Find("Sound/Sound").GetComponent<Image>().enabled = true;
                }

                if (PlayerPrefs.GetInt("Music") < 1)
                {
                    transform.Find("Music/Music/MusicOff").gameObject.SetActive(true);
                    //transform.Find("Music/Music").GetComponent<Image>().enabled = false;
                }
                else
                {
                    transform.Find("Music/Music/MusicOff").gameObject.SetActive(false);
                    //transform.Find("Music/Music").GetComponent<Image>().enabled = true;
                }
            }

            if (name == "CompletePanel")
            {
                transform.GetComponent<Animation>().Play();
                ResourceManager.LifeAmount++;
                // for (var i = 1; i <= 3; i++)
                // {
                //     transform.Find("Stars").Find("Star" + i).gameObject.SetActive(false);
                // }
            }

            var videoButton = transform.Find("Image/Video");
            if (videoButton == null) videoButton = transform.Find("Banner/Buttons/Video");
            if (videoButton != null)
            {
#if UNITY_ADS || GOOGLE_MOBILE_ADS || APPODEAL
            //AdsManager.THIS.rewardedVideoZone = "rewardedVideo";

			if (!//AdsManager.THIS.GetRewardedUnityAdsReady ())
				videoButton.gameObject.SetActive (false);
            else
                videoButton.gameObject.SetActive (true);
#else
                videoButton.gameObject.SetActive(false);
#endif
            }
        }

        void Update()
        {
            if (Input.GetKeyUp(KeyCode.Escape))
            {
                if (name == "PlayPanel" || name == "Settings" || name == "BoostInfo" || name == "GemsShop" || name == "LifeRefillPopup" || name == "BuyBoost" || name == "Reward")
                    CloseMenu();
            }
        }

        /// <summary>
        /// show rewarded ads
        /// </summary>
        public void ShowAds()
        {
            if (name == "GemsShop")
                Initiations.Instance.currentReward = RewardsType.GetGems;
            else if (name == "LifeRefillPopup")
                Initiations.Instance.currentReward = RewardsType.GetLifes;
            else if (name == "PreFailedPanel")
                Initiations.Instance.currentReward = RewardsType.GetGoOn;
            //AdsManager.Instance.ShowRewardedAds();
            CloseMenu();
        }

        /// <summary>
        /// Open rate store
        /// </summary>
        public void GoRate()
        {
#if UNITY_ANDROID
            // Application.OpenURL(Initiations.Instance.RateURL);
#elif UNITY_IOS
            // Application.OpenURL(InitScript.Instance.RateURLIOS);
#endif
            PlayerPrefs.SetInt("Rated", 1);
            PlayerPrefs.Save();
            CloseMenu();
        }

        void OnDisable()
        {
            if (transform.Find("Image/Video") != null)
            {
                transform.Find("Image/Video").gameObject.SetActive(true);
            }

            //if( PlayOnEnable )
            //{
            //    if( !GetComponent<SequencePlayer>().sequenceArray[0].isPlaying )
            //        GetComponent<SequencePlayer>().sequenceArray[0].Play
            //}
        }

        /// <summary>
        /// Event on finish animation
        /// </summary>
        public void OnFinished()
        {
            if (name == "CompletePanel")
            {
                StartCoroutine(MenuComplete());
                //StartCoroutine(MenuCompleteScoring());
            }

            if (name == "PlayPanel")
            {
                //InitScript.Instance.currentTarget = InitScript.Instance.targets[PlayerPrefs.GetInt( "OpenLevel" )];
                /*transform.Find("Boosters/Boosters/Boost1").GetComponent<BoostIcon>().InitBoost();
                transform.Find("Boosters/Boosters/Boost2").GetComponent<BoostIcon>().InitBoost();
                transform.Find("Boosters/Boosters/Boost3").GetComponent<BoostIcon>().InitBoost();*/
            }

            if (name == "MenuPause")
            {
                if (MainManager.Instance.gameStatus == GameState.Playing)
                    MainManager.Instance.gameStatus = GameState.Pause;
            }

            if (name == "FailedPanel")
            {
                if (MainManager.Score < MainManager.Instance.levelData.star1)
                {
                    TargetCheck(false, 2);
                }
                else
                {
                    TargetCheck(true, 2);
                }
            }

            if (name == "PrePlay")
            {
                CloseMenu();
                MainManager.Instance.gameStatus = GameState.Tutorial;
                if (MainManager.Instance.levelData.limitType == LIMIT.TIME) CentralSoundManager.Instance.PlayOneShot(CentralSoundManager.Instance.timeOut);
            }

            if (name == "PreFailedPanel")
            {
                transform.Find("Banner/Buttons/Video").gameObject.SetActive(false);
                CloseMenu();
            }

            if (name.Contains("gratzWord"))
                gameObject.SetActive(false);
            if (name == "NoMoreMatches")
                gameObject.SetActive(false);
            if (name == "failed")
                gameObject.transform.parent.gameObject.SetActive(false);
            // if (name == "CompleteLabel")
            //     gameObject.SetActive(false);
        }

        void TargetCheck(bool check, int n = 1)
        {
            var TargetCheck = transform.Find("Image/TargetCheck" + n);
            var TargetUnCheck = transform.Find("Image/TargetUnCheck" + n);
            TargetCheck.gameObject.SetActive(check);
            TargetUnCheck.gameObject.SetActive(!check);
        }

        /// <summary>
        /// Shows rewarded ad button in Prefailed popup
        /// </summary>
        [UsedImplicitly]
        public void WaitForGiveUp()
        {
            if (name == "PreFailedPanel" && MainManager.Instance.gameStatus != GameState.Playing)
            {
                GetComponent<Animation>()["bannerFailed"].speed = 0;
#if UNITY_ADS
			if (//AdsManager.THIS.enableUnityAds) {

				if (//AdsManager.THIS.GetRewardedUnityAdsReady ()) {
					transform.Find ("Banner/Buttons/Video").gameObject.SetActive (true);
				}
			}
#endif
            }
        }

        /// <summary>
        /// Complete popup animation
        /// </summary>
        IEnumerator MenuComplete()
        {
            for (var i = 1; i <= MainManager.Instance.stars; i++)
            {
                //  SoundBase.Instance.audio.PlayOneShot( SoundBase.Instance.scoringStar );
                transform.Find("Image").Find("Star" + i).gameObject.SetActive(true);
                CentralSoundManager.Instance.PlayOneShot(CentralSoundManager.Instance.star[i - 1]);
                yield return new WaitForSeconds(0.5f);
            }
        }

        /// <summary>
        /// Complete popup animation
        /// </summary>
        IEnumerator MenuCompleteScoring()
        {
            var scores = transform.Find("Score").GetComponent<TextMeshProUGUI>();
            for (var i = 0; i <= MainManager.Score; i += 500)
            {
                scores.text = "" + i;
                // SoundBase.Instance.audio.PlayOneShot( SoundBase.Instance.scoring );
                yield return new WaitForSeconds(0.00001f);
            }

            scores.text = "" + MainManager.Score;
        }

        /// <summary>
        /// SHows info popup
        /// </summary>
        public void Info()
        {
            ReferencerUI.Instance.Tutorials.gameObject.SetActive(false);
            ReferencerUI.Instance.Tutorials.gameObject.SetActive(true);
            // OpneMenu(gameObject);
        }


        public void PlaySoundButton()
        {
            MainManager.PlayButtonClickSound();
        }

        public void OpneMenu(GameObject menu)
        {
            if (menu.activeSelf)
                menu.SetActive(false);
            else
                menu.SetActive(true);
        }

        public IEnumerator Close()
        {
            yield return new WaitForSeconds(0.5f);
        }

        public void CloseMenu()
        {
            if (gameObject.name == "MenuPreGameOver")
            {
                ShowGameOver();
            }

            if (gameObject.name == "CompletePanel")
            {
                //            LevelManager.THIS.gameStatus = GameState.Map;
                GlobalValue.CurrentLevel = MainManager.Instance.currentLevel + 1;
                PersistantData.OpenNextLevel = true;
                MainManager.Instance.StartCoroutine(OpenMap());
            }

            if (gameObject.name == "FailedPanel")
            {
                MainManager.Instance.gameStatus = GameState.Map;
            }

            if (SceneManager.GetActiveScene().name == "GameScene")
            {
                if (MainManager.Instance.gameStatus == GameState.Pause)
                {
                    MainManager.Instance.gameStatus = GameState.WaitAfterClose;
                }
            }

            if (gameObject.name == "Settings" && MainManager.GetGameStatus() != GameState.Map)
            {
                BackToMap();
            }
            else if (gameObject.name == "Settings" && MainManager.GetGameStatus() == GameState.Map)
                SceneManager.LoadScene("SplashScene");

            //        SoundBase.Instance.PlayOneShot(SoundBase.Instance.swish[1]);

            gameObject.SetActive(false);
        }

        public void SwishSound()
        {
            CentralSoundManager.Instance.PlayOneShot(CentralSoundManager.Instance.swish[1]);
        }

        public void ShowInfo()
        {
            GameObject.Find("CanvasGlobal").transform.Find("BoostInfo").gameObject.SetActive(true);
        }

        public void Play()
        {
            Debug.LogError("Play  " + gameObject.name);

            CentralSoundManager.Instance.PlayOneShot(CentralSoundManager.Instance.click);
            if (gameObject.name == "MenuPreGameOver")
            {
                if (Initiations.Gems >= 12)
                {
                    Initiations.Instance.SpendGems(12);
                    //                LevelData.LimitAmount += 12;
                    MainManager.Instance.gameStatus = GameState.WaitAfterClose;
                    gameObject.SetActive(false);
                }
                else
                {
                    BuyGems();
                }
            }
            else if (gameObject.name == "FailedPanel")
            {
                MainManager.Instance.gameStatus = GameState.Map;
            }
            else if (gameObject.name == "PlayPanel")
            {
                GUIUtilities.Instance.StartGame();
                CloseMenu();
            }
            else if (gameObject.name == "MenuPause")
            {
                CloseMenu();
                MainManager.Instance.gameStatus = GameState.Playing;
            }
        }

        public void PlayTutorial()
        {
            MainManager.Instance.gameStatus = GameState.Playing;
            //    mainscript.Instance.dropDownTime = Time.time + 0.5f;
            //        CloseMenu();
        }

        public void BackToMap()
        {
            // Time.timeScale = 1;
            // LevelManager.THIS.gameStatus = GameState.GameOver;
            // CloseMenu();
            gameObject.SetActive(false);
            MainManager.Instance.gameStatus = GameState.Map;
            MainManager.Instance.StartCoroutine(OpenMap());
        }

        public void Next()
        {
            CentralSoundManager.Instance.PlayOneShot(CentralSoundManager.Instance.click);

            CloseMenu();
        }

        [UsedImplicitly]
        public void Again()
        {
            CentralSoundManager.Instance.PlayOneShot(CentralSoundManager.Instance.click);
            GameObject gm = new GameObject();
            gm.AddComponent<HandleLevelRestart>();
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void BuyGems()
        {
            Debug.LogError("BuyGems  ");
            CentralSoundManager.Instance.PlayOneShot(CentralSoundManager.Instance.click);
            FindFirstObjectByType<BottomPanelController>().OnMenuButtonClick(4);
            //MenuReference.THIS.GemsShop.gameObject.SetActive(true);
        }

        [UsedImplicitly]
        public void Buy(GameObject pack)
        {
            var i = pack.transform.GetSiblingIndex();
            Initiations.waitedPurchaseGems = int.Parse(pack.transform.Find("Count").GetComponent<TextMeshProUGUI>().text.Replace("x ", ""));
#if UNITY_WEBPLAYER || UNITY_WEBGL
            InitScript.Instance.PurchaseSucceded();
            CloseMenu();
            return;
#endif

            CloseMenu();
        }

        IEnumerator OpenMap()
        {
            Instantiate(Resources.Load("Loading"), transform.parent);
            yield return new WaitForEndOfFrame();
            SceneManager.LoadScene(Resources.Load<MapSwitcher>("Scriptable/MapSwitcher").GetSceneName());
        }

        public void BuyLifeShop()
        {
            Debug.LogError("BuyLifeShop  ");
            CentralSoundManager.Instance.PlayOneShot(CentralSoundManager.Instance.click);
            int lifeCap = 5;
            if (GlobalValue.Life < lifeCap)
                ReferencerUI.Instance.LiveShop.gameObject.SetActive(true);
        }

        public void BuyLife(GameObject button)
        {
            CentralSoundManager.Instance.PlayOneShot(CentralSoundManager.Instance.click);
            if (Initiations.Gems >= int.Parse(button.transform.Find("Price").GetComponent<TextMeshProUGUI>().text))
            {
                Initiations.Instance.SpendGems(int.Parse(button.transform.Find("Price").GetComponent<TextMeshProUGUI>().text));
                Initiations.Instance.RestoreLifes();
                CloseMenu();
            }
            else
            {
                ReferencerUI.Instance.GemsShop.gameObject.SetActive(true);
            }
        }

        public void BuyFailed(GameObject button)
        {
            //        if (GetComponent<Animation>()["bannerFailed"].speed == 0)
            {
                if (Initiations.Gems >= MainManager.Instance.FailedCost)
                {
                    Initiations.Instance.SpendGems(MainManager.Instance.FailedCost);
                    button.GetComponent<Button>().interactable = false;
                    GoOnFailed();
                    GetComponent<Animation>()["bannerFailed"].speed = 1;
                }
                else
                {
                    ReferencerUI.Instance.GemsShop.gameObject.SetActive(true);
                }
            }
        }

        public void GoOnFailed()
        {
            GetComponent<Pre_FailedPanel>().Continue();
        }

        [UsedImplicitly]
        public void GiveUp()
        {
            GetComponent<Pre_FailedPanel>().Close();
        }

        void ShowGameOver()
        {
            CentralSoundManager.Instance.PlayOneShot(CentralSoundManager.Instance.gameOver[1]);

            GameObject.Find("Canvas").transform.Find("MenuGameOver").gameObject.SetActive(true);
            gameObject.SetActive(false);
        }

        #region boosts

        public void BuyBoost(BoostType boostType, int price, int count, Action callback)
        {
            CentralSoundManager.Instance.PlayOneShot(CentralSoundManager.Instance.click);
            if (Initiations.Gems >= price)
            {
                Initiations.Instance.SpendGems(price);
                Initiations.Instance.BuyBoost(boostType, price, count);
                callback?.Invoke();
                //InitScript.Instance.SpendBoost(boostType);
                CloseMenu();
            }
            else
            {
                BuyGems();
            }
        }

        #endregion

        public void SoundOff(GameObject Off)
        {
            if (!Off.activeSelf)
            {
                //            SoundBase.Instance.volume = 0;
                CentralSoundManager.Instance.audioMixer.SetFloat("SoundVolume", -80);
                //on.GetComponent<Image>().enabled = false;
                Off.SetActive(true);
            }
            else
            {
                //            SoundBase.Instance.volume = 1;
                CentralSoundManager.Instance.audioMixer.SetFloat("SoundVolume", 1);

                Off.SetActive(false);
            }

            float vol;
            CentralSoundManager.Instance.audioMixer.GetFloat("SoundVolume", out vol);
            PlayerPrefs.SetInt("Sound", (int)vol);
            PlayerPrefs.Save();
        }

        public void MusicOff(GameObject Off)
        {
            if (!Off.activeSelf)
            {
                //GameObject.Find("Music").GetComponent<AudioSource>().volume = 0;
                CentralMusicManager.Instance.audioMixer.SetFloat("MusicVolume", -80);
                //on.GetComponent<Image>().enabled = false;

                Off.SetActive(true);
            }
            else
            {
                //GameObject.Find("Music").GetComponent<AudioSource>().volume = 1;
                CentralMusicManager.Instance.audioMixer.SetFloat("MusicVolume", 1);

                Off.SetActive(false);
            }

            float vol;
            CentralMusicManager.Instance.audioMixer.GetFloat("MusicVolume", out vol);
            PlayerPrefs.SetInt("Music", (int)vol);
            PlayerPrefs.Save();
        }
    }
}
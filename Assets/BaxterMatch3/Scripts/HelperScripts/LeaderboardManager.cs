using UnityEngine;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using BaxterMatch3.Animation.Directory.AnimationUI.Demo;
using Nakama;
using Nakama.TinyJson;
using NakamaOnline;
using Internal.Scripts.MapScripts;
using UnityEngine.UI;

namespace HelperScripts
{
    public class LeaderboardManager : MonoBehaviour
    {
        public Transform leaderboardContent;
        public GameObject leaderboardItem; //, leaderboardItemSelf;
        public ScrollRect scrollRect;
        public GameObject loading;
        [SerializeField] private GameObject itemUp, itemDown, offlineUi;
        private List<int> ranks;
        private List<int> scores;
        private List<int> subScores;
        private List<string> names;
        private int _ourRank;

        private NakamaController nakamaServer;

        //private void OnEnable()
        //{
        //    // Check if the user is banned or not.
        //    if (GameManager.bonusFightersAuto.Count + GameManager.bonusArchersAuto.Count > 2000)
        //    {
        //        _ = NakamaController.ins.BanUser();
        //    }
        //}

        private void OnEnable()
        {
            MapCamera.IsPopupOpen = true;
        }

        private void OnDisable()
        {
            MapCamera.IsPopupOpen = false;
        }

        private void Start()
        {
            //if (GameManager.currentLevel == 9 && PlayerPrefs.GetInt(Save.LeaderboardTutorial.ToString(), 0) != 1) TutorialManager.currentTutorial++;
            nakamaServer = FindObjectOfType<NakamaController>();
            ShowRanks();
        }

        private void SetupNotConnectedUI()
        {
            //StartCoroutine(FindObjectOfType<GameManager>().ShowMassage("Please Check your Internet connection!"));
            loading.SetActive(false);
            offlineUi.SetActive(true);
        }

        public void Retry()
        {
            offlineUi.SetActive(false);
            loading.SetActive(true);
            ShowRanks();
        }

        private async void ShowRanks()
        {
            Debug.LogError("ShowRanks  ");
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                Debug.LogError("ShowRanks 1  ");
                SetupNotConnectedUI();
                return;
            }

            Debug.LogError("ShowRanks  ");
            if (!await nakamaServer.SocketConnectionCheck())
            {
                Debug.LogError("ShowRanks 2  ");
                SetupNotConnectedUI();
                return;
            }

            Debug.LogError("ShowRanks  ");
            var ourResult = await nakamaServer.WriteLeaderboardRecordAsync();
            if (ourResult == null)
            {
                Debug.LogError("ShowRanks 3  ");
                SetupNotConnectedUI();
                return;
            }

            Debug.LogError("ShowRanks  ");
            _ourRank = Int32.Parse(ourResult.Rank);
            foreach (Transform child in leaderboardContent) Destroy(child.gameObject);
            ranks = new List<int>();
            scores = new List<int>();
            subScores = new List<int>();
            names = new List<string>();
            var result = await nakamaServer.ListLeaderboardRecordsAsync(100);
            if (result == null)
            {
                Debug.LogError("ShowRanks 4  ");
                SetupNotConnectedUI();
                return;
            }

            foreach (IApiLeaderboardRecord player in result.Records)
            {
                if (player.Rank == null || player.Score == null || player.Subscore == null)
                    continue;
                ranks.Add(Int32.Parse(player.Rank));
                scores.Add(Int32.Parse(player.Score));
                subScores.Add(Int32.Parse(player.Subscore));
                names.Add(player.Metadata.FromJson<Dictionary<string, string>>()["n"]);
            }

            bool flag = true;
            GameObject item;
            int spriteNumber;
            for (int i = 0; i < ranks.Count; i++)
            {
                spriteNumber = flag ? 1 : 2;
                if (ranks[i] == _ourRank)
                {
                    spriteNumber = 3;

                    var t = Instantiate(leaderboardItem, itemUp.transform);
                    t.GetComponent<LeaderboardItemManager>().SetItem(spriteNumber, ranks[i], GameManager.Username, scores[i].ToString(), subScores[i].ToString());
                    t = Instantiate(leaderboardItem, itemDown.transform);
                    t.GetComponent<LeaderboardItemManager>().SetItem(spriteNumber, ranks[i], GameManager.Username, scores[i].ToString(), subScores[i].ToString());

                    item = Instantiate(leaderboardItem, leaderboardContent);
                    var itemSelf = item.GetComponent<LeaderboardItemManager>();
                    itemSelf.SetItem(spriteNumber, ranks[i], GameManager.Username, scores[i].ToString(), subScores[i].ToString());
                    //itemSelf.SetUpAndDownObjects(itemUp, itemDown);

                    //item.GetComponent<BoxCollider2D>().enabled = true;
                }
                else
                {
                    flag = !flag;
                    item = Instantiate(leaderboardItem, leaderboardContent);
                    item.GetComponent<LeaderboardItemManager>().SetItem(spriteNumber, ranks[i], names[i], scores[i].ToString(), subScores[i].ToString());
                }
            }

            loading.SetActive(false);
            Debug.LogError("ShowRanks <= 100 end");
            if (_ourRank <= 100) return;
            var fake = Instantiate(leaderboardItem, leaderboardContent);
            fake.AddComponent<CanvasGroup>().alpha = 0;
            var rectFake = fake.GetComponent<RectTransform>();
            rectFake.sizeDelta = new Vector2(rectFake.sizeDelta.x, rectFake.sizeDelta.y - 10f);
            var tt = Instantiate(leaderboardItem, itemDown.transform);
            tt.GetComponent<LeaderboardItemManager>().SetItem(3, _ourRank, GameManager.Username, GameManager.CurrentLevel.ToString(), /*ourResult.Subscore.ToString()*/55.ToString());
            itemDown.SetActive(true);
            await Task.Delay(20);
            leaderboardContent.localPosition = new Vector2(leaderboardContent.localPosition.x, -8000);
            Debug.LogError("ShowRanks  end");
        }
    }
}
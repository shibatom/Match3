

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Internal.Scripts;
using Internal.Scripts.Items;
using Internal.Scripts.System;
using Internal.Scripts.System.Combiner;
using TMPro;
using UnityEngine;

namespace Internal.Scripts.GUI
{
    public class TutorialManager : MonoBehaviour
    {
        public static List<Item> items = new List<Item>();
        public GameObject tutorial;
        public GameObject text;
        public GameObject canvas;
        bool showed;

        public GameObject[] tutorials;
        private bool checkStarted;

        void OnEnable()
        {
            tutorial.SetActive(false);
            MainManager.OnWaitForTutorial += Check;
            MainManager.OnSublevelChanged += Check;
            MainManager.OnStartPlay += DisableTutorial;
        }

        void OnDisable()
        {
            MainManager.OnWaitForTutorial -= Check;
            MainManager.OnSublevelChanged -= Check;
            MainManager.OnStartPlay -= DisableTutorial;
        }

        void DisableTutorial()
        {
            if (showed)
            {
                ChangeLayerNum(0);
                tutorial.SetActive(false);
                showed = true;
                OnDisable();
            }

            MainManager.Instance.gameStatus = GameState.Playing;
        }

        void Check()
        {
            if (!checkStarted && !showed)
                StartCoroutine(CheckTutorial());
        }

        IEnumerator CheckTutorial()
        {
            checkStarted = true;
            object tutorialType = null;
            tutorialType = IsTutorialRequirePregeneration();
            if (tutorialType != null)
            {
                do
                {
                    if (MainManager.Instance.gameStatus == GameState.ChangeSubLevel)
                        yield return new WaitUntilTheSubLevelIsChanged();
                    if ((ItemsTypes)tutorialType == ItemsTypes.TimeBomb)
                    {
                        tutorialType = ItemsTypes.NONE;
                        text.GetComponent<TextMeshProUGUI>().text = "Time bomb should be destroyed before the counter reaches 0";
                    }

                    FillTutorial(tutorialType);
                    if (items.Count > 0 && MainManager.Instance.gameStatus != GameState.ChangeSubLevel)
                    {
                        if (MainManager.Instance.gameStatus != GameState.Tutorial)
                            MainManager.Instance.gameStatus = GameState.Tutorial;
                        CheckNewTarget(tutorialType);
                    }

                    yield return new WaitingFortheNextMove();
                } while (!showed);
            }
            else
                MainManager.Instance.gameStatus = GameState.ButlersGifts;
            //LevelManager.THIS.gameStatus = GameState.Playing;
        }


        void CheckNewTarget(object tutorialType)
        {
            if ((ItemsTypes)tutorialType == ItemsTypes.NONE && !showed)
            {
                //StartCoroutine(AI.THIS.CheckPossibleCombines());
                StartCoroutine(WaitForCombine());
            }
            else if (MainManager.Instance.currentLevel < tutorials.Length && !showed)
            {
                text.GetComponent<TextMeshProUGUI>().text = "Combine candies this way to get bonus item!";
                ShowItems();
            }

            // tutorials[LevelManager.This.currentLevel - 1].SetActive(true);
        }

        void ShowStarsTutorial()
        {
            tutorial.SetActive(true);
            ChangeLayerNum(4);
            showed = true;
        }

        IEnumerator WaitForCombine()
        {
            yield return new WaitUntil(() => ArtificialIntelligence.Instance.GetCombine() != null);
            items = ArtificialIntelligence.Instance.GetCombine();
            if (items.Count == 0)
                yield break;
            ShowItems();
        }

        private void ShowItems()
        {
            items.Sort(SortByDistance);
            if (!showed)
            {
                ShowStarsTutorial();
            }
        }

        public Vector3[] GetItemsPositions()
        {
            var positions = new Vector3[items.Count];
            for (var i = 0; i < items.Count; i++)
            {
                positions[i] = items[i].transform.position + new Vector3(1, -1, 0);
            }

            return positions;
        }

        private int SortByDistance(Item item1, Item item2)
        {
            var itemFirst = items[0];
            var x = Vector3.Distance(itemFirst.transform.position, item1.transform.position);
            var y = Vector3.Distance(itemFirst.transform.position, item2.transform.position);
            var retval = y.CompareTo(x);

            if (retval != 0)
            {
                return retval;
            }

            return y.CompareTo(x);
        }

        public int FindMaxY(List<Item> list)
        {
            var max = int.MinValue;
            foreach (var type in list)
            {
                if (type.transform.position.y > max)
                {
                    max = (int)type.transform.position.y + 2;
                }
            }

            return max;
        }

        void ChangeLayerNum(int num)
        {
            foreach (var item in items)
            {
                if (item)
                {
                    // item.square.GetComponent<SpriteRenderer>().sortingLayerName = num>0 ? "UI" : "Default";
                    // item.square.GetComponent<SpriteRenderer>().sortingOrder = num;
                    item.SprRenderer.ToList().ForEach(i =>
                    {
                        i.sortingLayerName = num > 0 ? "UI" : "Default";
                        i.sortingOrder = num + 4;
                    });
                }
            }
        }

        public static object IsTutorialRequirePregeneration()
        {
            switch (MainManager.Instance.levelData.selectedTutorial)
            {
                case 1:
                    return ItemsTypes.NONE;
                case 2:
                    return ItemsTypes.RocketHorizontal;
                case 3:
                    return ItemsTypes.Bomb;
                case 5:
                    return ItemsTypes.TimeBomb;
            }

            return null;
        }

        void FillTutorial(object type)
        {
            items.Clear();
            items = AbstractBonusItemPrediction.IsItemPredicted((ItemsTypes)type);
        }
    }
}
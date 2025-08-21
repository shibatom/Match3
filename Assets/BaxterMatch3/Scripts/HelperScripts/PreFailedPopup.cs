using System;
using Internal.Scripts;
using Internal.Scripts.Items;
using Internal.Scripts.System;
using TMPro;
using UnityEngine;

namespace HelperScripts
{
    public class PreFailedPopup : MonoBehaviour
    {
        [SerializeField] private TMP_Text coinCountText;
        [SerializeField] private TMP_Text playOnCostText;
        [SerializeField] private TMP_Text addedMovesText;


        private void Start()
        {
            coinCountText.text = GlobalValue.Coin.ToString();
            playOnCostText.text = "100";
            addedMovesText.text = MainManager.Instance.ExtraFailedMoves.ToString();
        }

        public void PlayOn()
        {
            if (GlobalValue.SpendItem(CurrencyType.Coin, 100))
                Continue();
            else
            {
                ReferencerUI.Instance.OnToggleShop?.Invoke(false);

                ReferencerUI.Instance.inGameCoinShop.SetActive(true);
            }
        }

        public void SetFailed()
        {
            gameObject.SetActive(true);
        }

        /*public void SetFailed()
        {
            objects[0].SetActive(true);
            if (LevelManager.THIS.levelData.limitType == LIMIT.TIME)
                objects[0].GetComponent<Image>().sprite = spriteTime;
            objects[1].SetActive(false);
        }*/

        /*public void SetBombFailed()
        {
            objects[1].SetActive(true);
            objects[0].SetActive(false);
        }*/

        /// <summary>
        /// Continue the game after choose a variant
        /// </summary>
        public void Continue()
        {
            /*if (IsFail())
            {*/
            ContinueFailed();
            ContinueBomb();
            // }
            // else ContinueBomb();
            gameObject.SetActive(false);
            MainManager.Instance.gameStatus = GameState.Playing;

            //AnimAction(() => LevelManager.THIS.gameStatus = GameState.Playing);
        }

        /// <summary>
        /// Further animation and game over
        /// </summary>
        public void Close()
        {
            MainManager.Instance.gameStatus = GameState.GameOver;
            gameObject.SetActive(false);
            /*var timeBombs = FindObjectsOfType<itemTimeBomb>().Where(i => i.timer <= 0);
            if (timeBombs.Count() > 0)
            {
                timeBombs.NextRandom().OnExlodeAnimationFinished += () => LevelManager.THIS.gameStatus = GameState.GameOver;
                AnimAction(() =>
                {
                    for (var index = 0; index < timeBombs.Count(); index++)
                    {
                        var i = timeBombs.ToList()[index];
                        i.ExlodeAnimation(index != 0, null);
                    }
                });
            }
            else AnimAction(() => LevelManager.THIS.gameStatus = GameState.GameOver);*/
        }

        void AnimAction(Action call)
        {
            Animation anim = GetComponent<Animation>();
            var animationState = anim["bannerFailed"];
            animationState.speed = 1;
            anim.Play();
            LeanTween.delayedCall(anim.GetClip("bannerFailed").length - animationState.time, call);
        }

        //private bool IsFail() => objects[0].activeSelf;

        void ContinueBomb()
        {
            FindObjectsOfType<TimeBombItem>().ForEachY(i =>
            {
                i.timer += 5;
                i.InitItem();
            });
        }

        /// <summary>
        /// Continue the game
        /// </summary>
        private void ContinueFailed()
        {
            if (MainManager.Instance.levelData.limitType == LIMIT.MOVES)
            {
                MainManager.Instance.ChangeCounter(MainManager.Instance.ExtraFailedMoves);
                MainManager.Instance.GameRestartOrGiveNEwHealth();
            }

            else
                MainManager.Instance.levelData.limit += MainManager.Instance.ExtraFailedSecs;
        }
    }
}
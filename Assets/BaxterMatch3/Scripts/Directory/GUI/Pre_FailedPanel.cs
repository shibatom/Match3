

using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Internal.Scripts.Items;
using Internal.Scripts.System;

namespace Internal.Scripts.GUI
{
    /// <summary>
    /// Pre_Failed Panel
    /// </summary>
    public class Pre_FailedPanel : MonoBehaviour
    {
        public Sprite spriteTime;
        public GameObject[] objects;

        /// <summary>
        /// Initialization
        /// </summary>
        public void SetFailed()
        {
            objects[0].SetActive(true);
            if (MainManager.Instance.levelData.limitType == LIMIT.TIME)
                objects[0].GetComponent<Image>().sprite = spriteTime;
            objects[1].SetActive(false);
        }

        public void SetBombFailed()
        {
            objects[1].SetActive(true);
            objects[0].SetActive(false);
        }

        /// <summary>
        /// Continue the game after choose a variant
        /// </summary>
        public void Continue()
        {
            if (IsFail())
            {
                ContinueFailed();
                ContinueBomb(); //sss
            }
            else ContinueBomb();

            AnimAction(() => MainManager.Instance.gameStatus = GameState.Playing);
        }

        /// <summary>
        /// Further animation and game over
        /// </summary>
        public void Close()
        {
            var timeBombs = FindObjectsOfType<TimeBombItem>().Where(i => i.timer <= 0);
            if (timeBombs.Count() > 0)
            {
                timeBombs.NextRandom().OnExlodeAnimationFinished += () => MainManager.Instance.gameStatus = GameState.GameOver;
                AnimAction(() =>
                {
                    for (var index = 0; index < timeBombs.Count(); index++)
                    {
                        var i = timeBombs.ToList()[index];
                        i.ExplodeAnimation(index != 0, null);
                    }
                });
            }
            else AnimAction(() => MainManager.Instance.gameStatus = GameState.GameOver);
        }

        void AnimAction(Action call)
        {
            Animation anim = GetComponent<Animation>();
            var animationState = anim["bannerFailed"];
            animationState.speed = 1;
            anim.Play();
            LeanTween.delayedCall(anim.GetClip("bannerFailed").length - animationState.time, call);
        }

        private bool IsFail() => objects[0].activeSelf;

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
                MainManager.Instance.levelData.limit += MainManager.Instance.ExtraFailedMoves;
            else
                MainManager.Instance.levelData.limit += MainManager.Instance.ExtraFailedSecs;
        }
    }
}
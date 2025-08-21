using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace NakamaOnline
{
    public class NakamaUi : MonoBehaviour
    {
        [SerializeField] private GameObject _matchmakingCover;
        [SerializeField] private GameObject _disconnectedCover;
        [SerializeField] private Image[] _playersImageStates;
        [SerializeField] private GameObject _readyButton;
        [SerializeField] private GameObject _matchmakingButton;

        public Text remainingText;
        public int remainingTime = 15;
        private MatchMaker _matchMaker;
        private BattleController _battleController;
        private IEnumerator countdownEnumerator;


        public void FindMatchButton()
        {
            /*_matchmakingCover.SetActive(true);
            (_matchMaker ??= GetComponent<MatchMaker>()).StartMatchmaking();
            (_gameManager ??= FindObjectOfType<GameManager>()).playButtons.SetActive(false);*/
        }

        public void CancelMatchButton()
        {
            _matchmakingCover.SetActive(false);
            //_gameManager.playButtons.SetActive(true);
        }

        public void TogglePlayerStatusButton()
        {
            (_battleController ??= GetComponent<BattleController>()).TogglePlayerStatus();
        }

        public IEnumerator MatchFound()
        {
            yield return new WaitForSeconds(1);
            _matchmakingCover.SetActive(false);
            ActiveStatus(true);
        }

        public void ActiveStatus(bool active)
        {
            _readyButton.SetActive(active);
            _matchmakingButton.SetActive(!active);
        }

        public void ColorStatus(int local, int other)
        {
            _playersImageStates[0].color = local == 1 ? Color.white : Color.red;
            _playersImageStates[1].color = other == 1 ? Color.white : Color.red;
        }

        public static void LeaveOngoingMatch()
        {
            var ui = FindObjectOfType<NakamaUi>();
            //ui.StartCoroutine(Leave());
            ui.StopCounterDown();
        }

        /*static IEnumerator Leave()
        {
            var ui = FindObjectOfType<NakamaUi>();
            var ds = ui._disconnectedCover;
            ds.SetActive(true);
            yield return new WaitForSeconds(3f);
            FindObjectOfType<GameManager>().playButtons.SetActive(true);
            ui.ActiveStatus(false);
            LevelManager.Instance.LoadLevel();
            ds.SetActive(false);
        }*/

        public void StartCountdown()
        {
            countdownEnumerator = ReadyCountdown();
            StartCoroutine(countdownEnumerator);
        }

        private IEnumerator ReadyCountdown()
        {
            var time = remainingTime;
            while (time > 0)
            {
                remainingText.text = time.ToString();
                yield return new WaitForSecondsRealtime(1f);
                time--;
            }

            remainingText.text = "Times up";
            yield return new WaitForSecondsRealtime(1f);
            remainingText.text = String.Empty;
            (_battleController ??= GetComponent<BattleController>()).CountdownFinished();
            ActiveStatus(false);
        }

        public void StopCounterDown()
        {
            StopCoroutine(countdownEnumerator);
            remainingText.text = String.Empty;
        }
    }
}
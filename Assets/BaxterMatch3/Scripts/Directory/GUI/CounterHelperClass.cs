

using System.Collections;
using System.Linq;
using HelperScripts;
using Internal.Scripts.Level;
using TMPro;
using UnityEngine;

namespace Internal.Scripts.GUI
{
    /// <summary>
    /// various GUi counters
    /// </summary>
    public class CounterHelperClass : MonoBehaviour
    {
        private TextMeshProUGUI _txt;
        private float _lastTime;
        private bool _alert;

        private LevelData _thisLevelData;

        public LevelData ThisLevelData
        {
            get
            {
                if (_thisLevelData == null) _thisLevelData = LevelData.THIS;
                return _thisLevelData;
            }
            set => _thisLevelData = value;
        }

        void Awake()
        {
            _txt = GetComponent<TextMeshProUGUI>();
        }

        private void Start()
        {
            ThisLevelData = MainManager.Instance.levelData;
        }

        void OnEnable()
        {
            _lastTime = 0;
            UpdateText();
            _alert = false;
            if (name == "Limit") StartCoroutine(TimeTick());
        }

        IEnumerator UpdateRare()
        {
            while (true)
            {
                if (_txt == null) continue;

                UpdateText();
                yield return new WaitForSeconds(0.5f);
            }
        }

        public void UpdateText()
        {
            if (name == "Score")
            {
                _txt.text = "" + MainManager.Score;
                //GlobalValue.Coin = MainManager.Score;
            }

            if (name == "BestScore")
            {
                _txt.text = "Best score:" + PlayerPrefs.GetInt("Score" + GlobalValue.CurrentLevel);
            }

            if (name == "Limit" && ThisLevelData != null)
            {
                if (ThisLevelData.limitType == LIMIT.MOVES)
                {
                    // Debug.LogError($"ThisLevelData.limit: {ThisLevelData.limit}");
                    _txt.text = "" + Mathf.Clamp(MainManager.Instance.Limit, 0, MainManager.Instance.Limit);
                    //Debug.LogError($"txt.text: {txt.text}");
                    _txt.transform.localScale = Vector3.one;
                    if (MainManager.Instance.Limit <= 3)
                    {
                        _txt.color = Color.red;
                        _txt.outlineColor = Color.white;
                        if (!_alert)
                        {
                            _alert = true;
                            //TopBarAnimationController.OnTopBarStateChange(TopBarAnimationState.Worried);
//                            SoundBase.Instance.PlayOneShot(SoundBase.Instance.alert);
                        }
                    }
                    else
                    {
                        _alert = false;
                        _txt.color = Color.white;
                        // txt.GetComponent<Outline>().effectColor = new Color(148f / 255f, 61f / 255f, 95f / 255f);
                    }
                }
                else
                {
                    var minutes = Mathf.FloorToInt(ThisLevelData.limit / 60F);
                    var seconds = Mathf.FloorToInt(ThisLevelData.limit - minutes * 60);
                    _txt.text = "" + $"{minutes:00}:{seconds:00}";
                    _txt.transform.localScale = Vector3.one * 0.68f;
                    _txt.fontSize = 80;
                    if (ThisLevelData.limit <= 5 && MainManager.Instance.gameStatus == GameState.Playing)
                    {
                        // txt.color = new Color(216f / 255f, 0, 0);
                        // txt.outlineColor = Color.white;
                        if (_lastTime + 5 < Time.time)
                        {
                            _lastTime = Time.time;
                            CentralSoundManager.Instance.PlayOneShot(CentralSoundManager.Instance.timeOut);
                        }
                    }
                    else
                    {
                        _txt.color = Color.white;
                        _txt.outlineColor = new Color(148f / 255f, 61f / 255f, 95f / 255f);
                    }
                }
            }

            if (name == "Lifes")
            {
                _txt.text = "" + Initiations.Instance?.GetLife();
            }

            if (name == "Coin")
            {
                _txt.text = "" + GlobalValue.Coin;
            }

            if (name == "FailedCount")
            {
                if (ThisLevelData.limitType == LIMIT.MOVES)
                    _txt.text = "+" + MainManager.Instance.ExtraFailedMoves;
                else
                    _txt.text = "+" + MainManager.Instance.ExtraFailedSecs;
            }

            if (name == "FailedPrice")
            {
                _txt.text = "" + MainManager.Instance.FailedCost;
            }

            if (name == "FailedDescription")
            {
                _txt.text = "" + LevelData.THIS.GetTargetCounters().First(i => !i.IsTotalTargetReached()).targetLevel.GetFailedDescription();
            }


            if (name == "Gems")
            {
                _txt.text = "" + Initiations.Gems;
            }

            if (name == "TargetScore")
            {
                _txt.text = "" + ThisLevelData.star1;
            }

            if (name == "Level")
            {
                _txt.text = "Level " + GlobalValue.CurrentLevel;
            }
        }

        IEnumerator TimeTick()
        {
            while (true)
            {
                if (MainManager.Instance == null)
                {
                    yield return new WaitForSeconds(1);
                    continue;
                }

                if (MainManager.Instance.gameStatus == GameState.Playing)
                {
                    if (_thisLevelData.limitType == LIMIT.TIME)
                    {
                        _thisLevelData.limit--;
                        if (!MainManager.Instance.DragBlocked)
                            MainManager.Instance.CheckWinLose();
                    }
                }

                if (MainManager.Instance.gameStatus == GameState.Map)
                    yield break;
                yield return new WaitForSeconds(1);
            }
        }
    }
}
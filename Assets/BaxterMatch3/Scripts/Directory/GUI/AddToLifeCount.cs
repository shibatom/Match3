

using System;
using HelperScripts;
using Internal.Scripts.Localization;
using Internal.Scripts.System;
using TMPro;
using UnityEngine;

namespace Internal.Scripts.GUI
{
    public class AddToLifeCount : MonoBehaviour
    {
        private TextMeshProUGUI _text;
        private static float _timeLeft;
        private float _totalTimeForRestLife = 15f * 60; //8 minutes for restore life
        private bool _startTimer;

        private DateTime _templateTime;

        // Use this for initialization
        private void Start()
        {
            _text = GetComponent<TextMeshProUGUI>();
            _totalTimeForRestLife = Initiations.Instance.TotalTimeForRestLifeHours * 60 * 60 + Initiations.Instance.TotalTimeForRestLifeMin * 60 + Initiations.Instance.TotalTimeForRestLifeSec;
        }

        private bool CheckPassedTime()
        {
            Initiations.DateOfExit = PlayerPrefs.GetString("DateOfExit", "");
            Initiations.RestLifeTimer = PlayerPrefs.GetFloat("RestLifeTimer");

            if (Initiations.DateOfExit == "" || Initiations.DateOfExit == default(DateTime).ToString())
                Initiations.DateOfExit = OnlineTime.THIS.serverTime.ToString();

            var dateOfExit = DateTime.Parse(Initiations.DateOfExit);
            float secondsPassedFromLastExit = (float)OnlineTime.THIS.serverTime.Subtract(dateOfExit).TotalSeconds;
            if (secondsPassedFromLastExit > _totalTimeForRestLife * (Initiations.Instance.CapOfLife - GlobalValue.Life))
            {
                Initiations.Instance.RestoreLifes();
                Initiations.RestLifeTimer = 0;
                return false; ///we dont need lifes
            }

            Initiations.Instance.AddLife((int)Math.Floor(secondsPassedFromLastExit / _totalTimeForRestLife));
            Initiations.RestLifeTimer -= secondsPassedFromLastExit;
            return true; ///we need lifes
        }

        private void TimeCount(float tick)
        {
            Initiations.RestLifeTimer -= tick;
            if (Initiations.RestLifeTimer <= 1 && GlobalValue.Life < Initiations.Instance.CapOfLife)
            {
                Initiations.Instance.AddLife(1);
                ResetTimer();
            }

            if (Initiations.RestLifeTimer <= 0)
                ResetTimer();
        }

        public void ResetTimer()
        {
            Initiations.RestLifeTimer += _totalTimeForRestLife;
        }


        private void Update()
        {
            if (!_startTimer && OnlineTime.THIS.dateReceived && OnlineTime.THIS.serverTime.Subtract(OnlineTime.THIS.serverTime).Days == 0)
            {
                if (GlobalValue.Life < Initiations.Instance.CapOfLife)
                {
                    if (CheckPassedTime())
                        _startTimer = true;
                }
            }


            TimeCount(Time.deltaTime);

            if (gameObject.activeSelf)
            {
                if (GlobalValue.Life < Initiations.Instance.CapOfLife)
                {
                    if (Initiations.Instance.TotalTimeForRestLifeHours > 0)
                    {
                        var hours = Mathf.FloorToInt(Initiations.RestLifeTimer / 3600);
                        var minutes = Mathf.FloorToInt((Initiations.RestLifeTimer - hours * 3600) / 60);
                        var seconds = Mathf.FloorToInt((Initiations.RestLifeTimer - hours * 3600) - minutes * 60);

                        _text.enabled = true;
                        _text.text = "" + string.Format("{0:00}:{1:00}:{2:00}", hours, minutes, seconds);
                    }
                    else
                    {
                        var minutes = Mathf.FloorToInt(Initiations.RestLifeTimer / 60F);
                        var seconds = Mathf.FloorToInt(Initiations.RestLifeTimer - minutes * 60);

                        _text.enabled = true;
                        _text.text = "" + string.Format("{0:00}:{1:00}", minutes, seconds);
                    }

                    //				//	text.text = "+1 in \n " + Mathf.FloorToInt( MainMenu.RestLifeTimer/60f) + ":" + Mathf.RoundToInt( (MainMenu.RestLifeTimer/60f - Mathf.FloorToInt( MainMenu.RestLifeTimer/60f))*60f);
                }
                else
                {
                    //text.text = "   Full";
                    _text.text = LanguageManager.GetText(38, "FULL");
                }
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            SetFocus(!pauseStatus);
        }

        private void SetFocus(bool focus)
        {
            if (!focus)
                SaveExitTime();
            else
                _startTimer = false;
        }

        private static void SaveExitTime()
        {
            Initiations.DateOfExit = OnlineTime.THIS.serverTime.ToString();
            PlayerPrefs.SetString("DateOfExit", OnlineTime.THIS.serverTime.ToString());
            PlayerPrefs.SetFloat("RestLifeTimer", Initiations.RestLifeTimer);
            PlayerPrefs.Save();
        }

        void OnApplicationQuit()
        {
            SaveExitTime();
        }
    }
}
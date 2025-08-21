using System;
using System.Collections;
using System.Globalization;
using TMPro;
using UnityEngine;


namespace HelperScripts
{
    public class ResourceManager : MonoBehaviour
    {
        public static ResourceManager Instance;
        public static Action OnCoinAmountChange;

        [SerializeField] private TMP_Text coinAmountText;
        [SerializeField] private TMP_Text lifeAmountText;
        [SerializeField] private TMP_Text lifeAmountRefillText;
        [SerializeField] private TMP_Text lifeTimerText;
        [SerializeField] private TMP_Text lifeRefillTimerText;
        [SerializeField] [Range(1, 60)] private int lifeTimeIntervalInMinutes = 2;

        private static Coroutine _lifeCoroutine;
        private readonly WaitForSeconds _oneSecondWait = new(1);

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else DestroyImmediate(gameObject);
        }

        private void Start()
        {
            OnCoinAmountChange = UpdateCoinUI;
            UpdateCoinUI();
            LifeAmount = InitializeLife();
        }

        // private void Update()
        // {
        //     if (Input.GetKeyUp(KeyCode.A))
        //     {
        //         LifeAmount--;
        //     }
        // }

        public static int LifeAmount
        {
            // get => PlayerPrefs.GetInt("Life",5);
            get => GlobalValue.Life;
            set
            {
                if (value >= 5)
                {
                    Instance.SetLifeToMax();
                    return;
                }

                if (value < 0)
                {
                    value = 0;
                }

                PlayerPrefs.SetInt("Life", value);
                if (_lifeCoroutine == null)
                    _lifeCoroutine = Instance.StartCoroutine(Instance.RegenerateLifeOverTime());
            }
        }

        private int InitializeLife()
        {
            if (string.IsNullOrEmpty(GlobalValue.LifeLastAddedTime))
            {
                GlobalValue.LifeLastAddedTime = DateTime.Now.ToUniversalTime().ToString(CultureInfo.InvariantCulture);
                return 5;
            }

            int currentLife = GlobalValue.Life;
            lifeAmountText.text = currentLife.ToString();
            if (currentLife >= 5)
                return 5;

            DateTime lastAddedTime = DateTime.Parse(PlayerPrefs.GetString("LifeLastAddedTime"), CultureInfo.InvariantCulture);
            TimeSpan elapsedTime = DateTime.Now.ToUniversalTime() - lastAddedTime;
            int regeneratedLives = Mathf.Max(0, Mathf.FloorToInt((float)elapsedTime.TotalMinutes / lifeTimeIntervalInMinutes));
            int newLifeAmount = Mathf.Min(currentLife + regeneratedLives, 5);
            if (newLifeAmount < 5)
            {
                GlobalValue.LifeLastAddedTime = lastAddedTime.AddMinutes(regeneratedLives * lifeTimeIntervalInMinutes).ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                GlobalValue.LifeLastAddedTime = DateTime.Now.ToUniversalTime().ToString(CultureInfo.InvariantCulture);
            }

            return newLifeAmount;
        }

        private IEnumerator RegenerateLifeOverTime()
        {
            while (LifeAmount < 5)
            {
                DateTime lastAddedTime = DateTime.Parse(GlobalValue.LifeLastAddedTime, CultureInfo.InvariantCulture);
                TimeSpan elapsedTime = DateTime.Now.ToUniversalTime() - lastAddedTime;
                if (elapsedTime >= TimeSpan.FromMinutes(lifeTimeIntervalInMinutes))
                {
                    LifeAmount++;
                    GlobalValue.LifeLastAddedTime = DateTime.Now.ToUniversalTime().ToString(CultureInfo.InvariantCulture);
                }
                else
                {
                    TimeSpan timeRemaining = TimeSpan.FromMinutes(lifeTimeIntervalInMinutes) - elapsedTime;
                    lifeRefillTimerText.text = lifeTimerText.text = $"{(int)timeRemaining.TotalMinutes}:{timeRemaining.Seconds:D2}";
                }

                lifeAmountRefillText.text = lifeAmountText.text = LifeAmount.ToString();
                yield return _oneSecondWait;
            }

            SetLifeToMax();
        }

        private void SetLifeToMax()
        {
            StopCoroutineIfRunning();
            GlobalValue.Life = 5;
            GlobalValue.LifeLastAddedTime = DateTime.Now.ToUniversalTime().ToString(CultureInfo.InvariantCulture);
            lifeAmountText.text = "5";
            lifeTimerText.text = "Full";
        }

        private void StopCoroutineIfRunning()
        {
            if (_lifeCoroutine != null)
            {
                StopCoroutine(_lifeCoroutine);
                _lifeCoroutine = null;
            }
        }

        private void UpdateCoinUI()
        {
            coinAmountText.text = GlobalValue.Coin.ToString();
        }

        private void OnDestroy()
        {
            StopCoroutineIfRunning();
            OnCoinAmountChange = null;
        }
    }

    public enum CurrencyType
    {
        Coin,
        Gem
    }
}
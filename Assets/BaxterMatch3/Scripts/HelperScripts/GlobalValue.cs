

using System;
using Internal.Scripts;
using UnityEngine;
using Internal.Scripts.GUI.Boost;

namespace HelperScripts
{
    public static partial class GlobalValue
    {
        public static bool IsSoundOn
        {
            get => PlayerPrefs.GetInt(SoundKey, 1) == 1;
            set
            {
                PlayerPrefs.SetInt(SoundKey, value ? 1 : 0);
                CentralSoundManager.Instance.audioMixer.SetFloat("SoundVolume", value ? 1 : 0);
            }
        }

        public static bool IsMusicOn
        {
            get => PlayerPrefs.GetInt(MusicKey, 1) == 1;
            set
            {
                PlayerPrefs.SetInt(MusicKey, value ? 1 : 0);
                CentralMusicManager.Instance.audioMixer.SetFloat("MusicVolume", value ? 1 : 0);
            }
        }

        public static bool IsVibrationOn
        {
            get => PlayerPrefs.GetInt(VibrationKey, 1) == 1;
            set => PlayerPrefs.SetInt(VibrationKey, value ? 1 : 0);
        }

        public static int CurrentLevel
        {
            get => PlayerPrefs.GetInt(CurrentLevelKey, 1);
            set => PlayerPrefs.SetInt(CurrentLevelKey, value);
        }


        public static int GetItem<T>(T itemType) where T : Enum
        {
            return PlayerPrefs.GetInt($"Item_{itemType.GetType()}_{itemType}",
                itemType.Equals(CurrencyType.Coin) ? 5000 : 0);
        }

        public static void AddItem<T>(T itemType, int count) where T : Enum
        {
            if (count > 0)
            {
                var itemCount = GetItem(itemType);
                PlayerPrefs.SetInt($"Item_{itemType.GetType()}_{itemType}", itemCount + count);
                PlayerPrefs.Save();
            }
            else
            {
                if (itemType is BoostType boostType && boostType == BoostType.Shuffle)
                {
                    if (PlayerPrefs.GetInt($"Item_{itemType.GetType()}_{itemType}", 0) <= 0)
                    {
                        Debug.Log("Not enough shuffle boost");
                        return;
                    }
                    else
                    {
                        var itemCount = GetItem(itemType);
                        PlayerPrefs.SetInt($"Item_{itemType.GetType()}_{itemType}", itemCount + count);
                        Debug.Log($"Shuffle boost added {count} items + {itemCount + count}");
                        PlayerPrefs.Save();
                    }
                }
            }
        }

        public static void SetItem<T>(T itemType, int count) where T : Enum
        {
            //if (count < 0) return;   // Should be this one
            if (count > 0)
                PlayerPrefs.SetInt($"Item_{itemType.GetType()}_{itemType}", count);
        }

        public static bool SpendItem<T>(T itemType, int spendCount) where T : Enum
        {
            var itemCount = GetItem(itemType);
            if (itemCount < spendCount || spendCount < 0) return false;
            PlayerPrefs.SetInt($"Item_{itemType.GetType()}_{itemType}", itemCount - spendCount);
            return true;
        }

        public static int Coin
        {
            get => GetItem(CurrencyType.Coin);
            set
            {
                //Debug.LogError("Set Coin " + value);
                SetItem(CurrencyType.Coin, value);
                ResourceManager.OnCoinAmountChange?.Invoke();
            }
        }

        public static int Life
        {
            get => PlayerPrefs.GetInt(LifeKey, 5);
            set => PlayerPrefs.SetInt(LifeKey, value <= 0 ? 0 : value);
            //ResourceManager.LifeAmount = value;
        }

        public static int WinCounter
        {
            get => PlayerPrefs.GetInt(WinCounterKey, 0);
            set
            {
                // should be 32
                if (CurrentLevel < 32) return;
                PlayerPrefs.SetInt(WinCounterKey, value);
            }
        }

        public static string LifeLastAddedTime
        {
            get => PlayerPrefs.GetString(LifeLastAddedTimeKey);
            set => PlayerPrefs.SetString(LifeLastAddedTimeKey, value);
        }

        public static bool UserHasRated
        {
            get => PlayerPrefs.GetInt(UserHasRatedKey, 0) == 1;
            set => PlayerPrefs.SetInt(UserHasRatedKey, value ? 1 : 0);
        }

        public static bool GetPreGameBooster(BoostType type)
        {
            return PlayerPrefs.GetInt(PregamePowerUpKey + type, 0) == 1;
        }

        public static void SetPreGameBooster(BoostType type, bool active)
        {
            PlayerPrefs.SetInt(PregamePowerUpKey + type, active ? 1 : 0);
        }
    }
}
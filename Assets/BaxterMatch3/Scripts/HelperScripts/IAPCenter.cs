using System;
using Internal.Scripts.GUI.Boost;
using Internal.Scripts.System;
using UnityEngine;

namespace HelperScripts
{
    public class IAPCenter : MonoBehaviour
    {
        public const string PACKAGE_1 = "package_1";
        public const string PACKAGE_2 = "package_2";

        public const string COIN_100 = "coin_100";
        public const string COIN_500 = "coin_500";
        public const string COIN_1000 = "coin_1000";
        public const string COIN_2000 = "coin_2000";
        public const string COIN_3000 = "coin_3000";
        public const string COIN_5000 = "coin_5000";

        [SerializeField] private PurchasePopup[] purchasePopup;


        public void CheckForPurchaseRewards(string id)
        {
            Debug.LogError("CheckForPurchaseRewards " + id);

            switch (id)
            {
                case PACKAGE_1:
                    GiveReward(200, 1);
                    break;
                case PACKAGE_2:
                    GiveReward(200, 2);
                    break;

                case COIN_100:
                    GiveReward(100);
                    break;
                case COIN_500:
                    GiveReward(500);
                    break;
                case COIN_1000:
                    GiveReward(1000);
                    break;
                case COIN_2000:
                    GiveReward(2000);
                    break;
                case COIN_3000:
                    GiveReward(3000);
                    break;
                case COIN_5000:
                    GiveReward(5000);
                    break;
                default:
                    Debug.LogError("Product ID Not Found inside Iap Center  ");
                    break;
            }
        }

        private void GiveReward(int coinCount)
        {
            Debug.Log("GiveReward coinCount  " + coinCount);

            GlobalValue.Coin += coinCount;
            PlayerPrefs.Save();

            var popup = Instantiate(purchasePopup[0], ReferencerUI.Instance.transform);
            popup.SetRewardInfo(coinCount);
        }

        private void GiveReward(int coinCount, int packageIndex)
        {
            GlobalValue.Coin += coinCount;

            foreach (BoostType boostType in Enum.GetValues(typeof(BoostType)))
            {
                GlobalValue.AddItem(boostType, packageIndex);
            }

            PlayerPrefs.Save();

            var popup = Instantiate(purchasePopup[1], ReferencerUI.Instance.transform);
            popup.SetPackageRewardInfo(coinCount, packageIndex);
        }
    }
}
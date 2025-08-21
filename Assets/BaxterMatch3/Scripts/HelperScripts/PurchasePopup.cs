using Internal.Scripts;
using UnityEngine;
using TMPro;

namespace HelperScripts
{
    public class PurchasePopup : MonoBehaviour
    {
        [SerializeField] private GameObject[] rewards;
        [SerializeField] private TMP_Text[] rewardTexts;

        public void SetRewardInfo(int coinReward)
        {
            rewards[0].SetActive(true);
            rewardTexts[0].text = coinReward.ToString();
        }

        public void SetPackageRewardInfo(int coinReward, int boosterMultiplier)
        {
            for (var index = 0; index < rewards.Length; index++)
            {
                if (index == 0)
                {
                    rewards[index].SetActive(coinReward > 0);
                    rewardTexts[index].text = coinReward.ToString();
                    continue;
                }

                rewards[index].SetActive(true);
                rewardTexts[index].text = $"x{boosterMultiplier}";
            }
        }

        public void Close()
        {
            CentralSoundManager.Instance.PlayOneShot(CentralSoundManager.Instance.cash);
            Destroy(gameObject);
        }
    }
}
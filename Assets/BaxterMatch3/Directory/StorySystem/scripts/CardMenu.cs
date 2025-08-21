
using Internal.Scripts.GUI.Boost;
using Internal.Scripts.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StorySystem
{
    /// <summary>
    /// Reward icon for the Reward popup
    /// </summary>
    public class CardMenu : MonoBehaviour {
        public Sprite[] sprites;
        public Image icon;
        public Transform iconHolder;
        public TextMeshProUGUI text;
        public TextMeshProUGUI rewardName;

        private void Awake() {
            Destroy(text.GetComponent<LanguageAndText>());
            Destroy(rewardName.GetComponent<LanguageAndText>());
        }

        /// <summary>
        /// Sets Wheel reward
        /// </summary>
        /// <param name="reward">reward object</param>
        public void SetWheelReward(CardConfig reward) {
            foreach (Transform item in iconHolder)
            {
                Destroy(item.gameObject);
            }
            var g = Instantiate(reward.cardObject, Vector2.zero, Quaternion.identity, iconHolder);
            g.transform.localPosition = Vector3.zero;
            g.transform.localScale = Vector3.one * 3;
            icon = g.GetComponent<Image>();
            if (reward.type == BoostType.None)
            {
                text.text = LanguageManager.GetText(47, "You got coins");
                rewardName.text = "use this to pass level";
            }
            else
            {
                text.text = LanguageManager.GetText(85, "You got the boost");
                rewardName.text = reward.GetDescription();
            }

        }

        /// <summary>
        /// Set icon
        /// </summary>
        /// <param name="i"></param>
        public void SetIconSprite(int i) {
            icon.sprite = sprites[i];
            if (i == 0)
            {
                text.text = LanguageManager.GetText(47, "You got coins");
                rewardName.text = LanguageManager.GetText(87, "Coins");
            }
            else if (i == 1)
            {
                text.text = LanguageManager.GetText(86, "You got life");
                rewardName.text = LanguageManager.GetText(88, "Life");
            }
        }
    }
}
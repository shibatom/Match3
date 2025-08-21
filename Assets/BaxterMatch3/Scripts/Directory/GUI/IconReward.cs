


using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Internal.Scripts.Localization;

namespace Internal.Scripts.GUI
{
    /// <summary>
    /// Reward icon for the Reward popup
    /// </summary>
    public class IconReward : MonoBehaviour
    {
        public Sprite[] sprites;
        public Image icon;
        public Transform iconHolder;
        public TextMeshProUGUI text;
        public TextMeshProUGUI rewardName;

        private void Awake()
        {
            Destroy(text.GetComponent<LanguageAndText>());
            Destroy(rewardName.GetComponent<LanguageAndText>());
        }

        /// <summary>
        /// Set icon
        /// </summary>
        /// <param name="i"></param>
        public void SetIconSprite(int i)
        {
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
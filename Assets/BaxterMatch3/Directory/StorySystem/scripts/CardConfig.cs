using Internal.Scriptable.Rewards;
using Internal.Scripts.GUI.Boost;
using Internal.Scripts.Localization;
using UnityEngine;

namespace StorySystem
{
    /// <summary>
    /// Reward on the wheel
    /// </summary>

    [CreateAssetMenu(fileName = "New Card", menuName = "CardConfig")]
    public class CardConfig : ScriptableObject {

        public GameObject cardObject;
        public RewardScriptable reward;
        public BoostType type;
        public int count;
        public string description;
        public int descriptionLocalizationRefrence;
        public string GetDescription() {
            return LanguageManager.GetText(descriptionLocalizationRefrence, description);
        }

    }
}
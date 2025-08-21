

using Internal.Scripts;
using Internal.Scripts.Localization;
using TMPro;
using UnityEngine;

namespace Internal.Scripts.MapScripts
{
    public class StaticMapPlay : MonoBehaviour
    {
        public TextMeshProUGUI text;
        private int level;

        private void OnEnable()
        {
            level = LevelCampaign.GetLastReachedLevel();
            text.text = LanguageManager.GetText(89, "Level") + " " + level;
        }

        public void PressPlay()
        {
            Initiations.OpenMenuPlay(level);
        }
    }
}
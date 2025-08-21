

using TMPro;
using UnityEngine;
using Internal.Scripts.Localization;

namespace Internal.Scripts.GUI
{
    /// <summary>
    /// Time message in the Lifeshop
    /// </summary>
    public class LifeTimerHelper : MonoBehaviour
    {
        public TextMeshProUGUI textSource;
        public TextMeshProUGUI textDest;

        private void Update()
        {
            textDest.text = "+1" + LanguageManager.GetText(0, "life after") + textSource.text;
        }
    }
}
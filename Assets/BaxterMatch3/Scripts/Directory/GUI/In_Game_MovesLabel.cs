

using TMPro;
using UnityEngine;
using Internal.Scripts.Localization;

namespace Internal.Scripts.GUI
{
    /// <summary>
    /// Moves / Time label in the game
    /// </summary>
    public class In_Game_MovesLabel : MonoBehaviour
    {
        private void OnEnable()
        {
            if (MainManager.Instance?.levelData == null || !MainManager.Instance.levelLoaded)
                MainManager.OnLevelLoaded += Reset;
            else
                Reset();
        }

        private void OnDisable()
        {
            MainManager.OnLevelLoaded -= Reset;
        }


        private void Reset()
        {
            if (MainManager.Instance != null && MainManager.Instance.levelLoaded)
            {
                if (MainManager.Instance.levelData.limitType == LIMIT.MOVES)
                    GetComponent<TextMeshProUGUI>().text = LanguageManager.GetText(41, GetComponent<TextMeshProUGUI>().text);
                else
                    GetComponent<TextMeshProUGUI>().text = LanguageManager.GetText(77, GetComponent<TextMeshProUGUI>().text);
            }
        }
    }
}
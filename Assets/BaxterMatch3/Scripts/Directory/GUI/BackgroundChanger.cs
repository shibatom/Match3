

using UnityEngine;
using UnityEngine.UI;

namespace Internal.Scripts.GUI
{
    /// <summary>
    /// Background selector. Select different level background for every 20 levels
    /// </summary>
    public class BackgroundChanger : MonoBehaviour
    {
        public Sprite[] pictures;

        private void OnEnable()
        {
            if (MainManager.Instance != null)
            {
                var backgroundSpriteNum = (int)(HelperScripts.GlobalValue.CurrentLevel / 20f - 0.01f);
                if (pictures.Length > backgroundSpriteNum)
                    GetComponent<Image>().sprite = pictures[backgroundSpriteNum];
            }
        }
    }
}
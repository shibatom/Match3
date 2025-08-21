using UnityEngine;
using UnityEngine.UI;

namespace HelperScripts
{
    public class MainBackgroundManager : MonoBehaviour
    {
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Sprite[] backgroundSprites;

        private void Start()
        {
            SetBackground();
        }

        private void SetBackground()
        {
            int lastUnlockedArea = (GlobalValue.CurrentLevel - 1) % backgroundSprites.Length;
            Debug.LogError("CurrentLevel  "+(GlobalValue.CurrentLevel - 1));
            /*int lastUnlockedArea = GlobalValue.CurrentLevel switch
            {
                >= 200 => 10,
                >= 180 => 9,
                >= 160 => 8,
                >= 140 => 7,
                >= 120 => 6,
                >= 100 => 5,
                >= 80 => 4,
                >= 60 => 3,
                >= 40 => 2,
                >= 20 => 1,
                _ => 0
            };*/

            backgroundImage.sprite = backgroundSprites[lastUnlockedArea];
        }
    }
}
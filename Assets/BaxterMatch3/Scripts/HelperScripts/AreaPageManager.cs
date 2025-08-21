

using Internal.Scripts.MapScripts;
using UnityEngine;
using UnityEngine.UI;

namespace HelperScripts
{
    public class AreaPageManager : MonoBehaviour
    {
        [SerializeField] private Image[] areaImages;
        [SerializeField] private GameObject[] locks;
        [SerializeField] private Sprite[] areaUnlockedSprites;

        private void OnEnable()
        {
            MapCamera.IsPopupOpen = true;
            SetAreasStatus();
        }

        private void SetAreasStatus()
        {
            // For Test
            //GlobalValue.CurrentLevel = 42;
            int lastUnlockedArea = GlobalValue.CurrentLevel switch
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
            };

            for (int i = 0; i <= lastUnlockedArea; i++)
            {
                areaImages[i].sprite = areaUnlockedSprites[i];
                areaImages[i].material = null;
                locks[i].SetActive(false);
            }
        }

        private void OnDisable()
        {
            MapCamera.IsPopupOpen = false;
        }
    }
}
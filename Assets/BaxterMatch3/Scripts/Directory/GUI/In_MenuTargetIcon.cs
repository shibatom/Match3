

using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Internal.Scripts.Level;
using Internal.Scripts.System;

namespace Internal.Scripts.GUI
{
    /// <summary>
    /// Target icon in Menu
    /// </summary>
    public class In_MenuTargetIcon : MonoBehaviour
    {
        public Image image;
        public TextMeshProUGUI description;
        private Image[] images;

        private void Awake()
        {
            images = transform.GetChildren().Select(i => i.GetComponent<Image>()).ToArray();
        }

        private void OnEnable()
        {
            DisableImages();
            var levelData = new LevelData(Application.isPlaying, MainManager.Instance.currentLevel);
            levelData = LoadingController.LoadForPlay(HelperScripts.GlobalValue.CurrentLevel, levelData);
            var list = levelData.GetTargetSprites();
            description.text = levelData.GetTargetContainersForUI().First().targetLevel.GetDescription();
            for (int i = 0; i < list.Length; i++)
            {
                images[i].sprite = list[i];
                images[i].gameObject.SetActive(true);
            }
        }

        private void DisableImages()
        {
            foreach (var item in images)
            {
                item.gameObject.SetActive(false);
            }
        }
    }
}
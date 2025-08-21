

using BaxterMatch3.Animation.Directory.AnimationUI.Demo;
using Internal.Scripts.GUI;
using Internal.Scripts.MapScripts;
using Internal.Scripts.System;
using UnityEngine;

namespace HelperScripts
{
    public class MenuPlayController : MonoBehaviour
    {
        [SerializeField] private GameObject lockedBoosters;
        [SerializeField] private GameObject unlockedBoosters;

        private void OnEnable()
        {
            MapCamera.IsPopupOpen = true;
            ReferencerUI.Instance.PlayButton.SetActive(false);
        }

        private void Start()
        {
            if (GameManager.CurrentLevel >= 3)
            {
                lockedBoosters.SetActive(false);
                unlockedBoosters.SetActive(true);
            }
            else
            {
                lockedBoosters.SetActive(true);
                unlockedBoosters.SetActive(false);
            }
        }

        public void PlayButton()
        {
            if (BoosterButtonManager.OnPlayAction != null)
                BoosterButtonManager.OnPlayAction();
            GUIUtilities.Instance.StartGame();
            Close();
        }

        public void Close()
        {
            gameObject.SetActive(false);
            MapCamera.IsPopupOpen = false;
            if (ReferencerUI.Instance.LiveShop.gameObject.activeSelf == false)
                ReferencerUI.Instance.PlayButton.SetActive(true);
            else
                ReferencerUI.Instance.PlayButton.SetActive(false);
        }
    }
}
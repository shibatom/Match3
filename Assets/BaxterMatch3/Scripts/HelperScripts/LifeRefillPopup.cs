using Internal.Scripts.MapScripts;
using Internal.Scripts.System;
using UnityEngine;
using TMPro;

namespace HelperScripts
{
    public class LifeRefillPopup : MonoBehaviour
    {
        [SerializeField] private int refillPrice;
        [SerializeField] private TMP_Text heartCount;
        public int CostIfRefill;

        private void OnEnable()
        {
            MapCamera.IsPopupOpen = true;
            ReferencerUI.Instance.PlayButton.SetActive(false);
            heartCount.text = ResourceManager.LifeAmount.ToString();
        }

        public void RefillLife()
        {
            if (GlobalValue.Coin >= refillPrice)
            {
                ResourceManager.LifeAmount += 1;
                GlobalValue.Coin -= refillPrice;
                Close();
            }
            else
            {
                FindFirstObjectByType<BottomPanelController>().OnMenuButtonClick(4);
            }
        }

        public void Close()
        {
            ReferencerUI.Instance.PlayButton.SetActive(true);

            gameObject.SetActive(false);
            FindFirstObjectByType<BottomPanelController>().OnMenuButtonClick(2);
        }
    }
}
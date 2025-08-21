using Internal.Scripts;
using Internal.Scripts.MapScripts;
using Internal.Scripts.System;
using UnityEngine;

namespace HelperScripts
{
    public class ShopManager : MonoBehaviour
    {
        [SerializeField] private GameObject returnButton;

        private void OnEnable()
        {
            MapCamera.IsPopupOpen = true;
            returnButton.SetActive(MainManager.GetGameStatus() != GameState.Map);
        }

        public void BuyShopItem(string productId)
        {
            UnityIAPClass.Instance.BuyProductID(productId);
        }

        private void OnDisable()
        {
            MapCamera.IsPopupOpen = false;
        }

        public void Close()
        {
            ReferencerUI.Instance.OnToggleShop?.Invoke(true);
            var boostIcons = FindObjectsByType<Internal.Scripts.GUI.Boost.BoostInventory>(FindObjectsSortMode.None);
            foreach (var boostIcon in boostIcons)
            {
                boostIcon.UpdateBoosterText();
            }

            gameObject.SetActive(false);
        }
    }
}
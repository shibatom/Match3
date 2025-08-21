using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Internal.Scripts.GUI.Boost;
using Internal.Scripts.System;
using Internal.Scripts;

namespace HelperScripts
{
    public class BoosterOffer : MonoBehaviour
    {
        [SerializeField] private Image boosterImage;
        [SerializeField] private Sprite[] boosterSprites;

        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text description;
        [SerializeField] private TMP_Text priceText;

        private static BoostType _boosterType;

        private int _price;

        public void SetBoosterOffer(BoostType boosterType)
        {
            MainManager.Instance.DragBlocked = true;
            MainManager.Instance._clickBoosterShop = true;

            _boosterType = boosterType;
            boosterImage.sprite = (boosterType) switch
            {
                BoostType.Bomb => boosterSprites[0],
                BoostType.Arrow => boosterSprites[1],
                BoostType.Canon => boosterSprites[2],
                BoostType.Shuffle => boosterSprites[3],
                BoostType.Rocket => boosterSprites[4],
                BoostType.BombBoostType => boosterSprites[5],
                BoostType.DiscoBall => boosterSprites[6],
                _ => boosterSprites[0]
            };
            _price = boosterType switch
            {
                BoostType.Bomb => 1700,
                BoostType.Arrow => 2000,
                BoostType.Canon => 3000,
                BoostType.Shuffle => 4000,
                BoostType.Rocket => 1700,
                BoostType.BombBoostType => 2000,
                BoostType.DiscoBall => 3000,
            };

            string boosterName = boosterType.ToString();
            if (boosterType == BoostType.BombBoostType)
                boosterName = "Bomb";
            title.text = boosterName;
            description.text = $"Get extra {boosterName} booster!";
            priceText.text = _price.ToString();
            gameObject.SetActive(true);
        }

        public void BuyBooster()
        {
            if (GlobalValue.Coin >= _price)
            {
                GlobalValue.AddItem(_boosterType, 1);
                GlobalValue.Coin -= _price;

                if (_boosterType is BoostType.BombBoostType or BoostType.Rocket or BoostType.DiscoBall)
                {
                    BoosterButtonManager.OnPowerUpPurchase();
                }

                MainManager.Instance._clickBoosterShop = false;
                Debug.Log("update text " + _boosterType + GlobalValue.GetItem(_boosterType));
                BoostInventory[] boostIcons = FindObjectsByType<BoostInventory>(FindObjectsSortMode.None);
                foreach (var boostIcon in boostIcons)
                {
                    if (boostIcon.type == _boosterType)
                    {
                        boostIcon.UpdateBoosterText();
                        boostIcon.ActivateBoost();
                    }
                }

                Debug.Log("update text " + _boosterType + GlobalValue.GetItem(_boosterType));
                // BoosterButtonManager.OnPowerUpPurchase?.Invoke();
                // PlayerPrefs.SetInt("" + _boosterType, PlayerPrefs.GetInt("" + _boosterType, 0) + 1);
                ReferencerUI.Instance.OnToggleShop?.Invoke(true);
                gameObject.SetActive(false);
            }
            else
            {
                Close();
                OpenShop();
            }
        }

        public void OpenShop()
        {
            Close();
            ReferencerUI.Instance.OnToggleShop?.Invoke(false);
            ReferencerUI.Instance.inGameCoinShop.SetActive(true);
        }

        public void Close()
        {
            Debug.LogError("closeShop");
            MainManager.Instance.DragBlocked = false;
            MainManager.Instance._clickBoosterShop = false;

            gameObject.SetActive(false);
            ReferencerUI.Instance.OnToggleShop?.Invoke(true);
        }
    }
}
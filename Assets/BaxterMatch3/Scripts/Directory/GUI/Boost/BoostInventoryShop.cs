

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Internal.Scripts.Localization;

namespace Internal.Scripts.GUI.Boost
{
    public enum BoostType
    {
        ExtraMoves = 0,
        Bombs = 1,
        Rockets = 2,
        ExtraTime = 3,
        Bomb = 4,
        DiscoBalls = 5,
        FreeMove = 6,
        ExplodeArea = 7,
        Chopper = 8,
        None = 9,
        Arrow = 10,
        Canon = 11,
        Shuffle = 12,
        Empty = 13,
        Rocket = 14,
        BombBoostType = 15,
        DiscoBall = 16
    }

    /// <summary>
    /// Boost shop popup
    /// </summary>
    public class BoostInventoryShop : MonoBehaviour
    {
        public int[] prices;
        public Image icon;
        public TextMeshProUGUI description;
        public TextMeshProUGUI boostName;
        private Action _callback;

        private BoostType _boostType;

        public List<BoostInventoryShopProduct> boostProducts = new List<BoostInventoryShopProduct>();


        private void OnEnable()
        {
            foreach (var item in boostProducts.Select(i => i.boostIconObject))
            {
                item.SetActive(false);
            }
        }

        public void SetBoost(BoostInventoryShopProduct boostProduct, Action callbackL)
        {
            _boostType = boostProduct.boostType;
            gameObject.SetActive(true);
            // icon.sprite = boost.icon;
            boostProduct.boostIconObject.SetActive(true);
            description.text = boostProduct.GetDescription();
            transform.Find("Image/BuyBoost/Count").GetComponent<TextMeshProUGUI>().text = "x" + boostProduct.count;
            transform.Find("Image/BuyBoost/Price").GetComponent<TextMeshProUGUI>().text = "" + boostProduct.GemPrices;
            boostName.text = boostProduct.GetName();
            _callback = callbackL;
        }

        /// <summary>
        /// Purchase boost button function
        /// </summary>
        [UsedImplicitly]
        public void BuyBoost(GameObject button)
        {
            var count = int.Parse(button.transform.Find("Count").GetComponent<TextMeshProUGUI>().text.Replace("x", ""));
            var price = int.Parse(button.transform.Find("Price").GetComponent<TextMeshProUGUI>().text);
            GetComponent<CentralEventManager>().BuyBoost(_boostType, price, count, _callback);
        }
    }

    [Serializable]
    public class BoostInventoryShopProduct
    {
        public BoostType boostType;
        public Sprite icon;
        public string description;
        public int descriptionLocalizationRefrence;
        public string name;
        public int nameLocalizationReference;
        public int count;
        public int GemPrices;
        public GameObject boostIconObject;

        public string GetDescription()
        {
            return LanguageManager.GetText(descriptionLocalizationRefrence, description);
        }

        public string GetName()
        {
            return LanguageManager.GetText(nameLocalizationReference, name);
        }
    }
}


using TMPro;
using UnityEngine;

namespace Internal.Scripts.GUI
{
    /// <summary>
    /// Life Shop Popup
    /// </summary>
    public class LifeShopSetter : MonoBehaviour
    {
        public int CostIfRefill = 12;

        private void OnEnable()
        {
            transform.Find("Image/Buttons/BuyLife/Price").GetComponent<TextMeshProUGUI>().text = "" + CostIfRefill;
            if (!MainManager.Instance.enableInApps)
                transform.Find("Image/Buttons/BuyLife").gameObject.SetActive(false);
        }
    }
}
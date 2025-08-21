using System;
using Internal.Scripts.GUI.Boost;
using Internal.Scripts.System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HelperScripts
{
    public class BoosterButtonManager : MonoBehaviour
    {
        public static Action OnPowerUpPurchase;
        public static Action OnPlayAction;

        [SerializeField] private BoostType boosterType;
        [SerializeField] private Image buttonImage;
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text countText;

        [SerializeField] private GameObject[] tagStates;
        [SerializeField] private Sprite[] buttonStatesSprites;

        private BoosterButtonState _buttonState = BoosterButtonState.NotOwned;


        private void OnEnable()
        {
            InitBoosterButton();
        }

        private void Start()
        {
            OnPowerUpPurchase += InitBoosterButton;
            OnPlayAction += InitGameBoosters;
        }

        private void OnDestroy()
        {
            OnPowerUpPurchase = null;
            OnPlayAction = null;
        }

        public void InitBoosterButton()
        {
            // Test
            //GlobalValue.SetItem(BoosterType.Rocket, 1);
            //GlobalValue.SetItem(BoosterType.Bomb, 2);
            //GlobalValue.SetItem(BoosterType.Multicolour, 3);

            int amount = GlobalValue.GetItem(boosterType);
            ChangeButtonState(amount > 0 ? BoosterButtonState.Owned : BoosterButtonState.NotOwned);
            countText.text = amount.ToString();
        }

        private void InitGameBoosters()
        {
            GlobalValue.SetPreGameBooster(boosterType, (_buttonState == BoosterButtonState.Selected));
        }

        private void ChangeButtonState(BoosterButtonState newState, bool unSelected = false)
        {
            if (_buttonState == BoosterButtonState.Selected && !unSelected)
                return;

            _buttonState = newState;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(_buttonState switch
            {
                BoosterButtonState.Selected => ButtonUnSelectFunction,
                BoosterButtonState.Owned => ButtonSelectFunction,
                BoosterButtonState.NotOwned => DirectToBoosterPanel,
                _ => DirectToBoosterPanel
            });
            switch (_buttonState)
            {
                case BoosterButtonState.Selected:
                    buttonImage.sprite = buttonStatesSprites[1];
                    tagStates[0].SetActive(false);
                    tagStates[1].SetActive(false);
                    tagStates[2].SetActive(true);
                    break;
                case BoosterButtonState.Owned:
                    buttonImage.sprite = buttonStatesSprites[0];
                    tagStates[0].SetActive(false);
                    tagStates[1].SetActive(true);
                    tagStates[2].SetActive(false);
                    break;
                case BoosterButtonState.NotOwned:
                default:
                    buttonImage.sprite = buttonStatesSprites[0];
                    tagStates[0].SetActive(true);
                    tagStates[1].SetActive(false);
                    tagStates[2].SetActive(false);
                    break;
            }
        }

        private void ButtonUnSelectFunction()
        {
            ChangeButtonState(BoosterButtonState.Owned, true);
        }

        private void ButtonSelectFunction()
        {
            ChangeButtonState(BoosterButtonState.Selected);
        }

        private void DirectToBoosterPanel()
        {
            ReferencerUI.Instance.BoostShop.SetBoosterOffer(boosterType);
        }

        private enum BoosterButtonState
        {
            Selected,
            Owned,
            NotOwned
        }
    }
}
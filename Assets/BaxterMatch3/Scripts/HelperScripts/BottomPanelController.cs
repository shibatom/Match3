using System.Collections;
using DG.Tweening;
using Internal.Scripts.System;
using UnityEngine;

namespace HelperScripts
{
    public class BottomPanelController : MonoBehaviour
    {
        [SerializeField] private Transform[] buttonsTransforms;
        [SerializeField] private MenuIconController[] iconsControllers;
        [SerializeField] private Transform highlightTransform;

        public static Transform StaticHighlightTransform;
        private static int _selectedButtonIndex = 2;

        private IEnumerator Start()
        {
            StaticHighlightTransform = highlightTransform;
            yield return null;
            OnMenuButtonClick(2);
        }


        public void OnMenuButtonClick(int index)
        {
            //if(index==0)return;         // Disable Ranking
            _selectedButtonIndex = index;
            ReferencerUI.Instance.MenuPlay.SetActive(false);
            DoButtonFunction(index);
            for (var i = 0; i < buttonsTransforms.Length; i++)
            {
                if (index != i)
                {
                    SetButtonToNormal(i);
                    SetIconNormal(i);
                }
                else
                {
                    SetButtonHighlighted(i);
                    SetIconHighlighted(i);
                }
            }

            ReferencerUI.Instance.PlayButton.SetActive(index == 2);
        }

        private static void DoButtonFunction(int index)
        {
            ReferencerUI.Instance.BoostShop.gameObject.SetActive(false);
            ReferencerUI.Instance.SettingsMenu.SetActive(false);
            ReferencerUI.Instance.LiveShop.SetActive(false);

            switch (index)
            {
                case 0:
                    ReferencerUI.Instance.GemsShop.SetActive(false);
                    ReferencerUI.Instance.Areas_Page.SetActive(false);
                    ReferencerUI.Instance.Collections_Page.SetActive(false);
                    ReferencerUI.Instance.leaderbaord.SetActive(true);
                    break;
                case 1:
                    ReferencerUI.Instance.leaderbaord.SetActive(false);
                    ReferencerUI.Instance.GemsShop.SetActive(false);
                    ReferencerUI.Instance.Areas_Page.SetActive(false);
                    ReferencerUI.Instance.Collections_Page.SetActive(true);
                    break;
                case 2:
                default:
                    ReferencerUI.Instance.leaderbaord.SetActive(false);
                    ReferencerUI.Instance.GemsShop.SetActive(false);
                    ReferencerUI.Instance.Areas_Page.SetActive(false);
                    ReferencerUI.Instance.Collections_Page.SetActive(false);
                    break;
                case 3:
                    ReferencerUI.Instance.leaderbaord.SetActive(false);
                    ReferencerUI.Instance.GemsShop.SetActive(false);
                    ReferencerUI.Instance.Collections_Page.SetActive(false);
                    ReferencerUI.Instance.Areas_Page.SetActive(true);
                    break;
                case 4:
                    ReferencerUI.Instance.leaderbaord.SetActive(false);
                    ReferencerUI.Instance.Areas_Page.SetActive(false);
                    ReferencerUI.Instance.Collections_Page.SetActive(false);
                    ReferencerUI.Instance.GemsShop.SetActive(true);
                    break;
            }
        }

        private void SetIconHighlighted(int index)
        {
            iconsControllers[index].SetIconStatus(IconStatus.Highlighted);
            //buttonsRects[index].sizeDelta = _highlightedButtonSize;
        }

        private void SetIconNormal(int index)
        {
            iconsControllers[index].SetIconStatus(_selectedButtonIndex > index ? IconStatus.MovedAsideLeft : IconStatus.MovedAsideRight);
            //buttonsRects[index].sizeDelta = _highlightedButtonSize;
        }


        private void SetButtonHighlighted(int index)
        {
            buttonsTransforms[index].DOScaleX(1.5f, .2f);
            //buttonsRects[index].sizeDelta = _highlightedButtonSize;
        }

        private void SetButtonToNormal(int index)
        {
            buttonsTransforms[index].DOScaleX(1f, .2f);
            //buttonsRects[index].sizeDelta = _normalButtonSize;
        }
    }
}
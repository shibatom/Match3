using System.Collections;
using System.Collections.Generic;
using Internal.Scripts.Items;
using UnityEngine;

namespace HelperScripts
{
    public class ButlersGiftsController : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private ButlersGift[] powerUps;
        [SerializeField] private Transform[] createPoses;


        private static List<Item> itemsToBeReplaced;
        private static int _giftLevel = 0;

        public bool CheckAndShowGifts(int winStreak)
        {
            switch (winStreak)
            {
                case < 0:
                default:
                    return false;
                    break;
                case 1:
                    _giftLevel = 1;
                    break;
                case 2:
                    _giftLevel = 2;
                    break;
                case > 2:
                    _giftLevel = 3;
                    break;
            }

            var tempItemsToBeReplaced = Internal.Scripts.MainManager.Instance.field.GetRandomItems(_giftLevel);
            Debug.LogError("tempItemsToBeReplaced.Count  " + tempItemsToBeReplaced.Count + " _giftLevel  " + _giftLevel);
            if (tempItemsToBeReplaced.Count < _giftLevel)
                return false;
            else
                itemsToBeReplaced = tempItemsToBeReplaced;

            Instantiate(gameObject);

            return true;
        }

        private void Awake()
        {
            switch (_giftLevel)
            {
                case 1:
                    animator.Play("NoghreReward");
                    break;
                case 2:
                    animator.Play("TalaReward");
                    break;
                case 3:
                    animator.Play("EpicReward");
                    break;
            }
        }

        public void CreatePowerUps()
        {
            for (int i = 0; i < _giftLevel; i++)
            {
                var gift = Instantiate(powerUps[i], createPoses[i].position, Quaternion.identity);
                gift.itemToBeReplaced = itemsToBeReplaced[i];
            }
        }

        public void PauseTheButlersPlate()
        {
            StartCoroutine(PauseTheButlersPlateCo(_giftLevel / 6));
        }

        private IEnumerator PauseTheButlersPlateCo(int pauseAmount)
        {
            animator.enabled = false;
            yield return new WaitForSeconds(pauseAmount);
            animator.enabled = true;
        }

        public void DestroyButler()
        {
            Destroy(gameObject);
        }
    }
}
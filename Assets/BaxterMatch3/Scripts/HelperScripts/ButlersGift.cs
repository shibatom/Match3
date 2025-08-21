using System.Collections;
using DG.Tweening;
using Internal.Scripts.Items;
using UnityEngine;

namespace HelperScripts
{
    public class ButlersGift : MonoBehaviour
    {
        /*[HideInInspector]*/
        public Item itemToBeReplaced;

        private static float _powerUpDelay = 0;

        [SerializeField] private ItemsTypes powerUpType;
        [SerializeField] private GameObject particle;

        private IEnumerator Start()
        {
            _powerUpDelay += 0.25f;
            yield return new WaitForSeconds(_powerUpDelay);
            StartMoving();
        }

        private void StartMoving()
        {
            transform.DOJump(itemToBeReplaced.transform.position, 3, 1, .8f).OnComplete(ReplaceItem);
        }

        private void ReplaceItem()
        {
            Instantiate(particle, transform.position, Quaternion.identity);
            itemToBeReplaced.SetType(powerUpType, null);
            DestroyGift();
        }

        private void DestroyGift()
        {
            _powerUpDelay = 0;
            Destroy(gameObject);
        }
    }
}
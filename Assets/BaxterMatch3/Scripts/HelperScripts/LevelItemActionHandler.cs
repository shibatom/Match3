using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace HelperScripts
{
    public class LevelItemActionHandler : MonoBehaviour
    {
        private static Vector3 newScale = new(.2f, .2f, .2f);
        [SerializeField] private Sprite[] itemImageSprites;
        [SerializeField] private Image itemImage;

        private Vector3 _targetLocation;
        private Transform _itemTransform;


        public void SetItemInfo(int itemNumber, Vector3 targetLocation)
        {
            _targetLocation = targetLocation;
            _itemTransform = itemImage.transform;
            itemImage.sprite = itemImageSprites[itemNumber];
        }


        public void ButtonAction()
        {
            transform.GetChild(0).gameObject.SetActive(false);
            _itemTransform.DOScale(newScale, 1);
            _itemTransform.DOMove(_targetLocation, 1.5f).OnComplete(delegate
            {
                //LevelManager.THIS.gameStatus = GameState.Map;
                RoadmapItemsManager.IsDoneShowingItem = true;
                Destroy(gameObject);
            });
        }
    }
}
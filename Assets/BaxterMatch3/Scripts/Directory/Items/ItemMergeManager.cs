using UnityEngine;

namespace Internal.Scripts.Items
{
    public class ItemMergeManager : MonoBehaviour
    {
        private Item _itemToBeMergeWhitin;

        public Item ItemToBeMergeWhitin
        {
            get { return _itemToBeMergeWhitin; }
            set
            {
                _itemToBeMergeWhitin = value;
                Debug.Log($"ItemToBeMergeWhitin set to: {value}");
            }
        }
    }
}
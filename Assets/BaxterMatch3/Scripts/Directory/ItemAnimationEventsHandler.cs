

using Internal.Scripts.Items;
using UnityEngine;

namespace Internal.Scripts
{
    public class ItemAnimationEventsHandler : MonoBehaviour
    {
        public Item item;

        public void SetAnimationDestroyingFinished()
        {
            Debug.Log("sallog SetAnimationDestroyingFinished");
            item.SetAnimationDestroyingFinished();
        }
    }
}
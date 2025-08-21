

using UnityEngine;

namespace Internal.Scripts.Items
{
    /// <summary>
    /// Common item methods
    /// </summary>
    public interface IItemInterface
    {
        void Destroy(Item item1, Item item2 , bool isPackageCombined = false , bool destroyNeighboours = true , int color = 0 ,bool isCombinedwithMulti = false);
        // void Check(Item item1, Item item2);
        GameObject GetGameobject();
        bool IsCombinable();
        bool IsExplodable();
        void SetExplodable(bool setExplodable);
        bool IsStaticOnStart();
        void SetOrder(int i);
        Item GetParentItem();

    }
}
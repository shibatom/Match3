

using System;
using System.Linq;
using Spine.Unity;
using UnityEngine;
using Internal.Scripts.Blocks;

namespace Internal.Scripts.Items
{
    /// <summary>
    /// Simple item
    /// </summary>
    public class SimpleItem : Item, IItemInterface
    {
        public bool ActivateByExplosion;
        public bool StaticOnStart;

        public SkeletonAnimation skeletonAnimation;
        public SkeletonAnimation skeletonAnimationSecond;

        public SpriteRenderer WhiteShadow;

        public void Destroy(Item item1, Item item2, bool isPackageCombined = false, bool destroyNeighboours = true, int color = 0, bool isCombinedwithMulti = false)
        {
            Item parentItem = GetParentItem();
            Rectangle parentRectangle = parentItem.square;
            int updatedColor = color + 1;
            item1.DestroyBehaviour();
            parentRectangle.DestroyBlock(destroyNeighbour: destroyNeighboours, color: updatedColor);
        }

        public override void Check(Item item1, Item item2)
        {
            if (item2.currentType != ItemsTypes.NONE)
                item2.Check(item2, item1);
        }

        public Item GetParentItem()
        {
            return transform.GetComponentInParent<Item>();
        }

        public GameObject GetGameobject()
        {
            return gameObject;
        }

        public bool IsCombinable()
        {
            return Combinable;
        }

        public bool IsExplodable()
        {
            return ActivateByExplosion;
        }

        public void SetExplodable(bool setExplodable)
        {
            ActivateByExplosion = setExplodable;
        }

        public bool IsStaticOnStart()
        {
            if (MainManager.Instance.gameStatus != GameState.Playing)
                return StaticOnStart;
            return false;
        }

        public void SetOrder(int i)
        {
            var spriteRenderers = GetSpriteRenderers();
            var orderedEnumerable = spriteRenderers.OrderBy(x => x.sortingOrder).ToArray();
            for (int index = 0; index < orderedEnumerable.Length; index++)
            {
                var spr = orderedEnumerable[index];
                spr.sortingOrder = i + index;
            }
        }

        public void SecondPartDestroyAnimation(Action callback)
        {
            throw new NotImplementedException();
        }
    }
}
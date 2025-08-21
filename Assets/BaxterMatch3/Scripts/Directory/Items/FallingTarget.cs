

using System;
using System.Linq;
using Spine.Unity;
using UnityEngine;

namespace Internal.Scripts.Items
{
    /// <summary>
    /// Spiral item
    /// </summary>
    public class FallingTarget : Item, IItemInterface
    {
        public SkeletonMecanim skeletonAnimation;
        public Animator animator;
        public bool ActivateByExplosion;
        public bool StaticOnStart;

        public void Destroy(Item item1, Item item2, bool isPackageCombined = false, bool destroyNeighboours = true, int color = 0, bool isCombinedwithMulti = false)
        {
            item1.DestroyBehaviour();
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

        public Item GetParentItem()
        {
            return this;
        }

        public void SecondPartDestroyAnimation(Action callback)
        {
            throw new NotImplementedException();
        }
    }
}
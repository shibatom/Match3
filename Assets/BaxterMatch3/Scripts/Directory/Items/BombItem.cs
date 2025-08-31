using System;
using System.Collections;
using System.Linq;
using Spine.Unity;
using Internal.Scripts.System.Pool;
using UnityEngine;

namespace Internal.Scripts.Items
{
    /// <summary>
    /// Item package
    /// </summary>
    public class BombItem : Item, IItemInterface //, ILongDestroyable
    {
        public Objectshaker objectshaker;
        public GameObject explosionPrefab;
        public GameObject DoubleExplosionPrefab;

        public GameObject BoomPrefab;

        // public bool Combinable;
        public bool ActivateByExplosion;
        public bool StaticOnStart;

        public Animator PackageAnimator;

        public GameObject explosion;
        public GameObject explosion2;
        public GameObject circle;

        public SpriteRenderer spriteRenderer;
        public SpriteRenderer spriteRenderer2;

        public SkeletonAnimation skeletonAnimation;

        private In_GameBlocker _inGameBlocker;

        private Action _callbackDestroy;

        // private bool animationFinished;
        private readonly int _priority = 0;
        private bool _canBeStarted;
        private bool _isSwitchItemOverChopper = false;

        private new Item GetItem => GetComponentInParent<Item>();

        public Vector3 SwitchItemDirection { get; private set; }
        public Action _stripedDestroy;

        public delegate void ShakeAction(float duration, float strength);

        public static event ShakeAction OnShakeRequested;
        private Item savedItem1;
        private Item savedItem2;

        public ItemAnimationDestroyer itemDestroyAnimation;
        public HelperScripts.PackageDestroyAnimation packageDestroyAnimation;
        public Item PackageItem;
        public Item combItem;
        public ItemsTypes interactionType;

        // Stores the item type for deferred animation setting


        public bool isCombinedwithMulti = false;

        public void Destroy(Item item1, Item item2, bool isBombCombined = false, bool destroyNeighboours = true, int color = 0, bool isCombinedwithMulti = false)
        {
            if (PackageAnimator != null && PackageAnimator.GetCurrentAnimatorStateInfo(0).IsName("appear State"))
            {
                PackageAnimator.SetTrigger("ForceIdleState");
            }

            MainManager.Instance.StartBusyOperation();
            if (item1 != null && item2 == null)
                MainManager.Instance.StopedAI();

            // Add GameBlocker only if it doesn’t already exist
            if (_inGameBlocker == null)
            {
                _inGameBlocker = gameObject.AddComponent<In_GameBlocker>();
            }

            item1.destroying = true;


            this.isCombinedwithMulti = isCombinedwithMulti;
            if (isBombCombined)
            {
                packageDestroyAnimation.item1 = item1;
                PackageAnimator.SetTrigger("DoubleExplode");
                StartCoroutine(DoubleExplode());
            }
            else
            {
                MainManager.HapticAndShake(2);

                // Use object pooling instead of instantiation
                GameObject explosion = ObjectPoolManager.Instance.GetPooledObject("BombParticleLight");
                if (explosion != null)
                {
                    explosion.transform.position = transform.position;
                    explosion.SetActive(true);
                    explosion.GetComponent<HelperScripts.BackToPool>().StartAnimation();
                }

                StartCoroutine(DestroyHelper(item1, 0.25f));
                // PackageAnimator.SetTrigger("SimpleExplode");
            }

            // Call DestroyPackage, assuming it’s optimized separately
        }

        private IEnumerator DestroyHelper(Item item1, float seconds = 0.2f)
        {
            packageDestroyAnimation.item1 = item1;
            yield return new WaitForSeconds(seconds);
            packageDestroyAnimation.DestroyIt(0);
        }

        private IEnumerator DoubleExplode()
        {
            yield return new WaitForSeconds(1.2f);
            OnShakeRequested?.Invoke(0.2f, 0.50f); // Call the event with parameters
            MainManager.HapticAndShake(2);
            MainManager.HapticAndShake(2);
            packageDestroyAnimation.DestroyIt(1);
        }

        public Item GetParentItem()
        {
            return transform.GetComponentInParent<Item>();
        }

        public override void Check(Item item1, Item item2)
        {
            if ((item2.currentType == ItemsTypes.DiscoBall))
            {
                item2.Check(item2, item1);
            }

            if ((item2.currentType == ItemsTypes.RocketHorizontal || item2.currentType == ItemsTypes.RocketVertical))
            {
                item2.Check(item1, item2);
                // SetAnimation(switchDirection , false);
                //  PackageAnimator.SetTrigger("PackageMerge");
            }

            if (item2.currentType == ItemsTypes.Chopper)
            {
                item2.Check(item1, item2);
            }

            if (item1.currentType == ItemsTypes.Bomb && item2.currentType == ItemsTypes.Bomb)
            {
                item1.GetTopItemInterface().Destroy(item1, item2, true);
            }
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
            return StaticOnStart;
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

        public bool IsAnimationFinished()
        {
            return animationFinished;
        }

        public int GetPriority()
        {
            return _priority;
        }

        public bool CanBeStarted()
        {
            return _canBeStarted;
        }

        private void HandleItemDestruction(Item item)
        {
        }

        void CheckDirection(Item item1, Item item2)
        {
            if (!(item1.currentType == ItemsTypes.Bomb && item2.currentType == ItemsTypes.Bomb))
            {
                // Check if item2 is Chopper, and if so, switch it with item1.
                if (item2.currentType == ItemsTypes.Bomb)
                {
                    SwitchItemDirection = item1.switchDirection;
                    (item1, item2) = (item2, item1);
                    _isSwitchItemOverChopper = true;
                }
                else
                {
                    SwitchItemDirection = item1.switchDirection;
                }
            }

            // Save item1 and item2
            SaveItemsForStripe(item1, item2);
        }

        private void SaveItemsForStripe(Item item1, Item item2)
        {
            savedItem1 = item1;
            savedItem2 = item2;
        }

        private void SetAnimation(Vector3 SwitchDirection, bool isSwitchItemOverChopper)
        {
            SetAnimation(switchDirection, false);

            if (interactionType != ItemsTypes.NONE)
            {
            }

            switch (interactionType)
            {
                case ItemsTypes.RocketHorizontal:
                    break;

                case ItemsTypes.RocketVertical:
                    break;

                case ItemsTypes.Bomb:
                    break;

                case ItemsTypes.NONE:
                    break;
            }
        }
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Spine.Unity;
using UnityEngine;
using static UnityEngine.ParticleSystem;
using Internal.Scripts.Blocks;
using Internal.Scripts.Effects;
using Internal.Scripts.System.Pool;
using Random = UnityEngine.Random;

namespace Internal.Scripts.Items
{
    /// <summary>
    /// Item multicolor - special item that interacts with other item types
    /// </summary>
    public class DiscoBallItem : Item, IItemInterface
    {
        #region Fields

        public Objectshaker objectshaker;
        public bool ActivateByExplosion;

        public bool StaticOnStart;

        //public bool Combinable;
        public bool activated;
        public bool targetSpread;

        public GameObject LightningPrefab;
        public GameObject explosionPrefab;
        public SpriteRenderer spriteRenderer;
        public SpriteRenderer mask;
        public Animator MultiColorAnimator;
        public SkeletonMecanim skeletonAnimation;

        private Objectshaker objecthaker;
        private MeshRenderer skeletonRenderer;
        private bool activatedByMulti;

        // Cache for optimization
        private readonly WaitForSeconds _waitOnIteration = new WaitForSeconds(0.01f);
        private readonly WaitForSeconds _destroyBombDelay = new WaitForSeconds(2.5f);
        private Dictionary<GameObject, SimpleItem> _itemSimplePool = new Dictionary<GameObject, SimpleItem>();

        #endregion

        #region Events

        public delegate void ShakeAction(float duration, float strength);

        public static event ShakeAction OnShakeRequested;

        #endregion

        #region Initialization

        public override void InitItem()
        {
            base.InitItem();
            activated = false;
        }

        #endregion

        #region Core Logic

        public override void Check(Item item1, Item item2)
        {
            SetUpMergeEffect(101);
            //item2.HideSprites(true);
            MainManager.Instance.StartBusyOperation();
            // LevelManager.THIS.FindMatches();
            if (MainManager.GetGameStatus() != GameState.PreWinAnimations)
                gameObject.AddComponent<In_GameBlocker>();
            if (item1?.square?.type == LevelTargetTypes.ExtraTargetType2 || item2?.square?.type == LevelTargetTypes.ExtraTargetType2)
                targetSpread = true;

            GetParentItem().destroying = true;

            if (item2.currentType == ItemsTypes.NONE)
            {
                HandleNoneItem(item2);
            }
            else if (item2.currentType == ItemsTypes.RocketHorizontal || item2.currentType == ItemsTypes.RocketVertical)
            {
                HandleStripedItem(item1, item2);
            }
            else if (item2.currentType == ItemsTypes.Bomb)
            {
                HandlePackageItem(item2);
            }
            else if (item2.currentType == ItemsTypes.Chopper)
            {
                HandleChopperItem(item2);
            }
            else if (item2.currentType == ItemsTypes.DiscoBall)
            {
                HandleMulticolorItem(item1, item2);
            }
        }

        private void HandleNoneItem(Item item2)
        {
            if (MainManager.GetGameStatus() != GameState.PreWinAnimations)
                gameObject.AddComponent<In_GameBlocker>();
            StartCoroutine(DestroyColor(item2.color, item2.currentType));
            //  ResetItem(item2);
            // item2.DestroyItem();
            activated = true;
        }

        private void HandleStripedItem(Item item1, Item item2)
        {
            if (MainManager.GetGameStatus() != GameState.PreWinAnimations)
                gameObject.AddComponent<In_GameBlocker>();
            DisableStripedAnimation(item1, item2);
            MainManager.Instance.StartCoroutine(SetTypeByColor(item2));
            activated = true;
        }

        private void HandlePackageItem(Item item2)
        {
            if (MainManager.GetGameStatus() != GameState.PreWinAnimations)
                gameObject.AddComponent<In_GameBlocker>();
            StartCoroutine(SetTypeByColor(item2));
            activated = true;
        }

        private void HandleChopperItem(Item item2)
        {
            activatedByMulti = true;
            if (MainManager.GetGameStatus() != GameState.PreWinAnimations)
                gameObject.AddComponent<In_GameBlocker>();
            GetParentItem().destroying = false;
            MainManager.Instance.StartCoroutine(SetTypeByColor(item2));
            activated = true;
        }

        private void HandleMulticolorItem(Item item1, Item item2)
        {
            if (MainManager.GetGameStatus() != GameState.PreWinAnimations)
                gameObject.AddComponent<In_GameBlocker>();
            item2.GetTopItemInterface().GetGameobject().GetComponent<DiscoBallItem>().MultiColorAnimator.gameObject.SetActive(false);
            DestroyDoubleMulticolor(item1.square.col, () =>
            {
                var list = new[] { item1, item2 };
                list.First(i => i != GetParentItem()).SmoothDestroy();
                list.First(i => i == GetParentItem()).SmoothDestroy();
                mask.gameObject.SetActive(false);
            });
            activated = true;
        }

        public void Destroy(Item item1, Item item2, bool isPackageCombined = false, bool destroyNeighboours = true, int color = 0, bool isCombinedwithMulti = false)
        {
            MainManager.Instance.StartBusyOperation();

            // Log the items involved in the destroy operation
            Debug.Log(
                $"[ItemMulticolor.Destroy] item1: {(item1 != null ? item1.name : "null")}, item2: {(item2 != null ? item2.name : "null")}, isPackageCombined: {isPackageCombined}, destroyNeighboours: {destroyNeighboours}, color: {color}, isCombinedwithMulti: {isCombinedwithMulti}");

            //LevelManager.THIS.FindMatches();

            if (activated) return;

            if (item2 == null)
            {
                HandleNullItem2(item1);
                return;
            }
        }

        private void HandleNullItem2(Item item1)
        {
            MainManager.Instance.StopedAI();

            if (GetParentItem().square.type == LevelTargetTypes.ExtraTargetType1)
            {
                GetParentItem().square.DestroyBlock();
            }

            if (MainManager.GetGameStatus() == GameState.PreWinAnimations)
            {
                var newItem2 = MainManager.Instance.field.squaresArray.First(i => i.item != null && i.item.currentType == ItemsTypes.NONE).item;
                Check(item1, newItem2);
                return;
            }

            if (explodedItem)
                StartCoroutine(DestroyColor(explodedItem.color, ItemsTypes.NONE, noSwitchItem: true));
            else
                StartCoroutine(DestroyColor(Random.Range(0, MainManager.Instance.levelData.colorLimit - 1), ItemsTypes.NONE, true));
        }

        #endregion

        #region Item Processing

        /// <summary>
        /// Get filtered items grouped by color
        /// </summary>
        public List<Item> GetFilteredAndGroupedItems()
        {
            var filteredItems = MainManager.Instance.field.GetItems()
                .Where(i => !i.Equals(GetParentItem()))
                .Where(i => i.color >= 0 && i.color <= 3)
                .Where(i => i.currentType == ItemsTypes.NONE)
                .Where(i => !i.JustIntItem)
                .ToList();

            // Group items by color and find the group with most items
            var colorGroups = filteredItems
                .GroupBy(i => i.color)
                .Select(group => new { Color = group.Key, Count = group.Count(), Items = group.ToList() })
                .OrderByDescending(group => group.Count)
                .ToArray();

            // Return items from color with highest count, or empty list if none
            return colorGroups.FirstOrDefault()?.Items ?? new List<Item>();
        }

        private IEnumerator SetTypeByColor(Item item2)
        {
            // LevelManager.THIS.StartBusyOperation();

            // Set up items to destroy list
            List<Item> itemsToDestroy = new List<Item>();
            Item[] items = GetItemsToProcess(item2);

            var nextType = item2.currentType;
            bool loopFinished = false;
            GameObject itemChopperTarget = new GameObject();

            StartCoroutine(IterateItems(items,
                item => ProcessItem(item, itemChopperTarget, nextType, itemsToDestroy, item2),
                () =>
                {
                    StartCoroutine(OnIterationComplete(itemChopperTarget, item2, itemsToDestroy));
                    loopFinished = true;
                }));

            yield return new WaitUntil(() => loopFinished);

            FinalizeProcessing(item2);
            activated = true;
        }

        private Item[] GetItemsToProcess(Item item2)
        {
            if (item2.currentType == ItemsTypes.NONE)
            {
                return MainManager.Instance.field.GetItemsByColor(item2.color)
                    .Where(i => !i.Equals(GetParentItem()) && !i.JustCreatedItem)
                    .ToArray();
            }
            else
            {
                var filteredItems = MainManager.Instance.field.GetItems()
                    .Where(i => !i.Equals(GetParentItem()))
                    .Where(i => i.color >= 0 && i.color <= 3)
                    .Where(i => i.currentType == ItemsTypes.NONE)
                    .Where(i => !i.JustCreatedItem);

                var colorGroups = filteredItems
                    .GroupBy(i => i.color)
                    .Select(group => new { Color = group.Key, Count = group.Count(), Items = group.ToArray() })
                    .OrderByDescending(group => group.Count)
                    .ToArray();

                return colorGroups.FirstOrDefault()?.Items ?? new Item[0];
            }
        }

        private void ProcessItem(Item item, GameObject itemChopperTarget, ItemsTypes nextType, List<Item> itemsToDestroy, Item item2)
        {
            // Determine next type for striped items
            item.NextType = (nextType == ItemsTypes.RocketHorizontal || nextType == ItemsTypes.RocketVertical)
                ? (ItemsTypes)Random.Range(4, 6)
                : nextType;

            item.ChopperTarget = itemChopperTarget;
            item.ChangeType(item => itemsToDestroy.Add(item), false);

            // Create lightning effect
            CreateLightning(transform.position, item.transform.position, item2.color, () =>
            {
                item.anim.SetTrigger("Electro");
                item.colorableComponent.ActivateShadow(true);
            });
        }

        private IEnumerator OnIterationComplete(GameObject itemChopperTarget, Item item2, List<Item> itemsToDestroy)
        {
            MultiColorAnimator.SetBool("HoldSpin", false);
            // ObjectPooler.Instance.GetPooledObject("BombParticleLightMulticolor", this).transform.position = transform.position;
            Destroy(itemChopperTarget);
            Destroy(item2.gameObject);

            foreach (Item item in itemsToDestroy.Where(i => i != null))
            {
                item.DestroyItem(true, true, this, true, WithoutShrink: true, destroyNeighbours: false, isCombinedwithMulti: true);
                yield return new WaitForEndOfFrame();
            }

            MainManager.Instance.multicolorWorking = false;
            StartCoroutine(EndDestructionSequence(itemsToDestroy, item2.currentType));
        }

        private void FinalizeProcessing(Item item2)
        {
            StartCoroutine(DestroyColor(item2.color, item2: item2.currentType));
        }

        private IEnumerator DestroyColor(int colorId, ItemsTypes item2, bool noSwitchItem = false)
        {
            PlayColorBombExplosionSound();
            MultiColorAnimator.SetTrigger("StartSpin");
            //LevelManager.THIS.StartBusyOperation();
            MainManager.Instance.multicolorWorking = true;
            MultiColorAnimator.SetBool("HoldSpin", true);
            yield return new WaitForSeconds(0.2f);

            if (MainManager.Instance.Falling)
            {
                yield return new WaitUntil(() => !MainManager.Instance.Falling || !MainManager.Instance.IsAnyPowerUpActive());
            }


            MainManager.Instance.multicolorWorking = true;
            List<Item> itemsToDestroy = noSwitchItem ? GetFilteredAndGroupedItems() : GetItemsToDestroy(colorId);

            StartCoroutine(IterateItems(itemsToDestroy.ToArray(),
                item =>
                {
                    HandleItemDestruction(item);
                    itemsToDestroy.Add(item);
                },
                () => { StartCoroutine(EndDestructionSequence(itemsToDestroy, item2)); }));
        }

        #endregion

        #region Effects

        private void PlayColorBombExplosionSound()
        {
            CentralSoundManager.Instance.PlayOneShot(CentralSoundManager.Instance.discoBallExplodeEffect);
        }

        private List<Item> GetItemsToDestroy(int colorId)
        {
            return MainManager.Instance.field.GetItemsByColor(colorId)
                .Where(i => !i.Equals(GetParentItem()))
                .ToList();
        }

        private void HandleItemDestruction(Item item)
        {
            CreateLightningEffect(item);
        }

        private void CreateLightningEffect(Item item)
        {
            CreateLightning(transform.position, item.transform.position, item.color, () =>
            {
                item.anim.SetTrigger("Electro");
                int colorNum = item.colorableComponent.ActivateShadow(true);
                ActivateElectrifiedParticles(item, item.color);
            });
        }

        private void ActivateElectrifiedParticles(Item item, int colorID)
        {
            var particle = ObjectPoolManager.Instance.GetPooledObject("Impact01");
            MainModule mainParticle = particle.GetComponent<ParticleSystem>().main;
            Color color = GetColorById(colorID);
            mainParticle.startColor = new MinMaxGradient(color);

            if (particle != null)
            {
                particle.transform.localPosition = item.transform.position;
            }
        }

        private IEnumerator EndDestructionSequence(List<Item> itemsToDestroy, ItemsTypes item2)
        {
            MultiColorAnimator.SetBool("HoldSpin", false);
            yield return new WaitForSeconds(0.18f);
            MainManager.Instance.multicolorWorking = false;

            if (item2 == ItemsTypes.NONE)
            {
                // ObjectPooler.Instance.GetPooledObject("BombParticleLightMulticolor", this).transform.position=transform.position;
            }

            if (item2 == ItemsTypes.NONE)
            {
                foreach (var item in itemsToDestroy)
                {
                    item.DestroyItem(true, true, this, true, WithoutShrink: true, multicolorEffect: true, destroyNeighbours: true);
                }
            }

            MainManager.Instance.EndBusyOperation();

            SmoothDestroy();
        }

        private IEnumerator IterateItems(Item[] items, Action<Item> iterateItem, Action onFinished = null)
        {
            MultiColorAnimator.SetTrigger("StartSpin");
            MultiColorAnimator.SetBool("HoldSpin", true);

            MainManager.Instance.multicolorWorking = true;
            if (MainManager.Instance.Falling)
            {
                yield return new WaitUntil(() => !MainManager.Instance.Falling || !MainManager.Instance.IsAnyPowerUpActive());
            }

            // Process in batches to maintain performance
            int iterationCount = 0;
            int yieldFrequency = Mathf.Clamp(items.Length, items.Length, 1);

            foreach (var item in items)
            {
                if (item == null || !item.gameObject.activeSelf) continue;

                if (targetSpread)
                    item.square?.SetType(LevelTargetTypes.ExtraTargetType2, 1, LevelTargetTypes.NONE, 1);

                iterateItem(item);
                iterationCount++;

                if (iterationCount % yieldFrequency == 0)
                    yield return new WaitForSeconds(0.1f);
            }

            yield return new WaitForSeconds(0.9f);
            onFinished?.Invoke();
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Disables the animation for striped items.
        /// </summary>
        private void DisableStripedAnimation(Item item1, Item item2)
        {
            var stripedAnim = (item1.currentType != ItemsTypes.DiscoBall)
                ? item1.GetComponent<Item>().GetTopItemInterface().GetGameobject().GetComponent<RocketItem>()
                : item2.GetComponent<Item>().GetTopItemInterface().GetGameobject().GetComponent<RocketItem>();

            stripedAnim.skeletonAnimation.gameObject.SetActive(false);
        }

        private void DisablePackageAnimation(Item item1, Item item2)
        {
            var packageAnim = (item1.currentType != ItemsTypes.DiscoBall)
                ? item1.GetComponent<Item>().GetTopItemInterface().GetGameobject().GetComponent<BombItem>()
                : item2.GetComponent<Item>().GetTopItemInterface().GetGameobject().GetComponent<BombItem>();

            packageAnim.skeletonAnimation.gameObject.SetActive(false);
        }

        private void DisableChopperAnimation(Item item1, Item item2)
        {
            var chopperAnim = (item1.currentType != ItemsTypes.DiscoBall)
                ? item1.GetComponent<Item>().GetTopItemInterface().GetGameobject().GetComponent<ChopperItem>()
                : item2.GetComponent<Item>().GetTopItemInterface().GetGameobject().GetComponent<ChopperItem>();

            chopperAnim.HeliAnimationSpine.gameObject.SetActive(false);
        }

        private void DisableSpineAnimations(Item[] items)
        {
            foreach (var item in items)
            {
                if (item != null && item.gameObject.activeSelf)
                {
                    if (_itemSimplePool.TryGetValue(item.GetComponent<Item>()?.GetTopItemInterface()?.GetGameobject(),
                            out var itemSimpleComponent) &&
                        itemSimpleComponent != null && itemSimpleComponent.skeletonAnimation != null)
                    {
                        // Animation handling code would go here if needed
                    }
                }
            }
        }

        // Reset item state before destroying
        public void ResetItem(Item item)
        {
            if (item != null && item.gameObject.activeSelf)
            {
                if (_itemSimplePool.TryGetValue(item.GetComponent<Item>()?.GetTopItemInterface()?.GetGameobject(),
                        out var itemSimpleComponent) &&
                    itemSimpleComponent != null && itemSimpleComponent.skeletonAnimation != null)
                {
                    // Reset animation code would go here if needed
                }
            }
        }

        private void CreateLightning(Vector3 pos1, Vector3 pos2, int ColorID, Action onLightningReachedTarget)
        {
            var go = Instantiate(LightningPrefab, Vector3.zero, Quaternion.identity);
            var lightning = go.GetComponent<LightningEffect>();
            lightning.SetLight(pos1, pos2, ColorID);
            lightning.OnLightningReachedTarget += onLightningReachedTarget;
        }

        private Color GetColorById(int colorID)
        {
            // Define a color array where 0 = red, 1 = yellow, etc.
            Color[] colors = { Color.red, Color.yellow, Color.green, Color.blue, Color.red };

            // Return corresponding color with alpha adjustment
            if (colorID >= 0 && colorID < colors.Length)
            {
                Color selectedColor = colors[colorID];
                selectedColor.a = 0.42f;
                return selectedColor;
            }

            return new Color(0, 0, 0, 0.5f); // Default color
        }

        #endregion

        #region Double Multicolor

        public void DestroyDoubleMulticolor(int col, Action callback)
        {
            MainManager.Instance.field.GetItems();
            mask.gameObject.SetActive(true);
            MultiColorAnimator.SetTrigger("StartMerge");
            StartCoroutine(DestroyDoubleBombCor(col, callback));
        }

        private IEnumerator DestroyDoubleBombCor(int col, Action callback)
        {
            yield return _destroyBombDelay;

            OnShakeRequested?.Invoke(0.2f, 0.35f);
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);
            var levelManager = MainManager.Instance;
            var levelData = levelManager.levelData;
            var additionalSettings = levelManager.AdditionalSettings;

            for (var i = 0; i < levelManager.field.fieldData.maxCols; i++)
            {
                // Process all items in column
                var columnItems = levelManager.GetColumn(i).Where(a => !a.destroying).ToList();

                foreach (var item in columnItems)
                {
                    if (item.currentType == ItemsTypes.Chopper)
                        item.GetComponent<ChopperItem>().noChopperLaunch = true;

                    if (targetSpread)
                        item.square?.SetType(LevelTargetTypes.ExtraTargetType2, 1, LevelTargetTypes.NONE, 1);

                    item.DestroyItem(true, true, this, true, WithoutShrink: true, destroyNeighbours: false);
                }

                // Destroy solid blocks
                if (true)
                {
                    var breakableBox = levelManager.GetColumnSquare(i);
                    foreach (var square in breakableBox)
                    {
                        square.DestroyBlock();
                    }
                }
            }

            callback?.Invoke();
        }

        public void SetUpMergeEffect(int sortingOrder = 59)
        {
            if (skeletonRenderer == null)
            {
                skeletonRenderer = skeletonAnimation.GetComponent<MeshRenderer>();
            }

            if (skeletonRenderer != null)
            {
                skeletonRenderer.sortingLayerName = "Spine";
                skeletonRenderer.sortingOrder = sortingOrder;
            }
            else
            {
                Debug.LogWarning("SkeletonAnimation component not found on object.");
            }
        }

        public void ElectroItemVFX()
        {
            // VFX implementation would go here
        }

        #endregion

        #region IItemInterface Implementation

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

        #endregion
    }
}
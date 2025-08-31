

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Spine.Unity;
using UnityEngine;
using Internal.Scripts.Blocks;

namespace Internal.Scripts.Items
{
    /// <summary>
    /// Striped item
    /// </summary>
    public class RocketItem : Item, IItemInterface
    {
        private Item itemMain;

        // public bool Combinable;
        public bool ActivateByExplosion;
        public bool StaticOnStart;
        public GameObject explosionPrefab;
        public SpriteRenderer spriteRenderer;

        public SkeletonAnimation skeletonAnimation;

        private Item savedItem1;
        private Item savedItem2;


        public Item PackageItem;
        public Item combItem;
        public ItemsTypes interactionType;
        private bool _isSwitchItemOverChopper;

        private new Item GetItem => GetComponentInParent<Item>();

        public Vector3 SwitchItemDirection { get; private set; }

        public delegate void ShakeAction(float duration, float strength);

        public static event ShakeAction OnShakeRequested;


        public void Destroy(Item item1, Item item2, bool isPackageCombined = false, bool destroyNeighboours = false,
            int color = 0, bool isCombinedwithMulti = false)
        {
            if (item1 != null && item2 == null)
                MainManager.Instance.StopedAI();
            DestroyCor(item1, item2);
        }

        private void DestroyCor(Item item1, Item item2, bool destroyNeighbours = false)
        {
            SortAndSetItems(item1, item2);

            PlayExplosionEffects();

            StartCoroutine(CleanupDestruction());
        }

        private bool InitializeDestruction()
        {
            var mainSquare = GetItem.square;
            mainSquare.DestroyBlock();

            if (mainSquare.type == LevelTargetTypes.ExtraTargetType1)
            {
                GetItem.StopDestroy();
                return false;
            }

            return true;
        }

        private void SortAndSetItems(Item item1, Item item2)
        {
            var items = new[] { item1, item2 }.OrderBy(i => i != GetItem).ToArray();
            itemMain = items[0];
        }

        private void PlayExplosionEffects()
        {
            MainManager.HapticAndShake(1);
            CentralSoundManager.Instance?.PlayLimitSound(CentralSoundManager.Instance.strippedExplosion);
            square.DestroyBlock(destroyNeighbour: false);
            MainManager.Instance.StripedShow(gameObject, square, itemMain.currentType == ItemsTypes.RocketHorizontal);
        }

        private void DestroyItemsInList(bool destroyNeighbours)
        {
            var destroyList = GetList(itemMain.square);
            foreach (var item in destroyList.Where(i => i != null))
            {
                item.DestroyItem(false, GetItem, this, destroyNeighbours);
            }
        }

        private void HandleSpecialBlocks(bool destroyNeighbours)
        {
            var affectedSquares = GetSquaresInRow(itemMain.square, itemMain.currentType);

            if (affectedSquares.Any(sq => sq.type == LevelTargetTypes.ExtraTargetType2))
            {
                MainManager.Instance.levelData.GetTargetObject().CheckSquares(affectedSquares.ToArray());
            }

            foreach (var emptySquare in affectedSquares.Where(sq => sq.item == null))
            {
                emptySquare.DestroyBlock(destroyNeighbours);
            }
        }

        private IEnumerator CleanupDestruction()
        {
            yield return null;
            yield return null;
            DestroyBehaviour();
        }

        private List<Rectangle> GetSquaresInRow(Rectangle rectangle, ItemsTypes type)
        {
            if (type == ItemsTypes.RocketHorizontal)
                return MainManager.Instance.GetRowSquare(rectangle.row);
            return MainManager.Instance.GetColumnSquare(rectangle.col);
        }

        public Item GetParentItem()
        {
            return transform.GetComponentInParent<Item>();
        }

        private List<Item> GetList(Rectangle rectangle)
        {
            if (itemMain.currentType == ItemsTypes.RocketHorizontal)
                return MainManager.Instance.GetRow(rectangle);
            return MainManager.Instance.GetColumn(rectangle);
        }

        public override void Check(Item item1, Item item2)
        {
            // Log item1 and item2 details
            Debug.Log($"Check called with item1: {item1}, item2: {item2}");

            // Determine the interaction direction and stripe behavior
            Debug.Log($"salllog mergeSwitchDirection: {SwitchItemDirection}");

            // Check for stripe-stripe combination
            if (gameObject.activeSelf && (item2.currentType == ItemsTypes.Bomb ||
                                          item2.currentType == ItemsTypes.RocketVertical ||
                                          item2.currentType == ItemsTypes.RocketHorizontal))
            {
                EvaluateItems(item1, item2);
                if (item2.currentType == ItemsTypes.Bomb || item1.currentType == ItemsTypes.Bomb)
                    CheckStripePackage(item1, item2);

                return;
            }

            // Handle special item interactions (e.g., rockets, bombs)
            HandleSpecialItemTypes(item1, item2);
        }

        private BombItem _item1;
        private Item _item2;

        private void EvaluateItems(Item item1, Item item2)
        {
            // Handle switching direction and determining if one item is a Chopper
            CheckDirection(item1, item2);
            Debug.Log($"salllog mergeSwitchDirection : {SwitchItemDirection}");
            // Check if both items are stripes and handle accordingly
            if ((item1.currentType == ItemsTypes.RocketVertical || item1.currentType == ItemsTypes.RocketHorizontal) &&
                (item2.currentType == ItemsTypes.RocketVertical || item2.currentType == ItemsTypes.RocketHorizontal))
            {
                CheckStripes(item1, item2);
            }
        }


        /// <summary>
        /// Handles switching of special items like Chopper or Multicolor.
        /// </summary>
        private void HandleSpecialItemTypes(Item item1, Item item2)
        {
            if (item2.currentType == ItemsTypes.DiscoBall)
            {
                Debug.Log("salStrip");
                item2.Check(item2, item1);
            }

            if (item2.currentType == ItemsTypes.Chopper)
            {
                Debug.Log("salStrip");
                item2.Check(item1, item2);
            }
        }

        public void TriggerStripedAndBomb()
        {
            if (_item2 != null && _item1 != null)
            {
                HandleStripePackageCombo(_item2, _item1, _item1, _item2);
            }
        }

        /// <summary>
        /// Coroutine to handle the checking and animation of Stripe + Package combo.
        /// </summary>
        private void CheckStripePackage(Item item1, Item item2)
        {
            Item[] itemList = { item1, item2 };
            _item2 = GetStripedItem(itemList);
            _item1 = GetPackageItem(itemList) as BombItem;

            Debug.Log($"salllog mergeSwitchDirection : {SwitchItemDirection}");
            SetAnimation(SwitchItemDirection, _isSwitchItemOverChopper, item1, item2);


            DisableStripedAnimation(item1, item2);

           StartCoroutine(DelayedTriggerStripedAndBomb());
        }

        private IEnumerator DelayedTriggerStripedAndBomb()
  {
      yield return new WaitForSeconds(0.55f); 
      TriggerStripedAndBomb();
  }

        /// <summary>
        /// Retrieves the first item of type Striped from the list.
        /// </summary>
        private Item GetStripedItem(Item[] itemList) =>
            itemList.FirstOrDefault(i =>
                i.currentType == ItemsTypes.RocketHorizontal || i.currentType == ItemsTypes.RocketVertical);

        /// <summary>
        /// Retrieves the first item of type Package from the list.
        /// </summary>
        private Item GetPackageItem(Item[] itemList) =>
            itemList.FirstOrDefault(i => i.currentType == ItemsTypes.Bomb);

        /// <summary>
        /// Disables the animation for striped items.
        /// </summary>
        private void DisableStripedAnimation(Item item1, Item item2)
        {
            var stripedAnim = (item1.currentType != ItemsTypes.Bomb)
                ? item1.GetTopItemInterface().GetGameobject().GetComponent<RocketItem>()
                : item2.GetTopItemInterface().GetGameobject().GetComponent<RocketItem>();

            stripedAnim.skeletonAnimation.gameObject.SetActive(false);
        }


        /// <summary>
        /// Handles the Stripe + Package combination effect.
        /// </summary>
        private void HandleStripePackageCombo(Item striped, Item package, Item item1, Item item2)
        {
            var centerSquare = item2.square;
            if (!_isSwitchItemOverChopper)
                centerSquare = item1.square;
            OnShakeRequested?.Invoke(0.2f, 0.35f);
            package.NoAnimSmoothDestroy();
            striped.NoAnimSmoothDestroy();

            CentralSoundManager.Instance?.PlayLimitSound(CentralSoundManager.Instance.bombExplodeEffect);
            ShowVisualEffects(centerSquare);

            var affectedSquares = GetAffectedSquares(package.square);
            HandleSquareDestruction(affectedSquares, item1, item2, striped, package);
        }

        /// <summary>
        /// Displays visual effects in a cross pattern around the center square.
        /// </summary>
        private void ShowVisualEffects(Rectangle centerRectangle)
        {
            var verticalStripes = new List<Rectangle>
            {
                centerRectangle,
                centerRectangle.GetNeighborTop(centerRectangle),
                centerRectangle.GetNeighborBottom(centerRectangle)
            };

            var horizontalStripes = new List<Rectangle>
            {
                centerRectangle,
                centerRectangle.GetNeighborLeft(centerRectangle),
                centerRectangle.GetNeighborRight(centerRectangle)
            };

            foreach (var square in verticalStripes)
                if (square != null)
                    MainManager.Instance.StripedShow(square.gameObject, square, true);

            foreach (var square in horizontalStripes)
                if (square != null)
                    MainManager.Instance.StripedShow(square.gameObject, square, false);
        }

        /// <summary>
        /// Retrieves affected squares around the package square.
        /// </summary>
        private List<Rectangle> GetAffectedSquares(Rectangle packageRectangle)
        {
            var surroundingSquares = MainManager.Instance.GetSquaresAroundSquareSpiral(packageRectangle);
            var affectedSquares = new List<Rectangle>();

            foreach (var square in surroundingSquares)
            {
                if (square != null)
                {
                    var horizontalSquares = GetSquaresInRow(square, ItemsTypes.RocketHorizontal);
                    var verticalSquares = GetSquaresInRow(square, ItemsTypes.RocketVertical);
                    affectedSquares = affectedSquares.Union(horizontalSquares.Union(verticalSquares)).ToList();
                }
            }

            return affectedSquares;
        }

        /// <summary>
        /// Handles the destruction of affected squares and items.
        /// </summary>
        private void HandleSquareDestruction(List<Rectangle> affectedSquares, Item item1, Item item2, Item striped,
            Item package)
        {
            if (affectedSquares.Any(sq => sq.type == LevelTargetTypes.ExtraTargetType2))
            {
                MainManager.Instance.levelData.GetTargetObject().CheckSquares(affectedSquares.ToArray());
            }

            var uniqueSquaresToDestroy = affectedSquares.Distinct();
            var itemsToDestroy = uniqueSquaresToDestroy
                .Where(sq => sq.Item != null)
                .Select(sq => sq.Item);

            item1.destroying = true;
            item2.destroying = true;

            // Adding small delay before destruction
            StartCoroutine(DestroyItemsWithDelay(itemsToDestroy, uniqueSquaresToDestroy, striped, package));
        }

        /// <summary>
        /// Coroutine to destroy items with a delay.
        /// </summary>
        private IEnumerator DestroyItemsWithDelay(IEnumerable<Item> itemsToDestroy,
            IEnumerable<Rectangle> uniqueSquaresToDestroy, Item striped, Item package)
        {
            yield return new WaitForSeconds(0.1f);

            foreach (var item in itemsToDestroy)
            {
                item.destroyNext = true;
                item.DestroyItem(true, this, this);
            }

            foreach (var square in uniqueSquaresToDestroy)
            {
                if (square.Item == null)
                    square.DestroyBlock();
            }

            CentralSoundManager.Instance?.PlayLimitSound(CentralSoundManager.Instance.explosion2);
            striped.square.DestroyBlock();
            package.square.DestroyBlock();

            //  LevelManager.THIS.levelData.GetTargetObject().CheckItems(new[] { striped, package });
            striped.DestroyBehaviour();
            package.DestroyBehaviour();
            //LevelManager.THIS.FindMatches();
        }


        /// <summary>
        /// Checks if both items are striped and handles their destruction.
        /// </summary>
        private void CheckStripes(Item item1, Item item2)
        {
            // Collect items and filter for striped ones
            var stripedItems = new[] { item1, item2 }
                .Where(i => i.currentType == ItemsTypes.RocketHorizontal || i.currentType == ItemsTypes.RocketVertical)
                .OrderBy(i => i == GetItem)
                .ToList();

            // Exit if there are not exactly two striped items
            if (stripedItems.Count < 2) return;

            // Mark items for destruction
            item1.destroying = true;
            item2.destroying = true;
            item2?.DestroyBehaviour();

            // Play explosion sound and display striped effects
            CentralSoundManager.Instance?.PlayLimitSound(CentralSoundManager.Instance.strippedExplosion);
            MainManager.Instance.StripedShow(gameObject, square, false);
            MainManager.Instance.StripedShow(gameObject, square, true);

            // Get all distinct squares affected by horizontal and vertical stripes
            var horizontalSquares = GetSquaresInRow(GetItem.square, ItemsTypes.RocketHorizontal);
            var verticalSquares = GetSquaresInRow(GetItem.square, ItemsTypes.RocketVertical);
            var affectedSquares = horizontalSquares.Union(verticalSquares).Distinct();


            if (affectedSquares.Any(sq => sq.type == LevelTargetTypes.ExtraTargetType2))
            {
                MainManager.Instance.levelData.GetTargetObject().CheckSquares(affectedSquares.ToArray());
            }

            // Destroy items and blocks in affected squares
            foreach (var square in affectedSquares)
            {
                if (square.Item != null)
                    square.Item.DestroyItem(true, this, this);
                else
                    square.DestroyBlock();
            }

            // Check for any remaining target items
            // LevelManager.THIS.levelData.GetTargetObject().CheckItems(new[] { item1, item2 });

            // Final destruction of item1's square and behavior
            item1.square.DestroyBlock();
            item1?.DestroyBehaviour();
        }

        public GameObject GetGameobject()
        {
            return gameObject;
        }

        private IEnumerator Timer(float sec, Action callback)
        {
            yield return new WaitForSeconds(sec);
            callback();
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

        public void SecondPartDestroyAnimation(Action callback)
        {
            throw new NotImplementedException();
        }

        void CheckDirection(Item item1, Item item2)
        {
            Debug.Log($"CheckDirection: item1 = {item1.currentType}, item2 = {item2.currentType}");

            // Swap items if item2 is a PACKAGE
            bool isPackage = item2.currentType == ItemsTypes.Bomb;
            if (isPackage)
            {
                (item1, item2) = (item2, item1); // Swap for consistent logic
                _isSwitchItemOverChopper = true;
            }
            else
            {
                _isSwitchItemOverChopper = false;
            }

            // Determine the swap direction using squares
            Rectangle square1 = item1.square;
            Rectangle square2 = item2.square;

            if (square1.GetNeighborRight() == square2)
            {
                SwitchItemDirection = Vector3Int.right; // Swapped from left to right
            }
            else if (square1.GetNeighborLeft() == square2)
            {
                SwitchItemDirection = Vector3Int.left; // Swapped from right to left
            }
            else if (square1.GetNeighborTop() == square2)
            {
                SwitchItemDirection = Vector3Int.up; // Swapped from bottom to top
            }
            else if (square1.GetNeighborBottom() == square2)
            {
                SwitchItemDirection = Vector3Int.down; // Swapped from top to bottom
            }
            else
            {
                Debug.LogWarning("Items are not neighbors!");
                SwitchItemDirection = Vector3Int.zero; // No valid direction
            }

            // Reverse direction if isSwitchItemOverChopper is true
            if (_isSwitchItemOverChopper)
            {
                SwitchItemDirection = -SwitchItemDirection; // Reverse the vector
            }

            SaveItemsForStripe(item1, item2);
        }


        private void SaveItemsForStripe(Item item1, Item item2)
        {
            savedItem1 = item1;
            savedItem2 = item2;
        }

        private void SetAnimation(Vector3 mergeSwitchDirection, bool isSwitchItemOverChopper, Item item1, Item item2)
        {
            MainManager.Instance.StartBusyOperation();

            var package = (item2.currentType == ItemsTypes.Bomb) ? item2 : item1;
            var comb = (item2.currentType == ItemsTypes.Bomb) ? item1 : item2;

            var packageComponent = package.GetComponent<Item>().GetTopItemInterface().GetGameobject()
                .GetComponent<BombItem>();
            var packageAnimator = packageComponent.PackageAnimator;
            packageAnimator.gameObject.SetActive(true);

            interactionType = comb.currentType;
            PackageItem = package;
            combItem = comb;

            packageAnimator.SetTrigger("PackageMerge");
            Debug.Log(
                $"SetAnimation: item1 = {item1.currentType}, item2 = {item2.currentType}, Direction = {mergeSwitchDirection}");

            SetAnimatorParameters(packageAnimator, isSwitchItemOverChopper, mergeSwitchDirection, comb, package);
            TriggerMergeAnimation(packageAnimator, interactionType, mergeSwitchDirection, isSwitchItemOverChopper);
        }

        private void SetAnimatorParameters(Animator animator, bool isOverChopper, Vector3 mergeSwitchDirection,
            Item comb, Item Package)
        {
            if (mergeSwitchDirection == Vector3.zero)
            {
                animator.SetInteger("SwitchDirection.x", 1);
                animator.SetBool("IsSwitchItemOverChopper", false);
            }
            else
            {
                if (isOverChopper == true)
                {
                    animator.SetInteger("SwitchDirection.x", (int)mergeSwitchDirection.x);
                    animator.SetInteger("SwitchDirection.y", (int)mergeSwitchDirection.y);
                    animator.SetBool("IsSwitchItemOverChopper", _isSwitchItemOverChopper);
                }
                else
                {
                    animator.SetInteger("SwitchDirection.x", (int)mergeSwitchDirection.x);
                    animator.SetInteger("SwitchDirection.y", (int)mergeSwitchDirection.y);
                    animator.SetBool("IsSwitchItemOverChopper", _isSwitchItemOverChopper);
                }
            }
        }

        private void TriggerMergeAnimation(Animator animator, ItemsTypes interactionType, Vector3 mergeSwitchDirection,
            bool isOverChopper)
        {
            string animationTrigger = interactionType switch
            {
                ItemsTypes.RocketHorizontal => "MergeHorRocket",
                ItemsTypes.RocketVertical => "MergeVerRocket",
                ItemsTypes.Bomb or ItemsTypes.NONE => string.Empty,
                _ => "UndefinedMerge"
            };

            if (!string.IsNullOrEmpty(animationTrigger))
            {
                animator.SetTrigger(animationTrigger);
            }
        }
    }
}
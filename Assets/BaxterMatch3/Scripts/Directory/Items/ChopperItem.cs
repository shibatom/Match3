using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Spine.Unity;
using Internal.Scripts.System;
using UnityEngine;
using System.Runtime.CompilerServices;
using Internal.Scripts.Blocks;
using Internal.Scripts.System.Pool;
using UnityEngine.Serialization;

namespace Internal.Scripts.Items
{
    /// <summary>
    /// Class handling Chopper item behavior in the game.
    /// Implements IItemInterface to ensure Chopper interacts with other items properly.
    /// </summary>
    public class ChopperItem : Item, IItemInterface
    {
        public bool ActivateByExplosion; // Indicates if Chopper is activated by an explosion.
        public bool StaticOnStart; // If true, the Chopper doesn't move when the game starts.
        public bool noChopperLaunch; // If true, the Chopper will not launch to other items.
        public ChopperFly secondItem; // Secondary Chopper object used in combined actions.
        public List<ChopperFly> choppers; // List of Chopper objects used for flying to targets.
        private bool _destroyStarted; // Tracks if the destruction process has started to avoid duplicate calls.
        public GameObject chopperSubParrent; // Parent object for Chopper sub-objects.
        public SkeletonMecanim HeliAnimationSpine; // Animation for Chopper flying effect.
        public Animator HeliAnimator;
        private bool _isSwitchItemOverChopper;
        private bool _isChopperSwitchWithPowerUp;
        private Vector3 _switchItemDirection;
        private Item _savedItem1;
        private bool _isChopperCombine;
        private int _chopperIndex; // index for each helicopter
        private Item _savedItem2;
        public GameObject fireworkStar;


        /// <summary>
        /// Handles destruction of this item and potentially destroys nearby items depending on the item combination.
        /// </summary>
        /// <param name="item1">First item involved in the destruction.</param>
        /// <param name="item2">Second item involved in the destruction.</param>
        /// <param name="isPackageCombined">Indicates if this is part of a package combination.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Destroy(Item item1, Item item2, bool isPackageCombined = false, bool destroyNeighboours = true, int color = 0, bool isCombinedwithMulti = false)
        {
            if (item1 != null && item2 == null)
                MainManager.Instance.StopedAI();
            HandleWireBlock(); // Check and handle WireBlock scenario

            MarkItemForDestruction(item1); // Mark item1 for destruction

            var switchItemType = item2?.currentType ?? ItemsTypes.NONE;

            //  var SwitchItemDirection = item2?.switchDirection ?? new Vector3(0,0,0);

            LaunchChopperIfNeeded(switchItemType, _switchItemDirection); // Launch Chopper if conditions are met

            HandleItemDestruction(item2, switchItemType); // Handle destruction based on item type
            if (!isCombinedwithMulti)
                DestroySurroundingItems(item1, item2); // Destroy surrounding blocks and items

            CentralSoundManager.Instance.PlayOneShot(CentralSoundManager.Instance.chopperExplodeEffect);
        }

        private void HandleWireBlock()
        {
            if (GetParentItem().square?.type == LevelTargetTypes.ExtraTargetType1)
            {
                GetParentItem().square.DestroyBlock(); // Destroy WireBlock
                return;
            }
        }

        private void MarkItemForDestruction(Item item)
        {
            item.destroying = true; // Mark item for destruction
        }

        private void LaunchChopperIfNeeded(ItemsTypes SecenderyItemType, Vector3 SwitchDirection)
        {
            if (!noChopperLaunch && !_destroyStarted)
            {
                _destroyStarted = true;
                MainManager.HapticAndShake(1);
                CreateChopper(SecenderyItemType, SwitchDirection); // Launch Chopper unless conditions prevent it
            }
        }

        private void HandleItemDestruction(Item item, ItemsTypes SwitchitemType)
        {
            if (SwitchitemType == ItemsTypes.Chopper)
            {
                item?.GetTopItemInterface()?.Destroy(item, null); // Recursive destruction for Chopper
            }
            else if (SwitchitemType != ItemsTypes.NONE)
            {
                //Debug.Log("ItemChopper: SmoothDestroy for item2");
                item?.HideSprites(true);
                item?.DestroyBehaviour(); // Destroy behavior based on item type
            }
            else if (SwitchitemType == ItemsTypes.DiscoBall)
            {
                item?.DestroyBehaviour();
            }
            else if (noChopperLaunch)
            {
                DestroyBehaviour();
            }
        }

        private void DestroySurroundingItems(Item item, Item switchedItem)
        {
            // Debug.Log($"ItemChopper: DestroySurroundingItems started for item: {item?.name}, switchedItem: {switchedItem?.name}");
            var parentSquare = GetParentItem()?.square;
            // Debug.Log($"ItemChopper: Parent square is {parentSquare?.name}");

            if (parentSquare == null)
            {
                // Debug.LogWarning("ItemChopper: Parent square is null. Cannot destroy surrounding items.");
                return; // Exit if parent square is null
            }


            // Collect neighboring squares in an array
            var neighbors = new[]
            {
                parentSquare.GetNeighborBottom(),
                parentSquare.GetNeighborTop(),
                parentSquare.GetNeighborLeft(),
                parentSquare.GetNeighborRight()
            };

            // Iterate through neighbors and destroy items if not the switched item
            foreach (var neighborSquare in neighbors)
            {
                if (neighborSquare == null) continue; // Skip null neighbors

                var neighborItem = neighborSquare.Item; // Get item from square
                // Check if the neighbor item is the switched item
                if (neighborItem != switchedItem)
                {
                    // Debug.Log($"ItemChopper: Destroying neighbor block: {neighborSquare?.name}");
                    neighborSquare.DestroyBlock(destroyTarget: true, destroyNeighbour: false); // Destroy the block/square
                    neighborSquare.item?.DestroyItem(); // Destroy the item if it exists
                }
                else
                {
                    Debug.Log($"ItemChopper: Skipping destruction of switched item: {neighborItem?.name} at square {neighborSquare?.name}");
                }
            }

            // Destroy the parent block if necessary
            // Debug.Log($"ItemChopper: Destroying parent block at square {parentSquare?.name}");
            if (parentSquare.type == LevelTargetTypes.BreakableBox)
                parentSquare.DestroyBlock(destroyNeighbour: true); // parentSquare is checked for null earlier

            if (item != null) // Check if item is not null
            {
                OnDestroyItem(item);
            }
            // Debug.Log($"ItemChopper: DestroySurroundingItems finished for item: {item?.name}");
        }


        /// <summary>
        /// Instantiates a Chopper and configures its behavior based on the given item type.
        /// </summary>
        /// <param name="itemsType">Type of the item the Chopper will act upon.</param>
        private void CreateChopper(ItemsTypes SecenderyItemType, Vector3 SwitchDirection)
        {
            //Debug.Log("ItemChopper: CreateChopper called with itemsType = " + SecenderyItemType);
            CheckIfMaramaldeCombine(SecenderyItemType);
            LaunchChopper(SecenderyItemType, SwitchDirection);
        }

        void CheckIfMaramaldeCombine(ItemsTypes itemsType)
        {
            // Handle Chopper-specific logic if the item type is Chopper.
            if (itemsType == ItemsTypes.Chopper)
            {
                var bonusChopper = Instantiate(choppers[0]); // Instantiate the first Chopper in the list.
                Transform transform1;
                (transform1 = bonusChopper.transform).SetParent(chopperSubParrent.transform); // Set parent transform.
                transform1.localPosition = Vector3.zero;
                transform1.localScale = Vector3.one;
                bonusChopper.gameObject.SetActive(true); // Activate Chopper object.
                bonusChopper.HeliAnimator.SetTrigger("InstanCombHeli");
                choppers.Add(bonusChopper.GetComponent<ChopperFly>()); // Add to the list.
                secondItem = bonusChopper; // Set secondary item for combination.
                HeliAnimator.SetTrigger("StartFly"); // Play flying animation
            }
        }

        private void LaunchChopper(ItemsTypes SecenderyItemType, Vector3 SwitchDirectoin)
        {
            int index = 0;


            if (choppers[0].ChopperIndex == 2)
            {
                choppers[1].ChopperIndex = 3;
            }

            // Configure each Chopper's flight and effects
            foreach (var chopper in choppers)
            {
                ConfigureChopperTargets(chopper);
                SetNextItemType(chopper, SecenderyItemType);
                HandleTargetType2Block(chopper);
                SetChopperDirection(chopper);
                PrepareChopperForLaunch(chopper, SwitchDirectoin);

                index++;
                //   yield return new WaitForSeconds(0.0f);
            }

            //Debug.Log("Starting WaitForReachTarget coroutine");
            StartCoroutine(WaitForReachTarget()); // Wait until all Choppers reach their targets
        }

        private void ConfigureChopperTargets(ChopperFly chopper)
        {
            chopper.targets = GetParentItem().itemForEditor.TargetChopperPositions; // Set target positions
        }

        private void SetNextItemType(ChopperFly chopper, ItemsTypes itemsType)
        {
            // Set the next item type if it isn't Chopper, multicolor, or ingredient
            if (itemsType != ItemsTypes.Chopper &&
                itemsType != ItemsTypes.DiscoBall &&
                itemsType != ItemsTypes.Gredient)
            {
                chopper.nextItemType = itemsType;
            }


            //Debug.Log($"ItemChopper: Chopper Lunching With {itemsType}");
            chopper.interactionType = itemsType; // Store the type for later animation setting
        }


        private void HandleTargetType2Block(ChopperFly chopper)
        {
            var parentSquareType = GetParentItem().square?.type;
            var lastSwitchedSquareType = MainManager.Instance.lastSwitchedItem?.square?.type;

            if (parentSquareType == LevelTargetTypes.ExtraTargetType2 || lastSwitchedSquareType == LevelTargetTypes.ExtraTargetType2)
            {
                chopper.setTargetType2 = true; // Special handling if inside a TargetType2
            }
        }

        private void SetChopperDirection(ChopperFly chopper)
        {
            var direction = UnityEngine.Random.value >= 0.5f ? Vector2.left : Vector2.right;
            //Debug.Log($"ItemChopper: Chopper direction set to {direction}");

            chopper.SetDirection(direction, this, _isChopperCombine); // Randomly choose a direction
        }

        private void PrepareChopperForLaunch(ChopperFly chopper, Vector3 SwitchDirectoin)
        {
            chopper.transform.SetParent(null);
            HeliAnimationSpine.gameObject.SetActive(true); // Enable animation

            CreateFireworkEffect();
            // HeliAnimator.SetTrigger("StartFly"); // Play flying animation
            chopper.StartFly(SwitchDirectoin, _isSwitchItemOverChopper, _savedItem1, _savedItem2); // Start Chopper flight
        }

        private void CreateFireworkEffect()
        {
            fireworkStar = ObjectPoolManager.Instance.GetPooledObject("FireLight", this);
            if (fireworkStar != null)
            {
                // fireworkStar.transform.SetParent(transform);
                fireworkStar.transform.position = transform.position; // Create firework effect at launch position
            }
        }


        /// <summary>
        /// Waits until all Choppers reach their target before completing destruction.
        /// </summary>
        IEnumerator WaitForReachTarget()
        {
            yield return new WaitWhile(() => choppers.Any(i => i.gameObject.activeSelf));

            // Destroy secondary item if it exists and clean up the Chopper list.
            if (secondItem != null)
                Destroy(secondItem.gameObject);
            if (choppers.Count > 1)
                choppers.Remove(choppers.Last());

            DestroyBehaviour(); // Final destruction behavior.
            HeliAnimationSpine.gameObject.SetActive(false); // Hide animation.
        }

        // private void OnEnable()
        // {
        //       HeliAnimationSpine.gameObject.SetActive(true); // Enable animation
        //
        // }
        /// <summary>
        /// Cleanup when the Chopper object is disabled.
        /// </summary>
        private void OnDisable()
        {
            // ObjectPooler.Instance?.PutBack(fireworkStar); // Return firework effect to the pool
            Destroy(fireworkStar); // Destroy firework effect
            if (square?.Item == this && MainManager.Instance.gameStatus != GameState.RegenLevel)
                square.Item = null; // Reset the item reference if this is the current item.
            _isChopperCombine = false; // Reset Chopper combine state.
        }

        /// <summary>
        /// Initializes the Chopper item when the game starts.
        /// </summary>
        public override void InitItem()
        {
            _destroyStarted = false;
            noChopperLaunch = false; // Reset Chopper launch state.
            _isSwitchItemOverChopper = false;
            choppers.ForEachY(i => i.gameObject.SetActive(true)); // Enable all Choppers.
            base.InitItem();
        }

        /// <summary>
        /// Checks if two items can be combined and triggers their destruction if applicable.
        /// </summary>
        public override void Check(Item item1, Item item2)
        {
            if (!(item1.currentType == ItemsTypes.Chopper && item2.currentType == ItemsTypes.Chopper))
            {
                //Debug.Log("ItemChopper: Check called with item1 = " + item1 + ", item2 = " + item2);
                //Debug.Log("ItemChopper: Check called with item1 = " + item1.switchDirection + ", item2 = " + item2.switchDirection);

                // Check if item2 is Chopper, and if so, switch it with item1.
                if (item2.currentType == ItemsTypes.Chopper)
                {
                    // Determine direction using squares
                    Rectangle square1 = item1.square;
                    Rectangle square2 = item2.square;

                    // Set direction based on neighbors
                    if (square1.GetNeighborRight() == square2)
                    {
                        _switchItemDirection = new Vector3Int(1, 0, 0); // Swapped from left to right
                    }
                    else if (square1.GetNeighborLeft() == square2)
                    {
                        _switchItemDirection = new Vector3Int(-1, 0, 0); // Swapped from right to left
                    }
                    else if (square1.GetNeighborTop() == square2)
                    {
                        _switchItemDirection = new Vector3Int(0, 1, 0); // Swapped from bottom to top
                    }
                    else if (square1.GetNeighborBottom() == square2)
                    {
                        _switchItemDirection = new Vector3Int(0, -1, 0); // Swapped from top to bottom
                    }
                    else
                    {
                        //Debug.LogWarning("Items are not neighbors!");
                        _switchItemDirection = Vector3Int.zero; // No valid direction
                    }

                    // Reverse direction if isSwitchItemOverChopper is true
                    if (_isSwitchItemOverChopper)
                    {
                        _switchItemDirection = -_switchItemDirection; // Reverse the vector
                    }

                    // Swap items
                    (item1, item2) = (item2, item1);
                    //Debug.Log("ItemChopper: Switched item1 and item2 because item2 is Chopper.");
                    _isSwitchItemOverChopper = true;
                }
                else
                {
                    // Use item1's switch direction if not swapping with Chopper
                    _switchItemDirection = item1.switchDirection;
                }

                //Debug.Log($"itemChopper: Item1 is {item1} , and switching direction is {item1.switchDirection} and item2 is {item2} , and switching is {item2.switchDirection}");
            }

            // Determine if items are both Chopper
            if (item1.currentType == ItemsTypes.Chopper && item2.currentType == ItemsTypes.Chopper)
            {
                var chopper1 = item1.GetComponent<Item>().GetTopItemInterface().GetGameobject().GetComponent<ChopperItem>();
                var chopper2 = item2.GetComponent<Item>().GetTopItemInterface().GetGameobject().GetComponent<ChopperItem>();

                if (chopper1 != null) chopper1._isChopperCombine = true;
                if (chopper2 != null) chopper2._isChopperCombine = true;
                _isChopperCombine = true;

                // Correct handling for merging two Choppers
                MergeChoppers(chopper1, chopper2); // New method to combine Choppers
            }


            // Save item1 and item2 for Chopper
            SaveItemsForChopper(item1, item2);

            // Special handling for multicolor items.
            if (item2.currentType == ItemsTypes.DiscoBall)
            {
                item1.DestroyBehaviour();
                item2.Check(item2, item1); // Recursively call check on multicolor item.
            }
            else if (item2.currentType != ItemsTypes.NONE)
            {
                Destroy(item1, item2); // Destroy items if they can be combined.
            }

            //LevelManager.THIS.FindMatches(); // Find new matches after destruction.
        }


        private void MergeChoppers(ChopperItem chopper1, ChopperItem chopper2)
        {
            if (chopper2 != null)
            {
                var chopperFly = chopper2.choppers[0];
                chopperFly.ChopperIndex = 1; // Assign index 1 to all Choppers in list 1
            }

            if (chopper1 != null)
            {
                var chopperFly = chopper1.choppers[0];
                chopperFly.ChopperIndex = 2; // Assign index 2 and 3 to Choppers in list 2
            }
        }

        /// <summary>
        /// Retrieves the parent item associated with this Chopper.
        /// </summary>
        public Item GetParentItem()
        {
            return GetComponent<Item>(); // Return the parent item.
        }


        /// <summary>
        /// Retrieves the GameObject associated with this item.
        /// </summary>
        public GameObject GetGameobject()
        {
            return gameObject; // Return the GameObject.
        }

        /// <summary>
        /// Determines if this item can be combined with other items.
        /// </summary>
        public bool IsCombinable()
        {
            return Combinable; // Return combinable status.
        }

        /// <summary>
        /// Determines if this item can be activated by an explosion.
        /// </summary>
        public bool IsExplodable()
        {
            return ActivateByExplosion; // Return explodable status.
        }

        /// <summary>
        /// Sets whether the item can be activated by an explosion.
        /// </summary>
        public void SetExplodable(bool setExplodable)
        {
            ActivateByExplosion = setExplodable; // Set explodable status.
        }

        /// <summary>
        /// Determines if this item should be static when the game starts.
        /// </summary>
        public bool IsStaticOnStart()
        {
            return StaticOnStart; // Return static status.
        }

        /// <summary>
        /// Sets the order of the item based on sorting layers.
        /// </summary>
        public void SetOrder(int i)
        {
            var spriteRenderers = GetSpriteRenderers();
            var orderedEnumerable = spriteRenderers.OrderBy(x => x.sortingOrder).ToArray();

            // Adjust the sorting order of all sprite renderers.
            for (int index = 0; index < orderedEnumerable.Length; index++)
            {
                var spr = orderedEnumerable[index];
                spr.sortingOrder = i + index;
            }
        }

        /// <summary>
        /// Sets a callback to trigger an action after the Chopper's destruction animation completes.
        /// </summary>
        public void SetCallback(Action callback)
        {
            //Debug.Log("ItemChopper: SetCallback called");
            choppers.ForEachY(i => i.SecondPartDestroyAnimation(callback)); // Set callback for each Chopper.
        }

        // Save the items
        private void SaveItemsForChopper(Item item1, Item item2)
        {
            _savedItem1 = item1;
            _savedItem2 = item2;
        }

        // Reset the saved items
        private void ResetSavedItems()
        {
            _savedItem1 = null;
            _savedItem2 = null;
        }
    }
}
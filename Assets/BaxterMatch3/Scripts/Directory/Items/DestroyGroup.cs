using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Internal.Scripts.Items;
using Internal.Scripts;

// Remove unused using statements if any:
// using Unity.VisualScripting;
// using NUnit.Framework.Constraints;


namespace Internal.Scripts.Items
{
    public class DestroyGroup
    {
        private readonly List<Item> items;
        private readonly Item targetItem;
        private bool isProcessing = false; // Prevent re-entry on the same instance

        // --- Crucial Recommendation ---
        // TODO: Add a state flag to your 'Item' class:
        // public bool IsBeingDestroyed { get; set; } = false;
        // The calling code should check this flag before creating a DestroyGroup.
        // This constructor should set this flag to true for all items.
        // Item.DestroyItem() should ideally handle removing the item and potentially resetting the flag if needed elsewhere.
        // --- End Recommendation ---

        public DestroyGroup(List<Item> itemsToDestroy)
        {
            if (itemsToDestroy == null || itemsToDestroy.Count == 0)
            {
                Debug.LogError("DestroyGroup created with null or empty item list.");
                items = new List<Item>(); // Ensure items list is not null
                targetItem = null;
                return;
            }

            // Filter out any null items just in case
            items = itemsToDestroy.Where(item => item != null).ToList();

            if (items.Count == 0)
            {
                Debug.LogWarning("DestroyGroup created with list containing only null items.");
                targetItem = null;
                return;
            }

            // **Important:** Mark items as being processed immediately
            // This helps prevent the calling code (match detection) from picking them up again.
            foreach (var item in items)
            {
                // **Requires adding this property to your Item class**
                // item.IsBeingDestroyed = true;
                // Alternatively, use another mechanism like adding them to a LevelManager set.
                // LevelManager.THIS.MarkItemForDestruction(item);
            }

            targetItem = FindTargetItem(items);
            LogConstruction();
        }

        // Finds the best target item (prioritizing power-up spawners)
        private Item FindTargetItem(List<Item> itemList)
        {
            Item potentialTarget = null;
            foreach (var item in itemList)
            {
                // Assuming ItemsTypes.NONE is the default/non-powerup type
                if (item.NextType != ItemsTypes.NONE)
                {
                    Debug.Log($"[DestroyGroup] Target item found: {item.gameObject.name} with NextType {item.NextType}");
                    return item; // Found the best target
                }

                if (potentialTarget == null)
                {
                    potentialTarget = item; // Keep track of the first item as a fallback
                }
            }

            // If no item creates a power-up, use the first item as target
            if (potentialTarget != null)
            {
                Debug.Log($"[DestroyGroup] No power-up target found. Defaulting to first item: {potentialTarget.gameObject.name}");
                return potentialTarget;
            }

            // This case should ideally not happen if the constructor checks worked
            Debug.LogError("[DestroyGroup] Could not determine a target item!");
            return null;
        }

        private void LogConstruction()
        {
            if (targetItem != null)
            {
                Debug.Log($"[DestroyGroup] Constructed for {items.Count} items. Target: {targetItem.gameObject.name}");
            }
            else
            {
                Debug.LogError($"[DestroyGroup] Constructed but failed to set a target item for {items.Count} items.");
            }
        }


        /// <summary>
        /// Starts the destruction process for the group.
        /// Handles animation of items moving to the target or direct destruction with particles.
        /// </summary>
        /// <param name="showScore">Should score popups be displayed?</param>
        /// <param name="useParticles">Should particle effects be used (typically for simple 3-matches)?</param>
        /// <param name="useExplosionEffect">Should an explosion effect be triggered (often for power-ups)?</param>
        /// <returns>Coroutine IEnumerator</returns>
        public IEnumerator DestroyGroupCor(bool showScore = false, bool useParticles = true, bool useExplosionEffect = false)
        {
            // useParticles = false;
            if (isProcessing)
            {
                Debug.LogWarning($"[DestroyGroup] Destruction already in progress for target {targetItem?.gameObject.name}. Aborting duplicate call.");
                yield break; // Prevent re-entry on the same instance
            }

            if (!ValidateTargetItem())
            {
                CleanupItemStates(); // Ensure flags are cleared if we abort early
                yield break;
            }

            isProcessing = true;
            LogDestructionStart();
            //AddGameBlockerToTarget(); // Pause falling while animation happens

            // Determine if we should use the "move-to-target" animation
            // Typically, don't move if it's just a basic 3-match (use particles) or if count is very high?
            // Let's adjust the logic: Use move-to-target *unless* useParticles is explicitly true.
            bool shouldMoveToTarget = items.Count > 3; // Only move if more than one item and not using particles directly
            //useParticles=true; // Force particles to true for now, as per original logic
            // Adjust particle setting based on count? (Original logic kept, but consider if this is desired)
            // if (items.Count > 3) useParticles = false; // Maybe intended to disable *default* particles when forming powerup?

            try
            {
                if (items.Count > 3)
                {
                    // --- Animate items moving to the target ---
                    yield return AnimateItemsToTarget(showScore, useParticles, useExplosionEffect);
                }
                else
                {
                    // --- Destroy items directly (with particles if enabled) ---
                    Debug.Log($"[DestroyGroup] Destroying items directly (useParticles: {useParticles}). Target: {targetItem.gameObject.name}");
                    // Destroy all items, letting the target handle potential power-up spawning
                    CentralSoundManager.Instance.PlayOneShot(CentralSoundManager.Instance.match3Effect);

                    foreach (var item in items)
                    {
                        if (item != null && item.gameObject != null) // Check if item still exists
                        {
                            // Pass true for destroyNeighbours only if needed? Check Item.DestroyItem logic.
                            item.DestroyItem(showScore, true, item == targetItem ? targetItem : null, useExplosionEffect, destroyNeighbours: true);
                        }
                    }
                    // Optional short delay after direct destruction?
                    // yield return new WaitForSeconds(0.1f);
                }
            }
            finally // Ensure state is cleaned up even if errors occur
            {
                // Cleanup potentially handled inside Item.DestroyItem() if it resets flags like IsBeingDestroyed
                // If not, uncomment the call below:
                // CleanupItemStates();
                isProcessing = false;
                Debug.Log($"[DestroyGroup] Destruction process finished for target: {targetItem?.gameObject.name}");
            }
        }

        private bool ValidateTargetItem()
        {
            if (targetItem == null || targetItem.gameObject == null) // Also check if GameObject was destroyed prematurely
            {
                Debug.LogError("[DestroyGroup] Target item is null or destroyed. Aborting destruction.");
                // Attempt cleanup just in case items were flagged
                CleanupItemStates();
                return false;
            }

            return true;
        }


        private void LogDestructionStart()
        {
            Debug.Log($"[DestroyGroup] Starting destruction. Target: {targetItem.gameObject.name}, Item Count: {items.Count}");
        }

        // Coroutine for the "move-to-target" animation sequence
        private IEnumerator AnimateItemsToTarget(bool showScore, bool useParticles, bool useExplosionEffect)
        {
            Debug.Log($"[DestroyGroup] Animating items to target: {targetItem?.gameObject.name ?? "NULL"}");

            if (!ValidateTargetItem()) // Re-validate target just before animation
            {
                CleanupItemStates();
                isProcessing = false; // Ensure processing flag is reset if we exit early
                yield break;
            }

            int animationsRemaining = 0;
            List<Item> itemsToMove = items.Where(item => item != targetItem && item != null && item.gameObject != null).ToList(); // Filter out null/destroyed items

            if (itemsToMove.Count == 0)
            {
                Debug.Log($"[DestroyGroup] AnimateItemsToTarget: No valid items to move to target {targetItem.gameObject.name}. Proceeding to final destruction.");
            }
            else
            {
                animationsRemaining = itemsToMove.Count;
                Debug.Log($"[DestroyGroup] Starting move animation for {animationsRemaining} items to target {targetItem.gameObject.name}.");

                foreach (var item in itemsToMove)
                {
                    // Double-check item validity before starting animation
                    if (item == null || item.gameObject == null)
                    {
                        Debug.LogWarning($"[DestroyGroup] Item became null before starting animation. Skipping.");
                        animationsRemaining--;
                        continue;
                    }

                    // Start the movement animation
                    item.MoveToTargetAnim(targetItem, 0.6f, () =>
                    {
                        // Callback when *one* item finishes moving
                        animationsRemaining--;
                        // Debug.Log($"[DestroyGroup] Item {item?.gameObject?.name ?? "NULL"} finished moving. Remaining: {animationsRemaining}");
                        // Optionally hide the item immediately after animation
                        if (item?.colorableComponent?.directSpriteRenderer != null)
                        {
                            item.colorableComponent.directSpriteRenderer.enabled = false;
                        }
                    });
                }

                // Wait until all movement animations have triggered their callback
                yield return new WaitUntil(() => animationsRemaining <= 0);
                Debug.Log($"[DestroyGroup] All item move animations complete for target: {targetItem.gameObject.name}");
            }


            // --- Clear NextType for Non-Target Items ---
            Debug.Log($"[DestroyGroup] Clearing NextType for non-target items before final destruction. Target: {targetItem.gameObject.name} (NextType: {targetItem.NextType})");
            foreach (var item in items)
            {
                // Check if item is valid and is NOT the target item
                if (item != null && item != targetItem)
                {
                    if (item.NextType != ItemsTypes.NONE)
                    {
                        Debug.Log($"[DestroyGroup] Clearing NextType ({item.NextType}) for non-target item: {item.gameObject.name}");
                        item.NextType = ItemsTypes.NONE; // Explicitly clear NextType
                    }
                }
                else if (item == targetItem)
                {
                    Debug.Log($"[DestroyGroup] Keeping NextType ({item.NextType}) for target item: {item.gameObject.name}");
                }
            }

            // --- Final Destruction Step ---
            // After animations and clearing NextType, destroy everything.
            // Only the targetItem should now have a non-NONE NextType if it was meant to spawn a power-up.
            Debug.Log($"[DestroyGroup] Proceeding to final destruction phase for target: {targetItem.gameObject.name}");
            foreach (var item in items)
            {
                if (item != null && item.gameObject != null) // Check if item still exists
                {
                    // Pass the original `useExplosionEffect` and `showScore`.
                    // Set `useParticles` to false as the visual effect was the movement.
                    item.colorableComponent.directSpriteRenderer.enabled = true;
                    item.DestroyItem(showScore, false, item == targetItem ? targetItem : null, useExplosionEffect, items.Count, destroyNeighbours: true);
                }
                else
                {
                    Debug.LogWarning("[DestroyGroup] An item was null or destroyed before the final destruction loop.");
                }
            }
            // Note: isProcessing flag is reset in the finally block of DestroyGroupCor
        }

        // Utility to pause game mechanics like falling items
        private void AddGameBlockerToTarget()
        {
            if (targetItem != null && MainManager.Instance != null)
            {
                Debug.Log($"[DestroyGroup] Adding game blocker (pausing falling) for target: {targetItem.gameObject.name}");
                // Consider making the duration configurable or slightly longer than the animation
                MainManager.Instance.StartCoroutine(MainManager.Instance.PauseFalling(0.6f, MainManager.Instance.self));
            }
        }

        // Call this if you added an IsBeingDestroyed flag to Item
        private void CleanupItemStates()
        {
            Debug.Log("[DestroyGroup] Cleaning up item states.");
            foreach (var item in items)
            {
                if (item != null)
                {
                    // **Requires adding this property to your Item class**
                    // item.IsBeingDestroyed = false;
                    // Or use the LevelManager equivalent
                    // LevelManager.THIS.UnmarkItemForDestruction(item);
                }
            }
        }
    }
}
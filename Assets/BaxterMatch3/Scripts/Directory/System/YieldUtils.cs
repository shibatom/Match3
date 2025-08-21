

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Internal.Scripts;
using Internal.Scripts.Items;
using UnityEngine;

namespace Internal.Scripts.System
{
    public class WaitUntilPipelineIsDestroyed : CustomYieldInstruction
    {
        private bool currentDestroyFinished;

        // This property returns true if the destroy process is still ongoing
        public override bool keepWaiting => !currentDestroyFinished;

        // Constructor that starts the destroy process and waits for it to finish
        public WaitUntilPipelineIsDestroyed(List<Item> destroyItems, Delays delays)
        {
            Debug.Log("[CustomYieldInstructionLog] WaitWhileDestroyPipeline started.");

            PipelineDestroyer.Instance.DestroyItems(destroyItems, delays, () =>
            {
                currentDestroyFinished = true;
                Debug.Log("[CustomYieldInstructionLog] WaitWhileDestroyPipeline destroy completed.");
            });
        }
    }

    public class WaitingFallingDuration : CustomYieldInstruction
    {
        private List<Item> items;

        // This property checks if any item is still falling
        public override bool keepWaiting
        {
            get
            {
                var ii = items.WhereNotNull().Where(i => i.gameObject.activeSelf).Any(i => i.falling);
                if (!ii)
                {
                    Debug.Log("[CustomYieldInstructionLog] Fall finished.");
                }
                return ii;
            }
        }

        // Constructor that optionally generates new items and checks for fall
        public WaitingFallingDuration(bool generateNewItems = true)
        {
            Debug.Log("[CustomYieldInstructionLog] WaitWhileFall started.");
            GenerateAndFall(generateNewItems);
        }

        // Generates and orders items for fall check
        private void GenerateAndFall(bool generateNewItems)
        {
            Debug.Log("[CustomYieldInstructionLog] Generating and ordering items for fall.");

            items = MainManager.Instance.field.GetItems(false, null, false);
        }
    }

    public class WaitCollectingDuration : CustomYieldInstruction
    {
        private AnimateItems[] items;

        // This property checks if any animated items still exist
        public override bool keepWaiting
        {
            get
            {
                var ii = items.WhereNotNull().Any();
                if (!ii)
                {
                    Debug.Log("[CustomYieldInstructionLog] Collection finished.");
                }
                return ii;
            }
        }

        // Constructor that gets the current animated items
        public WaitCollectingDuration()
        {
            Debug.Log("[CustomYieldInstructionLog] WaitWhileCollect started.");
            items = MainManager.Instance.animateItems.Where(i => i.target).ToArray();
        }
    }

    public class WaitingWhileFallSide : CustomYieldInstruction
    {
        private List<Item> items;

        // This property checks if any items are still falling and triggers side fall if needed
        public override bool keepWaiting
        {
            get
            {
                var ii = items.WhereNotNull().Where(i => i.gameObject.activeSelf).Any(i => i.falling);
                var squares = MainManager.Instance.field.squaresArray.Where(i => i.isEnterPoint && i.IsFree() && i.Item == null).ToArray();

                if (ii && squares.Any())
                {
                    Debug.Log("[CustomYieldInstructionLog] Falling side triggered.");
                    squares.ForEachY(i => i.GenItem());
                    FallSide();
                }
                return ii;
            }
        }

        // Constructor that starts the fall side process
        public WaitingWhileFallSide()
        {
            Debug.Log("[CustomYieldInstructionLog] WaitWhileFallSide started.");
            FallSide();
        }

        // Orders and checks items for side fall
        private void FallSide()
        {
            Debug.Log("[CustomYieldInstructionLog] Ordering and checking items for side fall.");

            items = MainManager.Instance.field.GetItems(false, null, false).Where(i => i.square).OrderBy(i => i.square.orderInSequence).ToList();
            items.Where(i => !i.destroying && !i.falling && !i.JustCreatedItem).ToList().ForEach(i => i.CheckNearEmptySquares());
        }
    }

    public class WaitWhileListIsNull : CustomYieldInstruction
    {
        private List<object> items;

        // This property returns true if all items in the list are null
        public override bool keepWaiting => items.AllNull();

        // Constructor that takes a list of objects
        public WaitWhileListIsNull(List<object> items_)
        {
            Debug.Log("[CustomYieldInstructionLog] WaitForListNull started.");
            items = items_;
        }
    }

    public class WaitDestroyingDuration : CustomYieldInstruction
    {
        private float startTime;
        private List<Item> items;

        // This property checks if any items are still being destroyed
        public override bool keepWaiting
        {
            get
            {
                if (startTime + 1 < Time.time)
                {
                    items.Where(i => i && i.destroying && i.gameObject.activeSelf).ForEachY(i =>
                    {
                        i.destroying = false;
                        i.DestroyItem(CheckJustInItem: false);
                    });
                }
                items = items.Where(i => i && i.gameObject.activeSelf).ToList();
                return items.Any(i => i.destroying);
            }
        }

        // Constructor that initializes the destruction check
        public WaitDestroyingDuration()
        {
            Debug.Log("[CustomYieldInstructionLog] WaitWhileDestroying started.");

            startTime = Time.time;
            items = MainManager.Instance.field.GetItems(false, null, false)
                      .Where(i => i.currentType != ItemsTypes.DiscoBall) // exclude items that type is multicolor
                      .ToList();
        }
    }

    public class WaitingFortheNextMove : CustomYieldInstruction
    {
        bool nextMove;

        // This property checks if the next move has been made
        public override bool keepWaiting
        {
            get
            {
                if (nextMove)
                {
                    Debug.Log("[CustomYieldInstructionLog] Next move detected.");
                    MainManager.OnTurnEnd -= OnTurnEnd;
                    return false;
                }

                return true;
            }
        }

        // Event handler for the end of a turn
        void OnTurnEnd()
        {
            Debug.Log("[CustomYieldInstructionLog] Turn ended, waiting for next move.");
            nextMove = true;
        }

        // Constructor that subscribes to the turn end event
        public WaitingFortheNextMove()
        {
            Debug.Log("[CustomYieldInstructionLog] WaitForNextMove started.");
            MainManager.OnTurnEnd += OnTurnEnd;
        }
    }

    public class WaitUntilTheSubLevelIsChanged : CustomYieldInstruction
    {
        bool nextMove;

        // This property checks if the sublevel has changed
        public override bool keepWaiting
        {
            get
            {
                if (nextMove)
                {
                    Debug.Log("[CustomYieldInstructionLog] Sublevel change detected.");
                    MainManager.OnSublevelChanged -= OnSublevelChanged;
                    return false;
                }

                return true;
            }
        }

        // Event handler for sublevel changes
        void OnSublevelChanged()
        {
            Debug.Log("[CustomYieldInstructionLog] Sublevel changed.");
            nextMove = true;
        }

        // Constructor that subscribes to the sublevel change event
        public WaitUntilTheSubLevelIsChanged()
        {
            Debug.Log("[CustomYieldInstructionLog] WaitForSubLevelChange started.");
            MainManager.OnSublevelChanged += OnSublevelChanged;
        }
    }

    public class WaitTillArrayIsNotNull : CustomYieldInstruction
    {
        object[] items;

        // This property returns true if any items in the array are not null
        public override bool keepWaiting => !items.AllNull();

        // Constructor that takes an array of objects
        public WaitTillArrayIsNotNull(object[] array)
        {
            Debug.Log("[CustomYieldInstructionLog] WaitWhileArrayNotNull started.");
            items = array;
        }
    }

    public class WaitAnimationDuration : CustomYieldInstruction
    {
        private bool animationFinished;

        // This property checks if all animations are finished
        public override bool keepWaiting => !animationFinished;

        // Constructor that starts the destruction of animated items
        public WaitAnimationDuration()
        {
            Debug.Log("[CustomYieldInstructionLog] WaitWhileAnimations started.");
            StartCoroutine(DestroyAnimatedItems());
        }

        // Coroutine to destroy animated items
        IEnumerator DestroyAnimatedItems()
        {
            Debug.Log("[CustomYieldInstructionLog] DestroyAnimatedItems coroutine started.");

            var destroyingItems = MainManager.Instance.field.GetDestroyingItems();
            var i = 0;
            foreach (var item in destroyingItems)
            {
                animationFinished = false;
                item.SecondPartDestroyAnimation(() =>
                {
                    StartCoroutine(SetupFalling());
                    i++;
                    if (i == destroyingItems.Count())
                        animationFinished = true;
                });

                yield return new WaitForEndOfFrame();
            }
            if (i == destroyingItems.Count())
                animationFinished = true;
        }

        // Coroutine to set up falling after destruction
        IEnumerator SetupFalling()
        {
            Debug.Log("[CustomYieldInstructionLog] SetupFalling coroutine started.");

            yield return new WaitingFallingDuration();
            animationFinished = true;
        }

        // Helper function to start a coroutine
        void StartCoroutine(IEnumerator ienumerator)
        {
            MainManager.Instance.StartCoroutine(ienumerator);
        }
    }

    public class CustomWaitForSecond : CustomYieldInstruction
    {
        private bool stopWait;
        public float s;

        // This property returns true if the custom wait time is not finished
        public override bool keepWaiting => !stopWait;

        // Constructor that starts a custom wait coroutine
        public CustomWaitForSecond()
        {
            Debug.Log("[CustomYieldInstructionLog] WaitForSecCustom started.");
            StartCoroutine(WaitForSecCustomCor(s));
        }

        // Coroutine for custom wait
        IEnumerator WaitForSecCustomCor(float sec)
        {
            Debug.Log($"[CustomYieldInstructionLog] Waiting for {sec} seconds.");
            yield return new WaitForSeconds(sec);
        }

        // Helper function to start a coroutine
        void StartCoroutine(IEnumerator ienumerator)
        {
            MainManager.Instance.StartCoroutine(ienumerator);
            stopWait = true;
        }
    }
}
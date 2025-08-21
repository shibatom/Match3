using System.Linq;
using Internal.Scripts;
using Internal.Scripts.Blocks;
using Internal.Scripts.Effects;
using Internal.Scripts.Items;
using Internal.Scripts.Level;
using Internal.Scripts.System;
using Internal.Scripts.System.Pool;
using Internal.Scripts.TargetScripts.TargetEditor;
using UnityEngine;

namespace Internal.Scripts.TargetScripts.TargetSystem
{
    /// <summary>
    /// Handles target counting logic and manages target animations.
    /// </summary>
    public class TargetCounter
    {
        #region Fields and Properties

        public GameObject targetPrefab;
        public int count; // Current target count
        private int savedCount; // Original target count (used for stars target)
        public int previousCount; // Previous count value
        public Sprite extraObject; // Primary extra sprite to use
        public Sprite[] extraObjects; // Collection of extra sprites
        public int color; // Color identifier (usage depends on context)
        public TargetGUI TargetGui; // Associated GUI element for targets
        public TargetContainer targetLevel; // The level container that holds target settings
        public CollectingTypes collectingAction; // How the target is collected (Destroy, Clear, Spread, etc.)
        public bool NotFinishUntilMoveOut; // Flag to delay finish until objects move out

        private bool showScores; // Flag to show score (for Stars target)
        private int totalSquaresCount; // Cached count of all squares in the level

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="TargetCounter"/> class.
        /// </summary>
        /// <param name="targetPrefab">Prefab to be used for target animations.</param>
        /// <param name="initialCount">Initial target count.</param>
        /// <param name="extraObjectArray">Extra objects (sprites) related to the target.</param>
        /// <param name="color">Color identifier.</param>
        /// <param name="targetLevel">Target container with level settings.</param>
        /// <param name="_NotFinishUntilMoveOut">Flag indicating whether the target finish waits until movement completes.</param>
        /// <param name="targetObject">TargetObject holding additional configuration (like score settings).</param>
        public TargetCounter(GameObject targetPrefab, int initialCount, Sprite[] extraObjectArray, int color, TargetContainer targetLevel, bool _NotFinishUntilMoveOut, TargetObject targetObject)
        {
            //  Debug.LogError("TargetCounter: Constructor called with initialCount " + initialCount);
            this.targetPrefab = targetPrefab;
            count = initialCount;
            this.targetLevel = targetLevel;
            this.NotFinishUntilMoveOut = _NotFinishUntilMoveOut;
            this.color = color;
            collectingAction = targetLevel.collectAction;

            // If the target is Stars and score display is enabled, adjust count accordingly.
            if (targetLevel.name == "Stars" && targetObject.ShowTheScoreForStar.ShowTheScore)
            {
                showScores = targetObject.ShowTheScoreForStar.ShowTheScore;
                // Assign count based on star thresholds defined in LevelData.
                switch (targetObject.CountDrawer.count)
                {
                    case 1:
                        count = LevelData.THIS.star1;
                        break;
                    case 2:
                        count = LevelData.THIS.star2;
                        break;
                    case 3:
                        count = LevelData.THIS.star3;
                        break;
                }
            }

            // Setup extra sprites.
            extraObjects = extraObjectArray;

            if (extraObjects != null && extraObjects.Length > 0)
            {
                // Try to find the UI sprite from the target object.
                var uiSprite = targetObject.sprites.FirstOrDefault(i => i.uiSprite == true);
                extraObject = (uiSprite != null) ? uiSprite.icon : extraObjects[0];
            }

            // Recalculate count based on squares on board.
            count = GetCountForSquares();
            savedCount = count;
            previousCount = count;
        }

        #endregion

        #region Private Methods

        private int GetCountForSquares()
        {
            if (targetLevel != null && targetLevel.setCount == TargetSystem.SetCount.FromLevel)
            {
                if (targetLevel.collectAction == CollectingTypes.Destroy || targetLevel.collectAction == CollectingTypes.Clear)
                {
                    count = GetSquareTargetCount();
                    return GetSquareTargetCount();
                }
            }

            return count;
        }

        /// <summary>
        /// Calculates remaining target count for the Spread collecting action.
        /// </summary>
        /// <param name="subLevel">If true, consider sublevel board squares.</param>
        /// <returns>Calculated count for Spread action.</returns>
        private int GetSquareSpreadCount(bool subLevel)
        {
            // Debug.LogError("TargetCounter: GetSquareSpreadCount called with subLevel " + subLevel);
            // Calculate the total number of available squares.
            totalSquaresCount = MainManager.Instance.fieldBoards.Sum(board => board.GetSquares().Where(square => square.IsAvailable()).Count());
            int targetType2Count;
            int availableCount;

            if (!subLevel)
            {
                targetType2Count = MainManager.Instance.fieldBoards.Sum(board => board.GetSquares()
                    .Sum(square => square.GetSpriteRenderers().Distinct().Sum(renderer => extraObjects.Count(extra => extra == renderer.sprite))));
                availableCount = MainManager.Instance.fieldBoards.Sum(board => board.GetSquares().Where(square => square.IsAvailable()).Count());
            }
            else
            {
                targetType2Count = MainManager.Instance.field.GetSquares()
                    .Sum(square => square.GetSpriteRenderers().Sum(renderer => extraObjects.Count(extra => extra == renderer.sprite)));
                availableCount = MainManager.Instance.field.GetSquares().Where(square => square.IsAvailable()).Count();
            }

            return availableCount - targetType2Count;
        }


        private int GetSquareTargetCount()
        {
            int matchCount = 0;

            var targetComponents = Object.FindObjectsByType<TargetComponent>(FindObjectsSortMode.None);

            foreach (var targetComponent in targetComponents)
            {
                var squareItem = targetComponent.GetComponent<ISquareItemCommon>();
                var spriteRenderers = squareItem.GetSpriteRenderers();

                foreach (var renderer in spriteRenderers)
                    if (renderer != null && renderer.sprite != null && extraObjects.Contains(renderer.sprite))

                        matchCount++;
            }

            return matchCount;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Checks and processes the target on the given game object.
        /// </summary>
        /// <param name="obj">The game object to check.</param>
        /// <param name="spr">The sprite associated with the object (currently unused).</param>
        public void OnCheckTarget(GameObject obj, Sprite spr)
        {
            //Debug.LogError("TargetCounter: OnCheckTarget called for object " + obj.name);
            // Get the square item component from the object.
            var item = obj.GetComponent<ISquareItemCommon>();
            //Debug.LogError("TargetCounter: OnCheckTarget called for object " + obj.name + "obj type " + obj?.GetComponent<SubSquare>() + "type" +  item.GetType());
//            //Debug.LogError($"TargetCounter: obj is {item}");
            var spriteRenderers = item.GetSpriteRenderers();
            var type = item.GetType();
            // //Debug.LogError($"TargetCounter: obj type is {type}");

            // For ReachBottom actions, only process if the item is at the bottom.
            if (collectingAction == CollectingTypes.ReachBottom && !item.IsBottom())
                return;

            // Filter sprite renderers that match one of the extra sprites.
            var matchingRenderers = spriteRenderers.Where(renderer => extraObjects.Any(extra => extra == renderer.sprite)).ToArray();
            if (matchingRenderers.Any() && count > 0)
            {
                // Debug.Log($"TargetCounter: matching objects count is {matchingRenderers.Count()}");
                // Choose the renderer with the highest sorting order for animation linking.
                var animLinkObject = matchingRenderers.OrderByDescending(renderer => renderer.sortingOrder).First().gameObject;
                var square = animLinkObject.GetComponent<Rectangle>();

                // // If the square is already scheduled for animation, exit.
                // if (LevelManager.THIS.animateItems.Any(anim => square != null && anim.linkObjectHash == square.hashCode))
                //     return;

                // Determine the animation object based on the square type.
                GameObject animItem;
                if (type is LevelTargetTypes.PlateCabinet)
                {
                    animItem = ObjectPoolManager.Instance.GetPooledObject("BrockenCupPrticle");
                    animItem.GetComponent<cupAnimation>().brockenParticle.GetComponent<SplashEffectParticles>().RandomizeParticleSeed();
                }
                else if (type is LevelTargetTypes.PotionCabinet)
                {
                    animItem = ObjectPoolManager.Instance.GetPooledObject("PaintParticle");
                    if (animItem != null)
                    {
                        //Debug.LogError($"TargetCounter: Attempting to set color - obj: {obj}, SubSquare: {obj?.GetComponent<SubSquare>()}, SplashParticles: {animItem?.GetComponent<SplashParticles>()}");
                        animItem.GetComponent<SplashEffectParticles>().mySetColor(obj.GetComponent<SubRectangle>().BottleColor);
                    }
                }
                else
                {
                    // Fallback: create a new GameObject if no pooled object is available.
                    animItem = new GameObject();
                }

                if (type is LevelTargetTypes.Mails)
                {
                    //Debug.Log("TargetCounter: Email");
                }

                // Add the animation component and register it.
                var animComp = animItem.AddComponent<AnimateItems>();
                MainManager.Instance.animateItems.Add(animComp);
                if (square != null)
                    animComp.linkObjectHash = square.hashCode;
                animComp.target = true;

                // Initialize the animation with a callback that decrements the target count.
                Vector3 targetPosition = TargetGUI.GetTargetGUIPosition(extraObject.name);
                animComp.InitAnimation(animLinkObject, targetPosition, obj.transform.localScale, () => { changeCount(-1); }, levelTargetType: type);
            }

            // Special handling for Stars target: adjust count based on score.
            if (targetLevel.name == "Stars")
            {
                if (!showScores)
                    count = savedCount - MainManager.Instance.stars;
                else
                    count = savedCount - MainManager.Score;

                if (count < 0)
                    count = 0;
            }
        }

        /// <summary>
        /// Updates the target count by the given delta.
        /// </summary>
        /// <param name="delta">The amount to change the count (usually negative).</param>
        public void changeCount(int delta)
        {
            // Debug.LogError("TargetCounter: changeCount called with delta " + delta);
            count += delta;
            //Debug.LogError($"changeCount:count:{count},previousCount:{previousCount} , !LevelManager.THIS.DragBlocked:{!LevelManager.THIS.DragBlocked}");
            if (count < 0) count = 0;
            previousCount = count;

            // // If no dragging is occurring and the count is zero, check win/lose condition.
            // if (!LevelManager.THIS.DragBlocked && count == 0){
            //     //Debug.LogError("TargetCounter: changeCount calling CheckWinLose");
            //     LevelManager.THIS.CheckWinLose();
            // }
        }

        /// <summary>
        /// Gets the current target count.
        /// </summary>
        /// <param name="game">If true, return the in-game count; otherwise, recalculate from the level.</param>
        /// <returns>The target count.</returns>
        public int GetCount(bool game = false)
        {
            //Debug.LogError("TargetCounter: GetCount called with game " + game);
            if (!game)
            {
                int countForSquares = GetCountForSquares();
                return countForSquares < 0 ? 0 : countForSquares;
            }
            else
            {
                // For in-game count, simply return the current count.
                return count;
            }
        }

        /// <summary>
        /// Determines if the sublevel target has been reached.
        /// </summary>
        /// <returns>True if reached, false otherwise.</returns>
        public virtual bool IsTargetReachedSubLevel()
        {
            if (targetLevel.collectAction == CollectingTypes.ReachBottom && MainManager.Instance.field.IngredientsByEditor)
            {
                if (MainManager.Instance.field.GetItems().Count(i => i.currentType == ItemsTypes.Gredient) == 0)
                    return true;
            }

            return GetCountForSquares() <= 0;
        }

        /// <summary>
        /// Determines if the total target for the level has been reached.
        /// </summary>
        /// <returns>True if reached, false otherwise.</returns>
        public virtual bool IsTotalTargetReached()
        {
            //Debug.LogError("TargetCounter: IsTotalTargetReached called");
            return GetCountForSquares() <= 0;
        }

        /// <summary>
        /// Binds the target GUI to this counter and applies animations for star targets.
        /// </summary>
        /// <param name="targetGui">The target GUI instance to bind.</param>
        public void BindGUI(TargetGUI targetGui)
        {
            // Debug.LogError("TargetCounter: BindGUI called");
            TargetGui = targetGui;
            if (targetLevel.name == "Stars" && !showScores)
            {
                // Retrieve the star object from the progress bar.
                var stars = GameObject.Find("ProgressBar").transform.Find("Stars");
                var star = stars.GetChild(savedCount - 1).gameObject;
                // Animate the star with a ping-pong scale effect.
                LeanTween.delayedCall(star, 5f, () =>
                {
                    LeanTween.scale(star, Vector3.one * 1.2f, 0.4f)
                        .setLoopPingPong()
                        .setRepeat(4);
                }).setRepeat(-1);
            }
        }

        /// <summary>
        /// Gets the name of the target (usually from the target level container).
        /// </summary>
        /// <returns>The target name.</returns>
        public string GetTargetName()
        {
            //Debug.LogError("TargetCounter: GetTargetName called");
            return targetLevel.name;
        }

        /// <summary>
        /// Checks whether the current target is a Stars target.
        /// </summary>
        /// <returns>True if it is a Stars target; otherwise, false.</returns>
        public bool IsTargetStars()
        {
            //Debug.LogError("TargetCounter: IsTargetStars called");
            return GetTargetName() == "Stars";
        }

        /// <summary>
        /// Processes an array of items by checking each for target conditions.
        /// </summary>
        /// <param name="items">Array of items to check.</param>
        public void CheckTarget(Item[] items)
        {
            //Debug.LogError("TargetCounter: CheckTarget called with " + items.Length + " items");
            foreach (var item in items)
            {
                if (item != null)
                    OnCheckTarget(item.gameObject, item.GetSprite());
            }
        }

        /// <summary>
        /// Processes an array of squares based on the current collecting action.
        /// </summary>
        /// <param name="squares">Array of squares to check.</param>
        /// <param name="afterDestroy">Flag indicating if this is called after destruction of squares.</param>
        public void CheckTarget(Rectangle[] squares, bool afterDestroy = true)
        {
            //  Debug.LogError("TargetCounter: CheckTarget called with " + squares.Length + " squares, afterDestroy: " + afterDestroy);
            //Debug.Log($"CheckTarget: squares length {squares.Length}, afterDestroy {afterDestroy}");

            // For Spread action, transform empty squares to TargetType2 if extra sprites are present.
            if (targetLevel.collectAction == CollectingTypes.Spread)
            {
                //Debug.Log("CheckTarget: Processing Spread action");
                if (squares.SelectMany(sq => sq.GetSpriteRenderers().Select(renderer => renderer.sprite))
                    .Any(sprite => extraObjects.Any(extra => extra == sprite)))
                {
                    foreach (var square in squares)
                    {
                        if (square != null && square.type == LevelTargetTypes.EmptySquare)
                        {
                            // Debug.Log($"CheckTarget: Setting TargetType2 type for square {square}");
                            square.SetType(LevelTargetTypes.ExtraTargetType2, 1, LevelTargetTypes.NONE, 1);
                        }
                    }
                }
            }
            // For Clear action that is processed before destruction.
            else if (targetLevel.collectAction == CollectingTypes.Clear && !afterDestroy)
            {
                // Debug.Log("CheckTarget: Processing Clear action");
                var targetComponents = Object.FindObjectsOfType<TargetComponent>()
                    .Where(component =>
                    {
                        var spriteRenderer = component.GetComponent<SpriteRenderer>();
                        return spriteRenderer != null && extraObjects.Any(extra => extra == spriteRenderer.sprite);
                    });

                foreach (var component in targetComponents)
                {
                    if (component.GetComponent<Rectangle>().IsCleared())
                    {
                        // Debug.Log($"CheckTarget: Processing cleared square {component}");
                        OnCheckTarget(component.gameObject, component.GetComponent<Rectangle>().GetSprite());
                    }
                }
            }
            // For all other collecting actions.
            else if (targetLevel.collectAction != CollectingTypes.Clear)
            {
                // Debug.Log($"CheckTarget: Processing non-Clear action {targetLevel.collectAction}");
                foreach (var square in squares)
                {
                    // Debug.Log($"CheckTarget: onCheckTarget for square {square}");
                    OnCheckTarget(square.gameObject, square.GetSprite());
                }
            }
        }

        /// <summary>
        /// Checks the bottom row of the field for matching objects and processes them.
        /// </summary>
        public void CheckBottom()
        {
            //Debug.LogError("TargetCounter: CheckBottom called");
            // Get the bottom row squares from the field.
            var bottomSquares = MainManager.Instance.field.GetBottomRow();
            foreach (var squareHolder in bottomSquares)
            {
                // Skip if there is no item or if the item is falling.
                if (squareHolder.Item == null || squareHolder.Item.falling)
                    continue;

                var item = squareHolder.Item;
                string spriteName = item.GetSprite().name;
                if (extraObjects.Any(extra => extra.name == spriteName))
                {
                    OnCheckTarget(item.gameObject, item.GetSprite());
                    item.DestroyBehaviour();
                }
            }
        }

        #endregion
    }
}
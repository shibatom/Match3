using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Spine.Unity;
using Internal.Scripts.System;
using Internal.Scripts.Blocks;
using Internal.Scripts.System.Pool;
using Internal.Scripts.TargetScripts.TargetSystem;
using UnityEngine;
using Object = System.Object;
using Random = UnityEngine.Random;

namespace Internal.Scripts.Items
{
    /// <summary>
    /// Chopper fly animation
    /// </summary>
    public class ChopperFly : MonoBehaviour
    {
        public GameObject explosionPrefab;
        public GameObject particles;
        private Action callback;
        private bool reachedTarget;
        private int priority = 1;
        private bool canBeStarted;
        public ItemsTypes nextItemType;
        public bool setTargetType2;
        public Vector2Int[] targets;
        public float animationTime = 3f;
        private Vector3 pos, scale;
        private Quaternion rot;
        public int originSortingOrder = 2;
        public Vector3 startDirection;
        private ChopperItem _thisChopperItem;
        public IChopperTargetable TargetItem;
        public ItemsTypes interactionType; // Stores the item type for deferred animation setting

        public Vector3 InteractionDirection;

        public Animator HeliAnimator;
        private Tween myTween;

        private Vector3 worldScale;
        private MeshRenderer skeletonRenderer;

        public int ChopperIndex { get; set; }


        public SkeletonMecanim skeletonAnimation;

        public void StartFly(Vector3 SwitchDirection, bool isSwitchItemOverChopper, Item _heliItem, Item _combItem)
        {
            MainManager.Instance.StartBusyOperation();

            skeletonAnimation.gameObject.SetActive(true);
            SetAnimation(SwitchDirection, isSwitchItemOverChopper);
            SetupFlightEffects();
            SetTargetItem();
        }

        private void SetAnimation(Vector3 SwitchDirection, bool isSwitchItemOverChopper)
        {
            if (interactionType != ItemsTypes.NONE)
            {
                // if ((int)combItem.switchDirection.x == 0 && (int)heliItem.switchDirection.x == 0)
                if (SwitchDirection.x == 0)
                {
                    HeliAnimator.SetInteger("SwitchDirection.x", 1);
                    //HeliAnimator.SetInteger("SwitchDirection.y", (int)combItem.switchDirection.y);
                    HeliAnimator.SetBool("IsSwitchItemOverChopper", false);
                }
                else
                {
                    if (isSwitchItemOverChopper == true)
                    {
                        HeliAnimator.SetInteger("SwitchDirection.x", (int)SwitchDirection.x);
                        HeliAnimator.SetInteger("SwitchDirection.y", (int)SwitchDirection.y);
                        HeliAnimator.SetBool("IsSwitchItemOverChopper", isSwitchItemOverChopper);
                    }
                    else
                    {
                        HeliAnimator.SetInteger("SwitchDirection.x", (int)SwitchDirection.x);
                        HeliAnimator.SetInteger("SwitchDirection.y", (int)SwitchDirection.y);
                        HeliAnimator.SetBool("IsSwitchItemOverChopper", isSwitchItemOverChopper);
                    }
                }
            }

            switch (interactionType)
            {
                case ItemsTypes.RocketHorizontal:
                    HeliAnimator.SetTrigger("MergeHorRocket");
                    break;

                case ItemsTypes.RocketVertical:
                    HeliAnimator.SetTrigger("MergeVerRocket");
                    break;

                case ItemsTypes.Bomb:
                    HeliAnimator.SetTrigger("MergePackage");
                    break;

                case ItemsTypes.NONE:
                    HeliAnimator.SetTrigger("StartFly");
                    break;
            }
        }

        public void SetupFlightEffects()
        {
            if (skeletonRenderer == null)
            {
                skeletonRenderer = skeletonAnimation.GetComponent<MeshRenderer>();
            }

            if (skeletonRenderer != null)
            {
                // Set the layer in the SkeletonAnimation's Skeleton
                skeletonRenderer.sortingLayerName = "Spine";
                skeletonRenderer.sortingOrder = 50;
            }
            else
            {
                //Debug.LogWarning("SkeletonAnimation component not found on object.");
            }
            //  particles.SetActive(true);

            //  var spriteRenderer = GetComponent<SpriteRenderer>();
            //  spriteRenderer.sortingLayerName = "ItemMask";
            //  spriteRenderer.sortingOrder = 10;
            trnsShadow.gameObject.SetActive(true);
            canBeStarted = true;
        }

        private void SetTargetItem()
        {
            if (targets == null) return;

            foreach (var target in targets)
            {
                var item = MainManager.Instance.field.GetSquare(target.x, target.y).Item;
                if (item.ChopperTarget == null && TargetItem == null)
                {
                    TargetItem = item;
                    TargetItem.GetChopperTarget = gameObject;
                }
            }
        }


        private void Awake()
        {
            _thisChopperItem = GetComponentInParent<ChopperItem>();
            pos = transform.localPosition;
            rot = transform.localRotation;
            scale = transform.localScale;
        }

        private void OnEnable()
        {
            this.gameObject.SetActive(true);
            nextItemType = ItemsTypes.NONE;
            particles.SetActive(false);
            TargetItem = null;
            setTargetType2 = false;
            transform.localPosition = pos;
            transform.localRotation = rot;
            transform.localScale = Vector3.one;
        }

        private void OnDisable()
        {
            //ReattachToParent(originalParentCache);

            reachedTarget = false;
            LeanTween.cancel(gameObject);
            if (TargetItem != null) TargetItem.GetChopperTarget = null;
            TargetItem = null;
            trnsShadow.gameObject.SetActive(false);
            StopAllCoroutines();
            GetComponent<SpriteRenderer>().sortingLayerName = "Default";
            GetComponent<SpriteRenderer>().sortingOrder = originSortingOrder;
            transform.localPosition = pos;
            transform.localRotation = rot;
            transform.localScale = Vector3.one;
            ChopperIndex = 1;
        }

        private void ReachItem()
        {
            if (TargetItem != null && TargetItem.GetGameObject.activeSelf)
            {
                if (TargetItem.GetType().BaseType == typeof(Item))
                {
                    if (setTargetType2)
                        TargetItem.GetItem.square.SetType(LevelTargetTypes.ExtraTargetType2, 1, LevelTargetTypes.NONE, 1);
                    reachedTarget = true;
                    if (nextItemType == ItemsTypes.NONE ||
                        (TargetItem.GetItem.currentType != ItemsTypes.NONE && (TargetItem.GetItem.currentType != ItemsTypes.DiscoBall ||
                                                                               MainManager.Instance.AdditionalSettings.MulticolorDestroyByBoostAndChopper)) &&
                        TargetItem.GetItem.currentType != ItemsTypes.TimeBomb)
                    {
                        callback?.Invoke();
                        TargetItem.GetItem.DestroyItem(true, true, _thisChopperItem);
                    }
                    else
                    {
                        TargetItem.GetItem.NextType = nextItemType;
                        TargetItem.GetItem.ChangeType(item =>
                        {
                            callback?.Invoke();
                            if (item != null) item.DestroyItem();
                        }, false);
                        TargetItem.GetItem.DestroyItem();
                    }

                    // targetItem.square.DestroyBlock();
                }
                else
                {
                    var square = TargetItem.GetGameObject.GetComponent<Rectangle>();
                    square.DestroyBlock();
                    if (setTargetType2)
                        square.SetType(LevelTargetTypes.ExtraTargetType2, 1, LevelTargetTypes.NONE, 1);
                    reachedTarget = true;
                    callback?.Invoke();
                }
            }

            DestroyChopper();
        }

        private void myReachItem(Vector3 target)
        {
            if (!IsTargetValid())
                return;

            if (IsTargetTypeItem())
                HandleItemTarget(target);
            else
                HandleSquareTarget();

            DestroyChopper();
        }

        private bool IsTargetValid()
        {
            if (TargetItem == null || !TargetItem.GetGameObject.activeSelf)
            {
                return false;
            }

            return true;
        }

        private bool IsTargetTypeItem()
        {
            return TargetItem.GetType().BaseType == typeof(Item);
        }

        private void HandleItemTarget(Vector3 target)
        {
            //Debug.Log($"ItemChopper: Processing target at position: {target}, NextItemType: {nextItemType}");

            ApplyTargetType2IfNeeded();
            reachedTarget = true;

            // Find the square at the target position using raycast
            Rectangle targetRectangle = FindSquareAtPosition(target);
            if (targetRectangle == null)
            {
                //Debug.LogWarning($"No square found at position: {target}");
                return;
            }

            // Step 1: Destroy any item at the square
            DestroyTargetItem(targetRectangle);

            // Step 2: Trigger Chopper’s own effect if nextItemType isn’t NONE
            if (nextItemType != ItemsTypes.NONE)
            {
                //Debug.Log($"Triggering Chopper effect for NextItemType: {nextItemType}");
                TriggerChopperEffect(targetRectangle);
            }
        }

        private Rectangle FindSquareAtPosition(Vector3 position)
        {
            var layerMask = LayerMask.GetMask("Square");
            Vector2 checkPosition = new Vector2(position.x, position.y);
            Collider2D hit = Physics2D.OverlapPoint(checkPosition, layerMask);

            if (hit == null)
            {
                //Debug.Log("No square found at this position.");
                return null;
            }

            var square = hit.GetComponent<Rectangle>();
            if (square == null)
            {
                //Debug.Log("Collider found, but it does not have a Square component.");
                return null;
            }

            //Debug.Log($"Found square: {square.gameObject.name} at position: {position}");
            return square;
        }

        private void DestroyTargetItem(Rectangle targetRectangle)
        {
            // Check for an item on the square
            if (targetRectangle.Item != null)
            {
                var targetItem = targetRectangle.Item;
                //Debug.Log($"Destroying target item: {targetItem.gameObject.name} on square: {targetSquare.gameObject.name}");
                callback?.Invoke();
                targetItem.DestroyItem(true, true, _thisChopperItem);
            }
            else
            {
                //Debug.Log($"No item found on square: {targetSquare.gameObject.name}, destroying block instead");
                targetRectangle.DestroyBlock();
            }
        }

        private void TriggerChopperEffect(Rectangle targetRectangle)
        {
            switch (nextItemType)
            {
                case ItemsTypes.RocketHorizontal:
                    //Debug.Log("Triggering horizontal striped effect");
                    MainManager.Instance.StripedShow(targetRectangle.gameObject, targetRectangle, true);
                    break;

                case ItemsTypes.RocketVertical:
                    //Debug.Log("Triggering vertical striped effect");
                    MainManager.Instance.StripedShow(targetRectangle.gameObject, targetRectangle, false);
                    break;

                case ItemsTypes.Bomb:
                    //Debug.Log("Triggering package effect");
                    MainManager.Instance.ShowDestroyPackage(targetRectangle, false);
                    break;

                default:
                    //Debug.Log($"No specific effect defined for {nextItemType}");
                    break;
            }
        }

        private void HandleSquareTarget()
        {
            //Debug.Log("DestroyTargetItem: Attempting to detect item using OverlapPoint.");

            // Define the layer mask for the objects you want to detect
            var layerMask = LayerMask.GetMask("Square");

            // Convert the transform position to Vector2 since OverlapPoint works in 2D
            Vector2 checkPosition = new Vector2(transform.position.x, transform.position.y);

            // Check for a collider at this position
            Collider2D hit = Physics2D.OverlapPoint(checkPosition, layerMask);

            if (hit == null)
            {
                //Debug.Log("No item found at this position.");
                return;
            }

            //Debug.Log("Detected item: " + hit.gameObject.name);

            // Try to get the Item component
            var targetItem = hit.GetComponent<Rectangle>();
            if (targetItem == null)
            {
                //Debug.Log("Collider found, but it does not have an Item component.");
                return;
            }

            //Debug.Log("ItemChopper: Processing Square target");
            var square = TargetItem.GetGameObject.GetComponent<Rectangle>();

            square.DestroyBlock();
            HandleNextItemType(square);
            ApplyTargetType2ToSquare(square);

            reachedTarget = true;
            callback?.Invoke();
        }

        private void ApplyTargetType2IfNeeded()
        {
            if (setTargetType2)
            {
                //Debug.Log("ItemChopper: Setting on target item");
                TargetItem.GetItem.square.SetType(LevelTargetTypes.ExtraTargetType2, 1, LevelTargetTypes.NONE, 1);
            }
        }

        private bool ShouldDestroyItem()
        {
            if (TargetItem.GetItem.currentType == ItemsTypes.Eggs || TargetItem.GetItem.currentType == ItemsTypes.Pots)
                return false;
            return nextItemType == ItemsTypes.NONE ||
                   (TargetItem.GetItem.currentType != ItemsTypes.NONE &&
                    (TargetItem.GetItem.currentType != ItemsTypes.DiscoBall ||
                     MainManager.Instance.AdditionalSettings.MulticolorDestroyByBoostAndChopper)) &&
                   TargetItem.GetItem.currentType != ItemsTypes.TimeBomb;
        }

        private void DestroyTargetItem(Vector3 target)
        {
            //Debug.Log("DestroyTargetItem: Attempting to detect item using OverlapPoint.");

            // Define the layer mask for the objects you want to detect
            var layerMask = LayerMask.GetMask("Item");

            // Convert the transform position to Vector2 since OverlapPoint works in 2D
            Vector2 checkPosition = new Vector2(transform.position.x, transform.position.y);

            // Check for a collider at this position
            Collider2D hit = Physics2D.OverlapPoint(checkPosition, layerMask);

            if (hit == null)
            {
                //Debug.Log("No item found at this position.");
                return;
            }

            //Debug.Log("Detected item: " + hit.gameObject.name);

            // Try to get the Item component
            var targetItem = hit.GetComponent<Item>();
            if (targetItem == null)
            {
                //Debug.Log("Collider found, but it does not have an Item component.");
                return;
            }

            //Debug.Log($"Destroying target item: {targetItem.GetItem.name}");
            callback?.Invoke();
            targetItem.DestroyItem(true, true, _thisChopperItem);
        }


        private void ChangeTargetItemType()
        {
            //Debug.Log("DestroyTargetItem: Attempting to detect item using OverlapPoint.");

            // Define the layer mask for the objects you want to detect
            var layerMask = LayerMask.GetMask("Square");

            // Convert the transform position to Vector2 since OverlapPoint works in 2D
            Vector2 checkPosition = new Vector2(transform.position.x, transform.position.y);

            // Check for a collider at this position
            Collider2D hit = Physics2D.OverlapPoint(checkPosition, layerMask);

            if (hit == null)
            {
                //Debug.Log("No item found at this position.");
                return;
            }

            //Debug.Log("Detected item: " + hit.gameObject.name);

            // Try to get the Item component
            var targetItem = hit.GetComponent<Rectangle>();
            if (targetItem == null)
            {
                //Debug.Log("Collider found, but it does not have an Item component.");
                return;
            }


            //Debug.Log($"ItemChopper: Changing item type to: {nextItemType}");
            TargetItem.GetItem.NextType = nextItemType;

            TargetItem.GetItem.ChangeType(item =>
            {
                callback?.Invoke();
                if (item != null)
                {
                    //Debug.Log($"ItemChopper: Destroying changed item: {item.name}");
                    item.DestroyItem(CheckJustInItem: false);
                }
            }, false);

            TargetItem.GetItem.DestroyItem();
        }

        private void HandleNextItemType(Rectangle rectangle)
        {
            //Debug.Log($"ItemChopper: Handling next item type: {nextItemType}");
            if (nextItemType == ItemsTypes.RocketHorizontal)
            {
                //Item newItem = square.GenItem(false, nextItemType);

                // newItem.GetTopItemInterface().Destroy(newItem, null);
                MainManager.Instance.StripedShow(rectangle.gameObject, rectangle, true);
            }

            if (nextItemType == ItemsTypes.RocketVertical)
            {
                //Item newItem = square.GenItem(false, nextItemType);
                //newItem.GetTopItemInterface().Destroy(newItem, null);
                MainManager.Instance.StripedShow(rectangle.gameObject, rectangle, false);
            }

            if (nextItemType == ItemsTypes.Bomb)
            {
                //Item newItem = square.GenItem(false, nextItemType);
                //newItem.GetTopItemInterface().Destroy(newItem, null);
                MainManager.Instance.ShowDestroyPackage(rectangle, false);
                // item.GetTopItemInterface().Destroy(item, null);
            }
        }

        private void ApplyTargetType2ToSquare(Rectangle rectangle)
        {
            if (setTargetType2)
            {
                //Debug.Log("ItemChopper: Setting on square");
                rectangle.SetType(LevelTargetTypes.ExtraTargetType2, 1, LevelTargetTypes.NONE, 1);
            }
        }


        private void DestroyChopper()
        {
            var HeliTargetExpl = ObjectPoolManager.Instance.GetPooledObject("HeliTargetExpl", this);
            if (HeliTargetExpl != null)
            {
                HeliTargetExpl.transform.position = transform.position; // Create firework effect at launch position
            }

            trnsShadow.gameObject.SetActive(false);
            HeliAnimator.SetTrigger("HitTarget");

            MainManager.Instance.EndBusyOperation();

            gameObject.SetActive(false);
        }

        public Transform transObject;
        public Transform trnsBody;
        public Transform trnsShadow;
        public float gravity = -10f;
        public Vector2 groundVelocity;
        public float verticalVelocity;
        public float moveSpeed = 1.0f; // Adjust speed as needed
        public bool isGrounded = false;
        public float curveAmplitude = 0.2f; // Curve amplitude for the sine wave
        public float targetReachThreshold = 0.1f; // Distance to consider as "reached"
        private Vector3 initialShadowPosition;
        private Vector3 initialBodyPosition;
        private DG.Tweening.Sequence seq;


        public AnimationCurve speedCurve; // Define a curve for speed adjustment

        // private void Start()
        // {
        //     // Create a custom speed curve
        //     speedCurve = new AnimationCurve(
        //         new Keyframe(0f, 0f),    // Start slow
        //         new Keyframe(0.3f, 0.5f),  // Accelerate toward the middle
        //         new Keyframe(0.6f, 3f), // Peak speed around here
        //         new Keyframe(1f, 6f)     // Slow down toward the end
        //     );
        private int directionSetCount = 0; // Static counter for all instances
        private static int globalDirectionSetCount = 0; // Shared among all items
        private static bool isTriangleFormationComplete = false; // Shared state
        public Vector3 myStartPosition;

        internal void SetDirection(Vector2 v, ChopperItem fly, bool IsChopperCombine)
        {
            myStartPosition = transform.position;
            directionSetCount++; // Increment counter
            Random.InitState(GetHashCode());

            // Cache initial positions
            initialShadowPosition = trnsShadow.localPosition;
            initialBodyPosition = trnsBody.localPosition;

            // Detach from parent and set scale
            Transform originalParent = transform.parent;
            transform.SetParent(null);
            //  transform.localScale = new Vector3(0.42f, 0.42f, 0);
            // seq = DOTween.Sequence();
            //   myTween =seq.Append(transform.DOScale(0.42f * 1.4f, 0.3f).SetEase(DG.Tweening.Ease.InOutSine));

            if (IsChopperCombine)
            {
                FormTriangleAroundMain(originalParent);
                //  myTween =seq.PrependCallback(() =>
                // {
                //     if (gameObject.activeSelf)
                //     {
                //
                //     }
                // });
            }
            else
            {
                // Default behavior for single item
                StartTargetFinding(originalParent);
                // myTween =seq.PrependCallback(() =>
                // {
                //     if (gameObject.activeSelf)
                //     {
                //         StartTargetFinding(originalParent);
                //     }
                // });
            }
        }

        private void FormTriangleAroundMain(Transform originalParent)
        {
            // Define the radius of the circle
            float radius = 1.0f; // Adjust as needed

            // Calculate the angles for 3 positions (equilateral triangle)
            float[] angles = { 0, 120, 240 }; // Angles in degrees

            // Convert angles to radians and calculate positions
            Vector3[] trianglePositions = new Vector3[3];
            for (int i = 0; i < angles.Length; i++)
            {
                float rad = Mathf.Deg2Rad * angles[i];
                trianglePositions[i] = transform.position + new Vector3(
                    radius * Mathf.Cos(rad),
                    radius * Mathf.Sin(rad),
                    0 // Assuming a 2D plane
                );
            }

            // Animate items to triangle positions
            seq = DOTween.Sequence();

            // First do the movement
            seq.Append(transform.DOMove(trianglePositions[ChopperIndex - 1], 0.5f)
                .SetEase(DG.Tweening.Ease.InOutQuad));

            seq.Join(transform.DOScale(0.42f * 1.4f, 0.3f).SetEase(DG.Tweening.Ease.InOutSine));

            // Then add delay
            seq.AppendInterval(ChopperIndex * 0.1f);
            seq.Join(transform.DOScale(0.42f * 1.4f, 0.3f).SetEase(DG.Tweening.Ease.InOutSine));


            // Finally add completion callback
            seq.InsertCallback(0.4f, () => { StartTargetFinding(originalParent); });
        }


        private void DefaultBehavior()
        {
            StartTargetFinding(transform.parent);
        }


        private void DefaultBehavior(Transform originalParent)
        {
            DG.Tweening.Sequence seq = DOTween.Sequence();
            seq.Join(transform.DOScale(0.42f * 1.4f, 0.1f).SetEase(DG.Tweening.Ease.InOutQuad));
            seq.AppendCallback(() =>
            {
                if (gameObject.activeSelf)
                {
                    StartTargetFinding(originalParent);
                }
            });
        }


        private void oldFormTriangleAroundMain(Transform originalParent)
        {
            // Define the radius of the circle
            float radius = 1.0f; // Adjust as needed

            // Calculate the angles for 3 positions (equilateral triangle)
            float[] angles = { 0, 120, 240 }; // Angles in degrees

            // Convert angles to radians and calculate positions
            Vector3[] trianglePositions = new Vector3[3];
            for (int i = 0; i < angles.Length; i++)
            {
                float rad = Mathf.Deg2Rad * angles[i];
                trianglePositions[i] = transform.position + new Vector3(
                    radius * Mathf.Cos(rad),
                    radius * Mathf.Sin(rad),
                    0 // Assuming a 2D plane
                );
            }

            // Animate items to triangle positions
            DG.Tweening.Sequence seq = DOTween.Sequence();
            for (int i = 0; i < 3; i++)
            {
                if (i == directionSetCount - 1) // Current item based on directionSetCount
                {
                    seq.Join(transform.DOMove(trianglePositions[i], 0.5f).SetEase(DG.Tweening.Ease.InOutQuad));
                }
            }

            // Wait for the movement to complete before starting
            seq.AppendInterval(1f);
            seq.AppendCallback(() => { StartTargetFinding(originalParent); });
        }


        private IEnumerator DelayedTargetFinding(Transform originalParent, float delay)
        {
            yield return new WaitForSeconds(delay);
            StartTargetFinding(originalParent);
        }


        private Coroutine targetLoopCoroutine;

        #region Target Finding & Processing

        private bool isSearchingTarget = false;
        private Transform originalParentCache;

        private void StartTargetFinding(Transform originalParent)
        {
            isSearchingTarget = true;
            originalParentCache = originalParent;
        }

        private void Update()
        {
            if (isSearchingTarget && TargetItem == null)
            {
                FindTarget();
            }
            else if (isSearchingTarget && TargetItem != null)
            {
                isSearchingTarget = false;
                ProcessAcquiredTarget(originalParentCache);
            }
        }


        private void ProcessAcquiredTarget(Transform originalParent)
        {
            Vector3 startPos = transform.position;
            GameObject targetGO = TargetItem.GetGameObject; // cache to avoid repeated property lookups
            Vector3 targetPos = targetGO.transform.position;

            //Debug.Log("Start Position: {0}, Target Position: {1}" + startPos+ targetPos);

            bool isVertical = IsVerticalMovement(startPos, targetPos);
            float duration = CalculateDynamicDuration(startPos, targetPos);

            if (targetReset)
                TransitionToNewPath(originalParent, targetPos, isVertical);
            else
                StartHelicopterMovement(startPos, targetPos, duration, isVertical, originalParent);
        }

        #endregion

        #region Tween & Movement Methods

        private bool IsTweenActive()
        {
            return myTween != null && !myTween.IsComplete() && myTween.active;
        }

        private string currentTweenID;

        private void ResetTween()
        {
            //Debug.Log($"ResetTween: Starting tween reset for {gameObject.name}");

            if (seq != null)
            {
                //Debug.Log($"ResetTween: Killing sequence for {gameObject.name}");
                seq.Pause();
                lastKnownPosition = transform.position;
                seq.Kill();
                seq = null;
            }
            else
            {
                //Debug.Log($"ResetTween: No sequence to kill for {gameObject.name}");
            }

            if (!string.IsNullOrEmpty(currentTweenID))
            {
                //Debug.Log($"ResetTween: Killing tween with ID {currentTweenID} for {gameObject.name}");
                DOTween.debugMode = true;

                DOTween.Kill(currentTweenID); // Kill the tween using the ID
                currentTweenID = null; // Reset the ID
            }
            else
            {
                //Debug.Log($"ResetTween: No tween ID to kill for {gameObject.name}");
            }

            if (myTween != null)
            {
                //Debug.Log($"ResetTween: Killing tween for {gameObject.name}");
                myTween.Kill();
                myTween = null;
            }
            else
            {
                //Debug.Log($"ResetTween: No tween to kill for {gameObject.name}");
            }

            //Debug.Log($"ResetTween: Completed tween reset for {gameObject.name}");
        }

        private Vector3 lastTarget; // stores the previous target
        private float currentTweenDuration; // store the dynamic duration for the current tween

        private Vector3 lastKnownPosition;

        /// <summary>
        /// Linear (helicopter-style) movement from start to target.
        /// </summary>
        private void StartHelicopterMovement(Vector3 start, Vector3 target, float duration, bool isVertical, Transform originalParent)
        {
            SetupHelicopterMovementParameters(start, target, duration);
            Vector3[] path = CreateHelicopterPath(start, target);
            LogHelicopterPath(path, duration);
            CreateAndStartHelicopterTween(path, duration, start, target, isVertical, originalParent);
        }

        private void SetupHelicopterMovementParameters(Vector3 start, Vector3 target, float duration)
        {
            currentTweenID = Guid.NewGuid().ToString();
            currentTweenDuration = duration; // Store duration for later use in transitions

            // Compute control point and store parameters for future transitions
            Vector3 controlPoint = ComputeBezierControlPoint(start, target);
            lastControlPoint = controlPoint;
            lastTarget = target;
        }

        private Vector3[] CreateHelicopterPath(Vector3 start, Vector3 target)
        {
            // The path is defined in the order: target point, starting point, and control point.
            return new Vector3[] { target, start, lastControlPoint };
        }

        private void LogHelicopterPath(Vector3[] path, float duration)
        {
            // Log the Bezier path information (using the first element as start for logging purposes).
            LogBezierData(path[0], path[0], path[2], duration);
        }

        private Tween currentPathTween;

        private void CreateAndStartHelicopterTween(Vector3[] path, float duration, Vector3 start, Vector3 target, bool isVertical, Transform originalParent)
        {
            var seq = DOTween.Sequence();

            myTween = seq.Append(
                    transform.DOPath(path, duration, PathType.CubicBezier, PathMode.Sidescroller2D, 5)
                        .SetEase(speedCurve)
                        // Insert a callback to change the path after the first tween
                        .OnUpdate(() => UpdateMovement(start, target, isVertical, originalParent))
                        .OnComplete(() => FinalizeMovement(originalParent, target))
                )
                // Scale up at the start
                .Join(transform.DOScale(0.42f * 1.4f, 0.2f).SetEase(speedCurve))
                // Scale down near the end (after 80% of the duration)
                //.Insert(duration * 0.8f, transform.DOScale(0.42f, 0.2f).SetEase(speedCurve))
                .SetId(currentTweenID);
        }

        /// <summary>
        /// Called when the current tween must be terminated and a new path is required.
        /// This version uses both the previous momentum and dynamic duration.
        /// </summary>
        private void TransitionToNewPath(Transform originalParent, Vector3 updatedTarget, bool isVertical)
        {
            var tweenProgress = GetTweenProgressData();
            float elapsedTime = tweenProgress.Item1;
            float progress = tweenProgress.Item2;
            Vector3 currentPos = tweenProgress.Item3;

            var newPathData = GetNewPathData(updatedTarget, currentPos);
            Vector3 newControlPoint = newPathData.Item1;
            Vector3[] newPath = newPathData.Item2;

            float newDuration = ComputeBlendedDuration(elapsedTime, currentPos, updatedTarget, progress);

            PrepareNewTween();
            CreateAndStartNewTweenSequence(newPath, newDuration, currentPos, updatedTarget, isVertical, originalParent);

            lastTarget = updatedTarget;
            lastControlPoint = newControlPoint;
        }

        private (float, float, Vector3) GetTweenProgressData()
        {
            float elapsedTime = myTween.Elapsed();
            float progress = elapsedTime / currentTweenDuration;
            Vector3 currentPos = myTween.PathGetPoint(elapsedTime);
            //Debug.Log($"TransitionToNewPath: Current position: {currentPos}");
            return (elapsedTime, progress, currentPos);
        }

        private (Vector3, Vector3[]) GetNewPathData(Vector3 updatedTarget, Vector3 currentPos)
        {
            Vector3 newControlPoint = CalculateNewControlPoint(updatedTarget);
            Vector3[] newPath = BuildNewPath(updatedTarget);

            // If the target is very close, adjust the control point further to create a figure 8 pattern.
            float distance = Vector3.Distance(updatedTarget, currentPos);
            float closeThreshold = 3f; // Adjust threshold as needed
            //Debug.Log($"TransitionToNewPath: Distance to target: {distance}");
            // if (distance < closeThreshold)
            // {
            //     Vector3 direction = (updatedTarget - currentPos).normalized;
            //     // Calculate perpendicular vector on the XY plane
            //     Vector3 perpendicular = new Vector3(-direction.y, direction.x, direction.z);
            //     float offsetFactor = (closeThreshold - distance) * 10f; // Adjust factor as needed
            //     newControlPoint += perpendicular * offsetFactor;
            //     //Debug.Log($"TransitionToNewPath: Adjusted control point for close target: {newControlPoint}");
            // }

            //Debug.Log($"TransitionToNewPath: New path: {currentPos}, {newControlPoint}, {updatedTarget}");
            return (newControlPoint, newPath);
        }

        private float ComputeBlendedDuration(float elapsedTime, Vector3 currentPos, Vector3 updatedTarget, float progress)
        {
            float previousRemainingDuration = currentTweenDuration - elapsedTime;
            float calculatedDuration = CalculateDynamicDuration(currentPos, updatedTarget);
            float momentum = speedCurve.Evaluate(progress);
            float blendFactor = Mathf.Clamp01(momentum);
            float newDuration = Mathf.Lerp(previousRemainingDuration, calculatedDuration, blendFactor);
            //Debug.Log($"TransitionToNewPath: Blended new duration: {newDuration}");
            return newDuration;
        }

        private void PrepareNewTween()
        {
            currentTweenID = Guid.NewGuid().ToString();
            ResetTween();
            //Debug.Log("TransitionToNewPath: Old tween reset");
        }

        private void CreateAndStartNewTweenSequence(Vector3[] newPath, float newDuration, Vector3 currentPos, Vector3 updatedTarget, bool isVertical, Transform originalParent)
        {
            var seq = DOTween.Sequence();
            myTween = seq.Append(
                    transform.DOPath(newPath, newDuration, PathType.CubicBezier, PathMode.Ignore, 1)
                        .SetEase(DG.Tweening.Ease.Linear)
                        .OnUpdate(() =>
                        {
                            //Debug.Log("TransitionToNewPath: Tween updating");
                            UpdateMovement(currentPos, updatedTarget, isVertical, originalParent);
                        })
                        .OnComplete(() =>
                        {
                            //Debug.Log("TransitionToNewPath: Tween complete, finalizing movement");
                            FinalizeMovement(originalParent, updatedTarget);
                        })
                )
                .Join(transform.DOScale(0.42f * 1.4f, 0.2f).SetEase(speedCurve))
                .Insert(newDuration * 0.8f, transform.DOScale(0.42f, 0.2f).SetEase(speedCurve))
                .SetId(currentTweenID);
        }

// Helper: Calculate a new control point for smooth transition
        private Vector3 CalculateNewControlPoint(Vector3 updatedTarget)
        {
            float distanceFactor = Mathf.Clamp01(Vector3.Distance(lastTarget, updatedTarget) / 2f);
            return Vector3.Lerp(lastTarget, updatedTarget, distanceFactor);
        }

// Helper: Construct the new path for the tween
        private Vector3[] BuildNewPath(Vector3 updatedTarget)
        {
            // New path: target, current position, and previous target point
            return new Vector3[] { updatedTarget, transform.position, lastTarget };
        }

        private Vector3 lastControlPoint; // New field for storing the last control point

        private Vector3 ComputeBezierControlPoint(Vector3 start, Vector3 target)
        {
            float distance = Vector3.Distance(start, target);
            float arcHeight = Mathf.Clamp(distance / 2f, 5f, 10f);
            float t = 0.5f;
            float offset = Mathf.SmoothStep(0f, arcHeight, t) * (1 - t);
            Vector3 direction = (target - start).normalized;
            Vector3 perpendicular = new Vector3(-direction.y, direction.x, 0f);
            Vector3 midPoint = (start + target) * 0.5f;
            Vector3 controlPoint = midPoint + perpendicular * offset;
            //Debug.Log($"Control Point: {controlPoint} (Start: {start}, Target: {target}, Offset: {offset})");
            return controlPoint;
        }

        private void LogBezierData(Vector3 start, Vector3 control, Vector3 target, float duration)
        {
            //Debug.Log($"Bezier Path - Start: {start}, Control: {control}, Target: {target}");
            // Debug.unityLogger.logEnabled = true;
            // Debug.DrawLine(start, control, UnityEngine.Color.yellow, duration);
            //Debug.DrawLine(control, target, UnityEngine.Color.yellow, duration);
            // Debug.unityLogger.logEnabled = false;
        }

        private void UpdateMovement(Vector3 start, Vector3 target, bool isVertical, Transform originalParent)
        {
            lastKnownPosition = transform.position;
            float totalDistance = Vector3.Distance(start, target);
            float traveled = (transform.position - start).magnitude;
            float t = traveled / totalDistance;
//Debug.unityLogger.logEnabled = true;
            // Draw debug lines to visualize movement.
            //Debug.DrawLine(start, target, UnityEngine.Color.red);
            //  Debug.DrawLine(transform.position, target, UnityEngine.Color.green);
            //Debug.unityLogger.logEnabled = false;

            ValidateTarget(originalParent);
            // ApplyArcDisplacement(totalDistance, t, isVertical);
        }

        bool targetReset = false;

        private void ValidateTarget(Transform originalParent)
        {
            // If the target is lost (null, inactive, or in an invalid state), restart the search.
            if (TargetItem == null ||
                TargetItem.GetItem?.destroying == true ||
                !TargetItem.GetGameObject.activeSelf ||
                (TargetItem.GetGameObject.GetComponent<Rectangle>()?.IsObstacle() == false))
            {
                RestartTargetSearch();
                //  TransitionToNewPath(originalParent, TargetItem.GetGameObject.transform.position, IsVerticalMovement(transform.position, TargetItem.GetGameObject.transform.position));
            }
        }


// Cache debug messages to avoid string garbage
        private static readonly string DebugPrefix = "RestartTargetSearch: ";

        private void RestartTargetSearch()
        {
            if (Debug.isDebugBuild) //Debug.Log(DebugPrefix + "Starting for " + gameObject.name);

                if (!gameObject.activeInHierarchy)
                {
                    //Debug.Log(DebugPrefix + "GameObject not active, returning.");
                    return;
                }

            // Stop coroutines and reset flags
            isSearchingTarget = true;
            targetReset = true;
            TargetItem = null;
            //originalParentCache = transform;

            //Debug.Log(DebugPrefix + "Target reset and search restarted.");
        }

        private void FinalizeMovement(Transform originalParent, Vector3 ReachedTarget)
        {
            //Debug.Log($"FinalizeMovement: Starting finalization for {gameObject.name}");

            // Only finalize if the target is still valid.
            if (TargetItem == null || !TargetItem.GetGameObject.activeSelf)
            {
                //Debug.Log("Movement complete: Target is invalid, skipping finalization.");
                return;
            }

            //Debug.Log($"FinalizeMovement: Resetting positions for {gameObject.name}");
            ResetPositions();

            if (originalParent == null && _thisChopperItem != null)
            {
                //Debug.Log($"FinalizeMovement: Original parent null, attempting to use thisItem parent");
                originalParent = _thisChopperItem.transform;
            }

            if (originalParent != null)
            {
                //Debug.Log($"FinalizeMovement: Reattaching to parent {originalParent.name}");
                ReattachToParent(originalParent);
            }
            else
            {
                //Debug.LogWarning("FinalizeMovement: originalParent is null and could not be recovered.");
            }

            if (gameObject.activeSelf)
            {
                //Debug.Log($"FinalizeMovement: GameObject {gameObject.name} is active, calling myReachItem");
                myReachItem(ReachedTarget);
            }
            else
            {
                //Debug.Log($"FinalizeMovement: GameObject {gameObject.name} is not active, skipping myReachItem");
            }

            //Debug.Log($"FinalizeMovement: Completed for {gameObject.name}");
            targetReset = false;
        }


        private void ApplyArcDisplacement(float distance, float t, bool isVertical)
        {
            float arcHeight = Mathf.Clamp(distance / 2f, 5f, 10f);
            float displacement = Mathf.SmoothStep(0f, arcHeight, t) * (1 - t);
            AdjustBodyPosition(displacement, isVertical);
        }

        private void AdjustBodyPosition(float displacement, bool isVertical)
        {
            if (float.IsNaN(displacement))
            {
                //Debug.LogWarning("Invalid displacement value. Defaulting to 0.");
                displacement = 0f;
            }

            Vector3 newBodyPos = initialBodyPosition;
            displacement = Mathf.Clamp(displacement, -10f, 10f);

            if (isVertical)
                newBodyPos.x += displacement;
            else
                newBodyPos.y += displacement;

            if (Vector3.SqrMagnitude(newBodyPos) > 100f || float.IsNaN(newBodyPos.x) || float.IsNaN(newBodyPos.y))
            {
                //Debug.LogWarning("Calculated position invalid. Reverting to initial position.");
                newBodyPos = initialBodyPosition;
            }

            trnsBody.localPosition = newBodyPos;
        }

        private void ResetPositions()
        {
            trnsShadow.localPosition = Vector3.zero;
            trnsBody.localPosition = initialBodyPosition;
        }

        private void ReattachToParent(Transform originalParent)
        {
            transform.SetParent(originalParent);
            transform.localScale = Vector3.one;
        }

        #endregion

        #region Utility Methods

        private bool IsVerticalMovement(Vector3 start, Vector3 target)
        {
            // Determine if the vertical displacement is greater than the horizontal.
            return Mathf.Abs(target.y - start.y) > Mathf.Abs(target.x - start.x);
        }

        private float CalculateDynamicDuration(Vector3 start, Vector3 target)
        {
            float distance = Vector3.Distance(start, target);
            float speed = 6f;
            return Mathf.Clamp(distance / speed, 1f, 1.8f);
        }

        public void SecondPartDestroyAnimation(Action _callback)
        {
            callback = _callback;
            // Optionally, add a LeanTween sequence here for additional destruction effects.
        }

        #endregion

        private void myFindTarget()
        {
            // Find hard to reach alone items or cages
            IEnumerable<IChopperTargetable> items = new List<IChopperTargetable>();
            // Check square targets
            if (!items.Any())
            {
                // looking for another item which is target of the current level
                var targetContainer = MainManager.Instance.levelData.GetFirstTarget(true);
                if (targetContainer != null)
                {
                    if (targetContainer.prefabs.FirstOrDefault()?.GetComponent<Rectangle>() != null ||
                        targetContainer.prefabs.FirstOrDefault()?.GetComponent<LayeredBlock>() != null)
                        items = MainManager.Instance.field.squaresArray.Where(i => i.type == (LevelTargetTypes)Enum.Parse(typeof(LevelTargetTypes), targetContainer.name))
                            .Select(i => i.Item).Where(i => !(i is null) && ArgItems(i)).ChopperCondition(gameObject, _thisChopperItem, ChopperIndex);

                    if (targetContainer.name == "Marshmello")
                    {
                        items = MainManager.Instance.field.squaresArray.Where(i => i.Item != null && i.subSquares.Count > 1 && i.subSquares.Any(i => i.type == LevelTargetTypes.ExtraTargetType4))
                            .Select(i => i.Item);
                    }
                }
            }

            if (!items.Any())
                items = MainManager.Instance.field.GetLonelyItemsOrCage()
                    .Where(i => ArgItems(i));
            if (setTargetType2)
                items = MainManager.Instance.field.squaresArray.Where(i => i.type != LevelTargetTypes.NONE && i.type != LevelTargetTypes.ExtraTargetType2).Select(i => i.Item).Where(i =>
                    i != null && i.Explodable).ChopperCondition(gameObject, _thisChopperItem, ChopperIndex);
            if (!items.Any())
            {
                var list = MainManager.Instance.field.squaresArray.Where(i => i.type != LevelTargetTypes.NONE).Select(i => i.Item).Where(i =>
                    i != null && i.GetComponent<TargetComponent>() && !i.Combinable);
                if (list.Any())
                    items = list.Where(i => i.square.nextRectangle != null && i.square.nextRectangle.Item && i.square.nextRectangle.Item.Explodable).Select(i => i.square.nextRectangle?.Item)
                        .ChopperCondition(gameObject, _thisChopperItem, ChopperIndex);
            }


            // Looking for blocks
            if (items == null || !items.Any())
            {
                var obstacles = MainManager.Instance.field.squaresArray
                    .Where(i => i.IsObstacle() && i.IsHaveDestroybleObstacle() && i.GetSubSquare().type != LevelTargetTypes.ExtraTargetType6);

                if (nextItemType == ItemsTypes.RocketHorizontal || nextItemType == ItemsTypes.RocketVertical)
                {
                    int targetRow = nextItemType == ItemsTypes.RocketHorizontal ? MainManager.Instance.field.GetRowWithMostLayers() : -1;
                    int targetCol = nextItemType == ItemsTypes.RocketVertical ? MainManager.Instance.field.GetColWithMostLayers() : -1;

                    items = obstacles.Where(i => i.row == targetRow || i.col == targetCol);
                }
                else
                {
                    items = obstacles;
                }
            }


            // Looking through all items
            if (items == null || !items.Any())
            {
                items = MainManager.Instance.field.GetItems().Where(i => i.Explodable).ChopperCondition(gameObject, _thisChopperItem, ChopperIndex);
            }


            TargetItem = items.ChopperCondition(gameObject, _thisChopperItem, ChopperIndex)
                .OrderBy(i => Vector3.Distance(transform.position, i.GetGameObject.transform.position) - (float)(i.GetItem?.currentType ?? 0)).FirstOrDefault();
            if (TargetItem != null && (Object)TargetItem != _thisChopperItem && (TargetItem.GetChopperTarget == null || TargetItem.GetChopperTarget == gameObject))
            {
                TargetItem.GetChopperTarget = gameObject;
            }
        }

        private void FindTarget()
        {
            //  //Debug.Log("Find Target: Starting target search.");

            IEnumerable<IChopperTargetable> items = new List<IChopperTargetable>();

            // Try to find hard-to-reach items or cages
            // //Debug.Log("Find Target: Searching for hard-to-reach items...");
            items = FindHardToReachItems(items);
            //  //Debug.Log($"Find Target: Found {items.Count()} hard-to-reach items. {name}");

            // If no items found, try to find other specific targets
            if (!items.Any())
            {
                // //Debug.Log("Find Target: No hard-to-reach items found. Searching for specific targets...");
                items = FindSpecificTargets();
                // //Debug.Log($"Find Target: Found {items.Count()} specific targets.");
            }
            //    if (!items.Any())
            // {
            //    // //Debug.Log("Find Target: No hard-to-reach items found. Searching for specific targets...");
            //    // //Debug.Log($"Find Target: Found {items.Count()} specific targets.");
            // }


            // If still no items found, try to find ingredients
            if (!items.Any())
            {
                items = FindSpiralTargets();
                //  //Debug.Log($"Find Target: Found {items.Count()} ingredients.");
            }

            // If still no items found, look for blocks
            if (!items.Any())
            {
                //  //Debug.Log("Find Target: FindEmailBlocks No ingredients found. Searching for obstacle blocks...");
                items = FindObstacleBlocks();
                //  //Debug.Log($"Find Target: FindEmailBlocks Found {items.Count()} obstacle blocks.");
            }

            if (!items.Any())
            {
                //  //Debug.Log("Find Target: FindEmailBlocks No obstacle blocks found. Searching for all items...");
                items = FindEmailBlocks();
                // //Debug.Log($"Find Target: FindEmailBlocks Found {items.Count()} total items.");
            }
            //   if (!items.Any())
            // {
            //     //Debug.Log("Find Target: FindEmailBlocks No obstacle blocks found. Searching for all items...");
            //     items = FindSquareSugerNoGenBlocks();
            //     //Debug.Log($"Find Target: FindEmailBlocks Found {items.Count()} total items.");
            // }


            // If still no items found, look for all items
            if (!items.Any())
            {
                //Debug.Log("Find Target: No obstacle blocks found. Searching for all items...");
                items = FindAllItems();
                //Debug.Log($"Find Target: Found {items.Count()} total items.");
            }


            // Set the TargetItem
            //Debug.Log($"Find Target: Setting TargetItem with {items.Count()} items found.");
            SetTargetItem(items);
        }

        private IEnumerable<IChopperTargetable> FindHardToReachItems(IEnumerable<IChopperTargetable> items)
        {
            var targetContainer = MainManager.Instance.levelData.GetFirstTarget(true);
            if (targetContainer == null)
            {
                //Debug.Log("Find HardToReachItems: No target container found.");
                //Debug.Log($"Find HardToReachItems: Found {items.Count()} items.");
                return items;
            }

            //Debug.Log($"Find HardToReachItems: Found target container: {targetContainer.name}");

            var firstPrefab = targetContainer.prefabs.FirstOrDefault();
            bool isSquareOrLayered = firstPrefab?.GetComponent<Rectangle>() != null ||
                                     firstPrefab?.GetComponent<LayeredBlock>() != null;

            if (isSquareOrLayered)
            {
                //Debug.Log("Find HardToReachItems: Identified valid prefab (Square or LayeredBlock).");
                items = GetItemsMatchingSquareType(targetContainer);

                //Debug.Log($"Find HardToReachItems: Found {items.Count()} items matching target container name.");
                LogSortedItems(items);
            }

            if (targetContainer.name == "Marshmello")
            {
                //Debug.Log("Find HardToReachItems: Target container is Marshmello, finding matching items.");
                items = MainManager.Instance.field.squaresArray
                    .Where(square => square.Item != null &&
                                     square.subSquares.Count > 1 &&
                                     square.subSquares.Any(subSquare => subSquare.type == LevelTargetTypes.ExtraTargetType4))
                    .Select(square => square.Item);
            }

            //Debug.Log($"Find HardToReachItems: Found {items.Count()} items.");
            return items;
        }

// Define an interface
        public interface ITargetContainer
        {
            string name { get; }
            List<GameObject> prefabs { get; set; }
        }

        //Implement that interface in the classes that you use for 'targetContainer'


        private IEnumerable<IChopperTargetable> GetItemsMatchingSquareType(TargetContainer targetContainer)
        {
            LevelTargetTypes levelTargetType = LevelTargetTypes.NONE;
            try
            {
                levelTargetType = (LevelTargetTypes)Enum.Parse(typeof(LevelTargetTypes), targetContainer.name, true);
            }
            catch (Exception ex)
            {
                // Handle the case when parsing fails
                // Debug.LogError("Invalid SquareType: " + targetContainer.name);
            }
            // Debug.Log($"Find HardToReachItems: Filtering squares with type {squareType}");

            return MainManager.Instance.field.squaresArray
                .Where(square =>
                {
                    bool matches = square.type == levelTargetType;
                    // Debug.Log($"Square at ({square.row},{square.col}) type {square.type} matches {squareType}: {matches}");
                    return matches;
                })
                .Select(square =>
                {
                    // Debug.Log($"GetItemsMatchingSquareType: Getting item from square at ({square.row},{square.col})");
                    return square.Item;
                })
                .Where(item =>
                {
                    bool valid = item != null && ArgItems(item);
                    // Debug.Log($"GetItemsMatchingSquareType: Item {(item?.gameObject?.name ?? "null")} valid: {valid}");
                    return valid;
                })
                .ChopperCondition(gameObject, _thisChopperItem, ChopperIndex);
        }

        private void LogSortedItems(IEnumerable<IChopperTargetable> items)
        {
            var sortedItems = items.OrderBy(item =>
            {
                float distance = -Vector3.Distance(transform.position, item.GetGameObject.transform.position);
                float typeValue = (float)(item.GetItem?.currentType ?? 0);
                float sortValue = distance - typeValue;
                // Debug.Log($"Sorting Item: {item.GetGameObject.name}, Distance: {distance}, TypeValue: {typeValue}, SortValue: {sortValue}");
                return sortValue;
            }).ToList();

            foreach (var sortedItem in sortedItems)
            {
                // Debug.Log($"Sorted Item: {sortedItem.GetGameObject.name}");
            }
        }

        private IEnumerable<IChopperTargetable> FindSpecificTargets()
        {
            //  Debug.Log("Find SpecificTargets: Searching for specific targets...");
            var specificTargets = MainManager.Instance.field.squaresArray
                .Where(i => i.Item != null && i.subSquares.Count > 1 && i.subSquares.Any(i => i.type == LevelTargetTypes.ExtraTargetType4))
                .Select(i => i.Item);

            //  Debug.Log($"Find SpecificTargets: Found {specificTargets.Count()} specific targets.");
            return specificTargets;
        }

        private IEnumerable<IChopperTargetable> FindObstacleBlocks()
        {
            //  Debug.Log("Find ObstacleBlocks: Searching for obstacle blocks...");
            var obstacleBlocks = MainManager.Instance.field.squaresArray
                .Where(i => i.IsObstacle() && i.IsHaveDestroybleObstacle() && i.GetSubSquare().type != LevelTargetTypes.ExtraTargetType6)
                .ChopperCondition(gameObject, _thisChopperItem, ChopperIndex);

            // Debug.Log($"Find ObstacleBlocks: Found {obstacleBlocks.Count()} obstacle blocks.{name}");
            return obstacleBlocks;
        }

        private IEnumerable<IChopperTargetable> FindEmailBlocks()
        {
            //  Debug.Log("Find ObstacleBlocks: Searching for obstacle blocks...");
            var obstacleBlocks = MainManager.Instance.field.squaresArray
                .Where(i => i.IsObstacle() && i.GetSubSquare().type == LevelTargetTypes.Mails).ChopperCondition(gameObject, _thisChopperItem, ChopperIndex);

            //  Debug.Log($"Find ObstacleBlocks: Found {obstacleBlocks.Count()} obstacle blocks.{name}");
            return obstacleBlocks;
        }

        private IEnumerable<IChopperTargetable> FindSpiralTargets()
        {
            //  Debug.Log("Find ObstacleBlocks: Searching for spiral items...");
            var list = MainManager.Instance.field.squaresArray
                .Where(i => i.type != LevelTargetTypes.NONE)
                .Select(i => i.Item)
                .Where(i => i != null &&
                            (i.currentType == ItemsTypes.Eggs || i.currentType == ItemsTypes.Pots));

            if (list.Any())
            {
                //  Debug.Log("Find ObstacleBlocks: Found spiral items.");
                var spiralItems = list.ChopperCondition(gameObject, _thisChopperItem, ChopperIndex);
                //  Debug.Log($"Find ObstacleBlocks: Found {spiralItems.Count()} valid spiral items.{name}");
                return spiralItems;
            }

            //  Debug.Log("Find ObstacleBlocks: No spiral items found.");
            return new List<IChopperTargetable>();
        }

        private IEnumerable<IChopperTargetable> FindSquareSugerNoGenBlocks()
        {
            //  Debug.Log("Find ObstacleBlocks: Searching for obstacle blocks...");
            var obstacleBlocks = MainManager.Instance.field.squaresArray
                .Where(i => i.GetSubSquare().type == LevelTargetTypes.GrassType2).ChopperCondition(gameObject, _thisChopperItem, ChopperIndex);

            //  Debug.Log($"Find ObstacleBlocks: Found {obstacleBlocks.Count()} obstacle blocks.{name}");
            return obstacleBlocks;
        }

        private IEnumerable<IChopperTargetable> FindAllItems()
        {
            //   Debug.Log("Find AllItems: Searching for all items...");
            var allItems = MainManager.Instance.field.GetItems(true)
                .Where(i => i.Explodable)
                .Where(i => !i.JustCreatedItem)
                .ChopperCondition(gameObject, _thisChopperItem, ChopperIndex);

            // Debug.Log($"Find AllItems: Found {allItems.Count()} items.");
            return allItems;
        }

        public class TargetSquare
        {
            public IChopperTargetable Target { get; set; }
            public Rectangle Rectangle { get; set; }
        }

        private void SetTargetItem(IEnumerable<IChopperTargetable> items)
        {
            var currentPosition = transform.position;
            var filteredItems = items.ChopperCondition(gameObject, _thisChopperItem, ChopperIndex)
                .Select(i => new TargetSquare { Target = i, Rectangle = GetValidSquare(i) })
                .Where(x => x.Rectangle != null) // Only keep targets with a valid Square
                .ToList();

            if (filteredItems.Count == 0)
            {
                TargetItem = null;
                return;
            }

            if (nextItemType == ItemsTypes.RocketHorizontal)
            {
                TargetItem = FindSquareWithMostInRow(filteredItems);
            }
            else if (nextItemType == ItemsTypes.RocketVertical)
            {
                TargetItem = FindSquareWithMostInColumn(filteredItems);
            }
            else if (nextItemType == ItemsTypes.Bomb)
            {
                TargetItem = FindCenterOfMostTargets(filteredItems);
            }
            else
            {
                TargetItem = filteredItems
                    .OrderByDescending(x => -Vector3.Distance(currentPosition, x.Target.GetGameObject.transform.position) - (float)(x.Target.GetItem?.currentType ?? 0))
                    .Select(x => x.Target)
                    .FirstOrDefault();
            }

            if (TargetItem != null)
            {
                Debug.Log($"SetTargetItem: Selected target is {TargetItem.GetGameObject.name} at position {TargetItem.GetGameObject.transform.position}");
                if (IsValidTarget(TargetItem))
                {
                    TargetItem.GetChopperTarget = gameObject;
                }
            }
            else
            {
                // Debug.Log("SetTargetItem: No valid target found after ordering.");
            }
        }

        private Rectangle GetValidSquare(IChopperTargetable target)
        {
            // Debug.Log($"GetValidSquare: Checking square for target {target.GetGameObject.name}");

            // Try to get the Item first, then its square
            var item = target.GetItem;
            Rectangle rectangle = null;

            if (item != null)
            {
                rectangle = item.square; // Get the square from the Item
                //Debug.Log($"GetValidSquare: Target is an Item. Square found: {square != null}");
            }
            else
            {
                // Fallback to direct Square component if not an Item
                rectangle = target.GetGameObject.GetComponent<Rectangle>();
                // Debug.Log($"GetValidSquare: Target is not an Item. Direct Square found: {square != null}");
            }

            if (rectangle != null)
            {
                // Debug.Log($"GetValidSquare: Square found. Has subsquares: {square.subSquares?.Count > 0}");

                // If this square has subsquares, skip if it’s a grass
                if (rectangle.subSquares != null && rectangle.subSquares.Count > 0)
                {
                    bool isValid = true;
                    //  Debug.Log($"GetValidSquare: Square has subsquares. Type: {square.type}, IsValid: {isValid}");
                    return isValid ? rectangle : null;
                }

                return rectangle;
            }

            // If no square yet, check if it’s a subsquare and get its parent
            var parentSquare = target.GetGameObject.GetComponentInParent<Rectangle>();
            bool isValidSubsquare = parentSquare != null &&
                                    parentSquare.subSquares != null &&
                                    parentSquare.subSquares.Any(sub => sub.gameObject == target.GetGameObject);

            //   Debug.Log($"GetValidSquare: Checking parent square. Found: {parentSquare != null}, IsValidSubsquare: {isValidSubsquare}");
            return isValidSubsquare ? parentSquare : null;
        }


        private IChopperTargetable FindSquareWithMostInRow(IEnumerable<TargetSquare> items)
        {
            var squaresByRow = items
                .GroupBy(x => x.Rectangle.row)
                .Select(g => new { Row = g.Key, Count = g.Count(), Items = g.ToList() })
                .OrderByDescending(g => g.Count)
                .FirstOrDefault();

            var result = squaresByRow?.Items
                .OrderBy(x => Vector3.Distance(transform.position, x.Target.GetGameObject.transform.position))
                .Select(x => x.Target)
                .FirstOrDefault();

            return result;
        }

        private IChopperTargetable FindSquareWithMostInColumn(IEnumerable<TargetSquare> items)
        {
            var squaresByColumn = items
                .GroupBy(x => x.Rectangle.col)
                .Select(g => new { Col = g.Key, Count = g.Count(), Items = g.ToList() })
                .OrderByDescending(g => g.Count)
                .FirstOrDefault();

            var result = squaresByColumn?.Items
                .OrderBy(x => Vector3.Distance(transform.position, x.Target.GetGameObject.transform.position))
                .Select(x => x.Target)
                .FirstOrDefault();

            return result;
        }

        private IChopperTargetable FindCenterOfMostTargets(IEnumerable<TargetSquare> items)
        {
            if (items.Count() == 1)
            {
                return items.First().Target;
            }

            Vector3 centroid = Vector3.zero;
            foreach (var item in items)
            {
                centroid += item.Target.GetGameObject.transform.position;
            }

            centroid /= items.Count();

            var result = items
                .OrderBy(x => Vector3.Distance(centroid, x.Target.GetGameObject.transform.position))
                .Select(x => x.Target)
                .FirstOrDefault();

            return result;
        }

        private IChopperTargetable FindSquareWithMostInRow(IEnumerable<IChopperTargetable> items)
        {
            var squaresByRow = items
                .GroupBy(i => i.GetGameObject.GetComponent<Rectangle>().row)
                .Select(g => new { Row = g.Key, Count = g.Count(), Items = g.ToList() })
                .OrderByDescending(g => g.Count)
                .FirstOrDefault();
            return squaresByRow?.Items
                .OrderBy(i => Vector3.Distance(transform.position, i.GetGameObject.transform.position))
                .FirstOrDefault();
        }

        private IChopperTargetable FindSquareWithMostInColumn(IEnumerable<IChopperTargetable> items)
        {
            var squaresByColumn = items
                .GroupBy(i => i.GetGameObject.GetComponent<Rectangle>().col)
                .Select(g => new { Col = g.Key, Count = g.Count(), Items = g.ToList() })
                .OrderByDescending(g => g.Count)
                .FirstOrDefault();
            return squaresByColumn?.Items
                .OrderBy(i => Vector3.Distance(transform.position, i.GetGameObject.transform.position))
                .FirstOrDefault();
        }

        private IChopperTargetable FindCenterOfMostTargets(IEnumerable<IChopperTargetable> items)
        {
            if (items.Count() == 1) return items.First();

            Vector3 centroid = Vector3.zero;
            foreach (var item in items)
            {
                centroid += item.GetGameObject.transform.position;
            }

            centroid /= items.Count();

            return items
                .OrderBy(i => Vector3.Distance(centroid, i.GetGameObject.transform.position))
                .FirstOrDefault();
        }

        private bool IsValidTarget(IChopperTargetable target)
        {
            return target != null &&
                   (Object)target != _thisChopperItem &&
                   (target.GetChopperTarget == null || target.GetChopperTarget == gameObject);
        }

        private static bool ArgItems(Item i)
        {
            return i != null && i.Explodable && !i.needFall && !i.falling && !i.destroying && !i.JustCreatedItem;
        }

        public bool IsAnimationFinished()
        {
            return reachedTarget;
        }

        public int GetPriority()
        {
            return priority;
        }

        public bool CanBeStarted()
        {
            return canBeStarted;
        }
    }

    public static class ChopperUtils
    {
        public static IEnumerable<IChopperTargetable> ChopperCondition(this IEnumerable<IChopperTargetable> seq, GameObject gameObject, Item item, int ChopperIndex)
        {
            var notNullItems = seq.WhereNotNull();

            var filteredItems = notNullItems.Where(i =>
            {
                bool hasNoTargetOrMatchingTarget = i.GetChopperTarget == null || i.GetChopperTarget == gameObject;
                bool isNotCurrentItem = (Object)i != item;
                bool isNotDestroying = !i.GetItem?.destroying ?? true;

                bool result = hasNoTargetOrMatchingTarget && isNotCurrentItem && isNotDestroying;

                return result;
            });

            var finalCount = filteredItems.Count();
            return filteredItems;
        }
    }

    public static class Vector3Extensions
    {
        public static Vector3 Divide(this Vector3 a, Vector3 b)
        {
            return new Vector3(a.x / b.x, a.y / b.y, a.z / b.z);
        }
    }
}
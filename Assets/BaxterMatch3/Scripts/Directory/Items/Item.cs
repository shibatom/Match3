

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Playables;
using Random = UnityEngine.Random;
using System.Runtime.CompilerServices;
using DG.Tweening;
using Internal.Scripts.Blocks;
using Internal.Scripts.Effects;
using Internal.Scripts.GUI.Boost;
using Internal.Scripts.Items.Interfaces;
using Internal.Scripts.Level;
using Internal.Scripts.System;
using Internal.Scripts.System.Combiner;
using Internal.Scripts.System.Pool;
using Internal.Scripts.TargetScripts.TargetSystem;
using UnityEngine.Serialization;

//using UnityEngine.InputSystem.EnhancedTouch;

namespace Internal.Scripts.Items
{
    //types of items
    public enum ItemsTypes
    {
        NONE = 0,
        RocketVertical = 4,
        RocketHorizontal = 5,
        Bomb = 3,
        DiscoBall = 1,
        Gredient = 6,
        Eggs = 7,
        Chopper = 2,
        TimeBomb = 8,
        Pots = 9
    }

    /// <summary>
    /// Item class is control item behaviour like switching, animations, turning to another bonus item, destroying
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    [RequireComponent(typeof(Animator))]
    public class Item : ItemMonoBehaviour, ISquareItemCommon, IField, IColorChangable, IChopperTargetable,
        IDestroyPipelineStripedShow
    {
        public int instanceID;

        [Header("Item can MATCH with other items")]
        public bool Combinable;

        [Header("Item can MATCH with a bonus items")]
        public bool CombinableWithBonus;

        [Header("Item can be destroy by neighbour items")]
        public bool DestroyByNeighbour;

        [Header("Item score")] public int scoreForItem = 10;
        public SpawnObjCounter SpawnAmount;

        public bool Explodable
        {
            get
            {
                bool? v = GetTopItemInterface()?.IsExplodable();
                return v ?? false;
            }
            set => GetTopItemInterface()?.SetExplodable(value);
        }

        //Sprite Renderer Reference
        public SpriteRenderer[] SprRenderer
        {
            get
            {
                if (SpriteRenderers == null || SpriteRenderers.Length == 0)
                    SpriteRenderers = GetComponentsInChildren<SpriteRenderer>();


                return SpriteRenderers;
            }
        }

        private SpriteRenderer[] SpriteRenderers;

        //square object reference
        public Rectangle square;

        [HideInInspector] public Rectangle previousSquare;

        //should item fall after check
        public bool needFall;

        //should item freeze
        public bool dontFall;

        //current field reference
        [HideInInspector] public FieldBoard field;

        //is that item dragging
        [HideInInspector] public bool dragThis;

        //Not null if this items targeted by Chopper
        [HideInInspector] public GameObject ChopperTarget;
        [HideInInspector] public Vector3 mousePos;

        [HideInInspector] public Vector3 deltaPos;

        //direction of switching this item
        //[HideInInspector]
        public Vector3 switchDirection;

        public Vector3 mergeSwitchDirection;

        //neighborSquare for switching
        public Rectangle neighborSquare;

        //Item switching with
        private Item switchItem;

        //item is switching
        public bool isSwithing;

        //true if the item falling
        public bool falling;
        public bool skipFalling = false;

        //Next type which item is going to became
        private ItemsTypes nextType = ItemsTypes.NONE;

        //current item type
        public ItemsTypes currentType = ItemsTypes.NONE;

        private SquareBoundaryLine squareBoundaryLine;


        [HideInInspector] public ItemsTypes debugType = ItemsTypes.NONE;

        public ItemsTypes NextType
        {
            get { return nextType; }

            set
            {
                if (MainManager.Instance.DebugSettings.BonusCombinesShowLog)
                    DebugLogManager.Log("set next " + value + " " + GetInstanceID(),
                        DebugLogManager.LogType.BonusAppearance);
                nextType = value;
            }
        }

        //child transform with sprite
        [HideInInspector] public Transform itemAnimTransform;

        //order in the squares sequence
        [HideInInspector] public int orderInSequence;

        //restriction from getting from pool
        [HideInInspector] public bool canBePooled;
        [HideInInspector] public int COLORView;
        private int COLOR;

        public int color
        {
            get
            {
                if (colorableComponent != null) return colorableComponent.color;
                return GetHashCode();
            }
            set => colorableComponent?.SetColor(value);
        }

        //if true - item just created
        public bool justCreatedItem;

        public bool JustCreatedItem
        {
            get { return justCreatedItem; }

            set
            {
                if (value)
                    SprRenderer.WhereNotNull().ToList()
                        .ForEach(i => i.sortingLayerID = SortingLayer.NameToID("Item mask"));
                else
                    SprRenderer.WhereNotNull().ToList().ForEach(i => i.sortingLayerID = 0);

                justCreatedItem = value;
            }
        }

        public bool justIntItem;

        public bool JustIntItem
        {
            get { return justIntItem; }

            set { justIntItem = value; }
        }

        //animator component
        [HideInInspector] public Animator anim;

        // public SkeletonAnimation skeletonAnimation;
        //gonna be destroy
        public bool destroying;

        //animation interface purpose
        [HideInInspector] public bool animationFinished;

        //animation value
        private float xScale;

        //animation value
        private float yScale;

        //item was set by editor
        [HideInInspector] public bool tutorialItem;

        //true - item should be switched in interactive tutorial
        [HideInInspector] public bool tutorialUsableItem;

        //true - item destroyed by multicolor
        [HideInInspector] public bool globalExplEffect;

        /// The destroy in the next destroying iteration
        internal bool destroyNext;

        //dont destroy this item, i.e. just appeared bonus item
        [HideInInspector] public bool dontDestroyOnThisMove;

        //editor item link
        [HideInInspector] public EditorItems itemForEditor;

        /// set this item Undestroyable for current combine
        public bool dontDestroyForThisCombine;

        /// playable director for package animation
        [HideInInspector] public PlayableDirector director;

        public GameObject plusTime;
        public GameObject plusTimeObj;
        private TrailRendererController trailRendererController;

        // Assign the trail material in the Inspector
        public Material trailMaterial;

        private void OnEnable()
        {
            switch (currentType)
            {
                case ItemsTypes.Gredient:
                    anim.SetBool("ingredient_idle", true);
                    anim.SetFloat("ingredient_offset", Random.Range(0.0f, 1.0f));
                    break;
                case ItemsTypes.Bomb:
                    anim.SetBool("package_idle", true);
                    break;
            }

            InitItem();
        }

        //init variable after getting from pool
        public virtual void InitItem()
        {
            if (!gameObject.activeSelf)
                return;
            anim?.Rebind();
            if (anim?.runtimeAnimatorController != null)
                anim?.ResetTrigger("disappear");
            destroycoroutineStarted = false;
            var animController = anim.runtimeAnimatorController;
            bool animoffset = false || transform.Find("Sprite").transform.localPosition != Vector3.zero;
#if UNITY_2019_4_OR_NEWER
            if (animoffset)
                anim.runtimeAnimatorController = null;
#endif
            anim.enabled = false;
            if (transform.childCount > 0)
            {
                transform.GetChild(0).transform.localScale = Vector3.one;
                transform.GetChild(0).transform.localPosition = Vector3.zero;
            }

            //        if(sprRenderer.FirstOrDefault())
            //            sprRenderer.FirstOrDefault().sortingOrder = 2;
            globalExplEffect = false;
            if (currentType != ItemsTypes.NONE)
                StartCoroutine(SetJustCreated());
            StartCoroutine(RareUpdate());
            GetComponentsInChildren<SpriteRenderer>().ForEachY(i => i.enabled = true);
            destroying = false;
            falling = false;
            destroyNext = false;
            tutorialItem = false;
            previousSquare = null;
            switchDirection = Vector3.zero;
            dragThis = false;
            fallingID = 0;
            debugType = currentType;
            ResetAnimTransform();
            anim.enabled = true;
            anim.runtimeAnimatorController = animController;
            ChopperTarget = null;
            combinationID = -1;
            if (animoffset)
            {
                anim.enabled = false;
                Invoke("AnimAwake", .1f);
            }
        }

        void AnimAwake()
        {
            anim.enabled = true;
            InitItem();
        }

        public void CheckPlusFive()
        {
            var range = Random.Range(0, 30);
            if (MainManager.Instance.gameStatus == GameState.PrepareGame) range = Random.Range(0, 5);
            if (currentType == ItemsTypes.NONE && MainManager.Instance.levelData.limitType == LIMIT.TIME)
            {
                if (range == 1 &&
                    square.field?.GetItems().Where(i => i.GetComponentInChildren<BonusFive>() != null).Count() <
                    SpawnAmount.SpawnAmount)
                {
                    plusTimeObj = Instantiate(plusTime, transform.position, Quaternion.identity, transform);
                    BindSortSetter bindSortSetter = plusTimeObj.transform.GetChild(0).GetComponent<BindSortSetter>();
                    bindSortSetter.sourceObject = GetSpriteRenderer();
                    bindSortSetter.destObjectSR = plusTimeObj.GetComponent<SpriteRenderer>();
                }
            }
        }

        private void Awake()
        {
            colorableComponent = GetComponent<ColorReciever>();
            director = GetComponent<PlayableDirector>();
            anim = GetComponent<Animator>();
            instanceID = GetInstanceID();
            name = "item " + currentType + instanceID;
            itemAnimTransform = transform.childCount > 0 ? transform.GetChild(0) : transform;
            defaultTransformPosition = itemAnimTransform.localPosition;
            defaultTransformScale = itemAnimTransform.localScale;
            defaultTransformRotation = itemAnimTransform.localRotation;
        }

        // Use this for initialization
        protected override void Start()
        {
            if (NextType != ItemsTypes.NONE)
            {
                transform.position = square.transform.position;
                falling = false;
            }

            xScale = transform.localScale.x;
            yScale = transform.localScale.y;
            base.Start();
        }

        /// Generate color for this Item
        /// <param name="colorGettable"></param>
        public void GenColor(IColorGettable colorGettable)
        {
            GenChopper();
            if (colorableComponent != null) colorableComponent.RandomizeColor(colorGettable);
            MainManager.Instance.levelData.SetItemTarget(this);
        }

        private void GenChopper()
        {
            if (MainManager.Instance.enableChopper && Random.Range(0, 10) == 0 &&
                MainManager.Instance.gameStatus == GameState.Playing && currentType == ItemsTypes.NONE && !tutorialItem)
            {
                var ChopperCount = MainManager.Instance.field.GetItems()
                    ?.Where(i => i.currentType == ItemsTypes.Chopper)
                    ?.Count() ?? 0;
            }
        }

        //animation event "Appear"
        public void SetAppeared()
        {
            if (currentType == ItemsTypes.Bomb || currentType == ItemsTypes.DiscoBall ||
                currentType == ItemsTypes.RocketVertical || currentType == ItemsTypes.RocketHorizontal ||
                currentType == ItemsTypes.Chopper)
                anim.SetBool("package_idle", true);
        }

        //animation event "Disappear"
        public void SetDissapeared()
        {
            //SmoothDestroy();
        }

        //start idle animation
        private IEnumerator AnimIdleStart()
        {
            var xScaleDest1 = xScale - 0.05f;
            var xScaleDest2 = xScale;
            var speed = Random.Range(0.02f, 0.07f);

            var trigger = false;
            while (true)
            {
                if (!trigger)
                {
                    if (xScale > xScaleDest1)
                    {
                        xScale -= Time.deltaTime * speed;
                        yScale += Time.deltaTime * speed;
                    }
                    else
                        trigger = true;
                }
                else
                {
                    if (xScale < xScaleDest2)
                    {
                        xScale += Time.deltaTime * speed;
                        yScale -= Time.deltaTime * speed;
                    }
                    else
                        trigger = false;
                }

                transform.localScale = new Vector3(xScale, yScale, 1);
                yield return new WaitForFixedUpdate();
            }
        }

        //reset drag variables
        private void ResetDrag()
        {
            Debug.Log("Item: sallog private void ResetDrag()");
            dragThis = false;
            usedItem = null;
            transform.position = square.transform.position + Vector3.back * 0.2f;
            switchDirection = Vector3.zero;
            if (switchItem != null && neighborSquare != null)
            {
                switchItem.transform.position = neighborSquare.transform.position + Vector3.back * 0.2f;
                neighborSquare = null;
            }

            switchItem = null;
            MainManager.Instance.DragBlocked = false;
        }

        //check destroying, changing type, falling and switching details
        private void Update()
        {
            COLORView = color;
            if (currentType != debugType && currentType != ItemsTypes.Gredient && NextType == ItemsTypes.NONE)
            {
                NextType = debugType;

                ChangeType();
                DestroyItem(destroyNeighbours: true);
            }

            if (dragThis && !MainManager.Instance.findMatchesStarted /*&& !usedItem*/)
            {
                if (JustIntItem || MainManager.Instance.Falling || MainManager.Instance.Limit == 0)
                {
                    Debug.Log("prevented from " + JustIntItem + MainManager.Instance.Falling);
                    return;
                }

                ;
                usedItem = this;
                deltaPos = ManageInput.Instance.GetMouseDelta();
                if (switchDirection == Vector3.zero)
                {
                    SwitchDirection(deltaPos);
                }
            }

            if (!falling && square != null && field != null &&
                MainManager.Instance.gameStatus != GameState.RegenLevel && field == MainManager.Instance.field)
                CheckSquareBelow();
        }

        IEnumerator RareUpdate()
        {
            while (true)
            {
                if (dontDestroyForThisCombine /*&& (lastCombine?.items.Where(i=>i != this).AllNull() ?? true)*/)
                {
                    yield return new WaitForSeconds(0.5f);

                    dontDestroyForThisCombine = false;
                }

                yield return new WaitForSeconds(0.3f);
            }
        }

        IEnumerator SetJustCreated()
        {
            JustIntItem = true;
            //Debug.Log($"JustIntItem {JustIntItem}");
            yield return new WaitForSeconds(1f);
            JustIntItem = false;
            //Debug.Log($"JustIntItem {JustIntItem}");
        }

        //Switching start method
        public void SwitchDirection(Vector3 delta)
        {
            if (MainManager.Instance.Falling)
                return;
            MainManager.Instance.StopedAI();
            deltaPos = delta;
            if (Vector3.Magnitude(deltaPos) <= 0.1f)
            {
                usedItem = null;
                return;
            }

            InitiateSwitchMovement();
            DetermineSwipeDirection();
            FindNeighborSquare();
            HandleSwitchWithNeighbor();
        }

        private void InitiateSwitchMovement()
        {
            MainManager.Instance.DragBlocked = true;
            switchItem = null;
        }

        private void DetermineSwipeDirection()
        {
            bool isHorizontalSwipe = Mathf.Abs(deltaPos.x) > Mathf.Abs(deltaPos.y);

            if (isHorizontalSwipe)
            {
                switchDirection.x = deltaPos.x > 0 ? 1 : -1;
                switchDirection.y = 0;
            }
            else
            {
                switchDirection.x = 0;
                switchDirection.y = deltaPos.y > 0 ? 1 : -1;
            }
        }

        private void FindNeighborSquare()
        {
            neighborSquare = switchDirection.x > 0 ? square.GetNeighborLeft() :
                switchDirection.x < 0 ? square.GetNeighborRight() :
                switchDirection.y > 0 ? square.GetNeighborBottom() :
                switchDirection.y < 0 ? square.GetNeighborTop() : null;

            if (neighborSquare != null)
            {
                switchItem = neighborSquare.Item;
            }
        }

        private void HandleSwitchWithNeighbor()
        {
            if (switchItem == null)
            {
                ResetDrag();
                return;
            }

            bool canSwitch = CanItemsSwitch();

            if (canSwitch)
            {
                MainManager.Instance.StartCoroutine(Switching());
            }
            else
            {
                ResetDrag();
            }
        }

        private bool CanItemsSwitch()
        {
            // Check if neighbor square can allow items to move out
            if (switchItem.square.GetSubSquare().CanGoOut())
            {
                return true;
            }

            // Check if either item is a special type (not NONE or INGREDIENT)
            bool hasSpecialItem = (currentType != ItemsTypes.NONE || switchItem.currentType != ItemsTypes.NONE) &&
                                  (currentType != ItemsTypes.Gredient && switchItem.currentType != ItemsTypes.Gredient);

            return hasSpecialItem && switchItem.square.GetSubSquare().CanGoOut();
        }

        //switching animation and check rest conditions


        //switching animation and check rest conditions
        private IEnumerator Switching()
        {
            if (switchDirection != Vector3.zero && neighborSquare)
            {
                // Get or add the TrailRendererController component to the player
                trailRendererController = gameObject.AddComponent<TrailRendererController>();
                mergeSwitchDirection = switchDirection;
                // Initialize the TrailRenderer with the desired parameters
                // trailRendererController.Initialize(trailMaterial, 1f, 0.5f, 0.2f);
                Rectangle[] backupSquares = { square, neighborSquare };
                Item[] switchingItems = { this, switchItem };
                var backMove = false;

                if ((switchItem.currentType == ItemsTypes.Bomb && this.currentType == ItemsTypes.Bomb) ||
                    (switchItem.currentType == ItemsTypes.DiscoBall && this.currentType == ItemsTypes.DiscoBall))
                {
                    Debug.Log("sallog1");
                    neighborSquare.Item = this;
                    square.Item = switchItem;
                    switchItem.square = backupSquares[0];
                    square = backupSquares[1];
                    switchItem.SmoothDestroy();
                }
                else if (switchItem.currentType != ItemsTypes.DiscoBall && this.currentType != ItemsTypes.DiscoBall &&
                         !(switchItem.currentType == ItemsTypes.Bomb &&
                           this.currentType ==
                           ItemsTypes
                               .Bomb) /*&&!(switchItem.currentType == ItemsTypes.HORIZONTAL_STRIPED && this.currentType == ItemsTypes.PACKAGE)*/
                        )
                {
                    Debug.Log("sallog1");
                    neighborSquare.Item = this;
                    square.Item = switchItem;
                    switchItem.square = backupSquares[0];
                    square = backupSquares[1];
                }

                if (this.currentType == ItemsTypes.Chopper || switchItem.currentType == ItemsTypes.Chopper)
                {
                    // Get the item that has the Chopper type
                    Item chopperItem = this.currentType == ItemsTypes.Chopper ? this : switchItem;

                    // Get the other non-Chopper item
                    Item otherItem = this.currentType == ItemsTypes.Chopper ? switchItem : this;

                    if (otherItem.currentType != ItemsTypes.NONE)
                    {
                        if ((otherItem.currentType == ItemsTypes.Eggs || otherItem.currentType == ItemsTypes.Pots))
                        {
                        }
                        else
                        {
                            // Get Chopper mesh component for animation
                            var heliAnim = chopperItem.GetComponent<Item>().GetTopItemInterface().GetGameobject()
                                .GetComponent<ChopperItem>().HeliAnimator;
                            heliAnim.SetTrigger("StartSwitch");
                        }
                    }
                }

                if (this.currentType == ItemsTypes.DiscoBall || switchItem.currentType == ItemsTypes.DiscoBall)
                {
                    Debug.Log("swithing DiscoBall");
                    // Get the item that has the multicolor type
                    Item multicolorItem = this.currentType == ItemsTypes.DiscoBall ? this : switchItem;

                    // Get the other non-multicolor item
                    Item otherItem = this.currentType == ItemsTypes.DiscoBall ? switchItem : this;
                    Debug.LogError("swithing DiscoBall" + otherItem.currentType);
                    if (otherItem.currentType != ItemsTypes.Pots && otherItem.currentType != ItemsTypes.Eggs)
                        otherItem.SmoothDestroy();
                }

                // Generate the combines2 list with potential matches
                var combines2 = GetMatchesAround().Concat(backupSquares.First().Item.GetMatchesAround()).ToList();

                // Separate combines2 into distinct groups
                var matchGroups = new List<List<CombineClass>>();
                foreach (var combine in combines2)
                {
                    bool addedToGroup = false;
                    foreach (var group in matchGroups)
                    {
                        if (group.Any(existingCombine => existingCombine.items.Intersect(combine.items).Any()))
                        {
                            group.Add(combine);
                            addedToGroup = true;
                            break;
                        }
                    }

                    if (!addedToGroup)
                    {
                        matchGroups.Add(new List<CombineClass> { combine });
                    }
                }

                // The rest of the code stays the same, but now we process each group independently
                var startTime = Time.time;
                var startPos = transform.position;
                float speed = 15;
                float distCovered = 0;
                while (distCovered < 1)
                {
                    distCovered = (Time.time - startTime) * speed;

                    // Define the target position with a depth offset
                    Vector3 targetPosition = neighborSquare.transform.position + Vector3.back * 0.3f;

                    // Check if either item is DiscoBall, or if one is Chopper and the other is a Rocket or Bomb item,
                    // or if one item is a Bomb and the other is a Rocket item.
                    if (currentType == ItemsTypes.DiscoBall || switchItem.currentType == ItemsTypes.DiscoBall ||
                        (currentType == ItemsTypes.Chopper &&
                         (switchItem.currentType == ItemsTypes.RocketVertical ||
                          switchItem.currentType == ItemsTypes.RocketHorizontal ||
                          switchItem.currentType == ItemsTypes.Bomb)) ||
                        (switchItem.currentType == ItemsTypes.Chopper &&
                         (currentType == ItemsTypes.RocketVertical ||
                          currentType == ItemsTypes.RocketHorizontal ||
                          currentType == ItemsTypes.Bomb)) ||
                        (currentType == ItemsTypes.Bomb &&
                         (switchItem.currentType == ItemsTypes.RocketVertical ||
                          switchItem.currentType == ItemsTypes.RocketHorizontal)) ||
                        (switchItem.currentType == ItemsTypes.Bomb &&
                         (currentType == ItemsTypes.RocketVertical ||
                          currentType == ItemsTypes.RocketHorizontal)))
                    {
                        transform.position = Vector3.Lerp(startPos, targetPosition, distCovered);
                        //Debug.Log($"Item/Switching: Current Item if {this} Position: {transform.position}");
                    }

                    // New check for both items being PACKAGE type
                    else if (currentType == ItemsTypes.Bomb && switchItem.currentType == ItemsTypes.Bomb)
                    {
                        transform.position = Vector3.Lerp(startPos, targetPosition, distCovered);
                        //Debug.Log($"Item/Switching: Both items are PACKAGE type, Current Item {this} Position: {transform.position}");
                    }
                    // else if (currentType == ItemsTypes.PACKAGE && switchItem.currentType == ItemsTypes.HORIZONTAL_STRIPED)
                    // {
                    //    // transform.position = Vector3.Lerp(startPos, targetPosition, distCovered);
                    //     //Debug.Log($"Item/Switching: Both items are PACKAGE Horizontal type, Current Item {this} Position: {transform.position}");
                    // }
                    else
                    {
                        // Default behavior: animate both the current item and switch item
                        transform.position = Vector3.Lerp(startPos, targetPosition, distCovered);
                        //Debug.Log($"Item/Switching: Current Item {this} Position: {transform.position}");

                        switchItem.transform.position = Vector3.Lerp(targetPosition + Vector3.forward * 0.1f, startPos,
                            distCovered);
                        //Debug.Log($"Item/Switching: Switch Item {switchItem} Position: {switchItem.transform.position}");
                    }


                    yield return new WaitForEndOfFrame();
                }


                var list = new[] { this, switchItem };

                // Validate the move
                if (!matchGroups.Any() && !IsSwitchBonus() &&
                    MainManager.Instance.ActivatedBoost != BoostType.FreeMove &&
                    NotContainsBoth(ItemsTypes.DiscoBall, ItemsTypes.DiscoBall) ||
                    ContainsBoth(ItemsTypes.DiscoBall, ItemsTypes.Gredient) ||
                    ContainsBoth(ItemsTypes.DiscoBall, ItemsTypes.Eggs) ||
                    ContainsBoth(ItemsTypes.DiscoBall, ItemsTypes.Pots) ||
                    (ContainsBoth(ItemsTypes.TimeBomb, ItemsTypes.TimeBomb) && !matchGroups.Any()))

                {
                    // Handle invalid move by reversing the swap
                    square = backupSquares[0];
                    switchItem.square = backupSquares[1];
                    neighborSquare.Item = switchItem;
                    square.Item = this;
                    backMove = true;
                    CentralSoundManager.Instance?.PlayOneShot(CentralSoundManager.Instance.wrongMatch);
                }
                else
                {
                    // Handle valid move and adjust game state
                    if (MainManager.Instance.ActivatedBoost != BoostType.FreeMove)
                    {
                        if (MainManager.Instance.levelData.limitType == LIMIT.MOVES)
                            MainManager.Instance.ChangeCounter(-1);
                        MainManager.Instance.moveID++;
                    }

                    if (MainManager.Instance.ActivatedBoost == BoostType.FreeMove)
                        MainManager.Instance.ActivatedBoost = BoostType.Empty;

                    MainManager.Instance.lastDraggedItem = this;
                    MainManager.Instance.lastSwitchedItem = switchItem;
                }

                // Process each match group separately
                if (!backMove)
                {
                    BonusesAnimation(this, switchItem);
                    yield return new WaitWhile(() => MainManager.Instance.StopFall);
                    Check(this, switchItem);

                    foreach (var group in matchGroups)
                    {
                        foreach (var combine in group)
                        {
                            if (combine.nextType != ItemsTypes.NONE)
                            {
                                if (combine.color == MainManager.Instance.lastDraggedItem.color &&
                                    MainManager.Instance.lastDraggedItem.currentType == ItemsTypes.NONE)
                                    MainManager.Instance.lastDraggedItem.NextType = combine.nextType;
                                else if (combine.color == MainManager.Instance.lastSwitchedItem.color)
                                    MainManager.Instance.lastSwitchedItem.NextType = combine.nextType;
                            }
                        }

                        var destroyItems = group.SelectMany(i => i.items).ToList();
                        //LevelManager.THIS.FindMatches();
                        yield return new WaitUntilPipelineIsDestroyed(destroyItems,
                            new Delays { after = new CustomWaitForSecond() });
                    }

                    Debug.Log("Item: Checking if all items meet conditions...");
                    foreach (Item item in list)
                    {
                        Debug.Log("item name: " + item.name + ", CurrentType: " + item);
                    }

                    if (!list.All(i => i.currentType != ItemsTypes.NONE /*&& i.CombinableWithBonus*/))
                    {
                        var filteredItems = list.Where(i =>
                            i.currentType != ItemsTypes.NONE &&
                            (i.currentType != ItemsTypes.Eggs || i.currentType != ItemsTypes.Pots)).ToList();

                        // Check if there are any items that meet the condition
                        if (filteredItems.Any())
                        {
                            // Perform actions on each item in the filtered list
                            foreach (var item in filteredItems)
                            {
                                // Perform action on item
                                //Debug.Log($"Item: Performing action on item with currentType: {item.currentType}");
                                item.DestroyItem(true, destroyNeighbours: true);
                                // Additional actions...
                            }
                        }
                        else
                        {
                            // Log a message indicating that there are no items with currentType other than ItemsTypes.NONE
                            Debug.Log("Item: There are no items with currentType other than ItemsTypes.NONE.");
                        }

                        Debug.Log("Item: At least one item does not meet conditions. Performing actions...");
                        // var destroyItems = combines2.SelectMany(i => i.items).ToList();
                        //LevelManager.THIS.FindMatches();
                        Debug.Log("Item: Waiting for items to be destroyed...");
                        //  yield return new WaitWhileDestroyPipeline(destroyItems, new Delays { after = new WaitForSecCustom() });
                    }
                    else
                    {
                        Debug.Log("Item: All items meet conditions. No action required.");
                    }


                    MainManager.Instance.levelData.GetTargetsByAction(CollectingTypes.ReachBottom)
                        .ForEachY(i => i?.CheckBottom());
                    //  CheckAndChangeTypes();
                }

                // Reverse the swap animation if backMove is true
                startTime = Time.time;
                distCovered = 0;
                while (distCovered < 1 && backMove)
                {
                    distCovered = (Time.time - startTime) * speed;
                    transform.position = Vector3.Lerp(neighborSquare.transform.position + Vector3.back * 0.3f, startPos,
                        distCovered);
                    switchItem.transform.position = Vector3.Lerp(startPos,
                        neighborSquare.transform.position + Vector3.back * 0.2f, distCovered);
                    yield return new WaitForEndOfFrame();
                }

                if (backMove)
                    ResetDrag();

                trailRendererController.DestroyTrailAndController();
            }

            if (switchDirection == Vector3.zero && !neighborSquare)
                ResetDrag();
        }

        private bool NotContainsBoth(ItemsTypes type1, ItemsTypes type2) =>
            (currentType != type1 && switchItem.currentType != type2);

        private bool ContainsBoth(ItemsTypes type1, ItemsTypes type2) =>
            currentType == type1 && switchItem.currentType == type2 ||
            currentType == type2 && switchItem.currentType == type1;

        /// Cloud effect animation for different direction levels
        public IEnumerator DirectionAnimation(Action callback)
        {
            GameObject itemPrefabObject = gameObject;

            var duration = 0.5f;
            Vector2 destPos = itemPrefabObject.transform.localPosition + (Vector3)square.direction * 0.1f;
            var startPos = itemPrefabObject.transform.localPosition;
            var curveX = new AnimationCurve(new Keyframe(0, startPos.x), new Keyframe(duration / 2, destPos.x));
            var curveY = new AnimationCurve(new Keyframe(0, startPos.y), new Keyframe(duration / 2, destPos.y));
            curveX.postWrapMode = WrapMode.PingPong;
            curveY.postWrapMode = WrapMode.PingPong;

            var startTime = Time.time;
            float distCovered = 0;
            while (distCovered < duration)
            {
                if (itemPrefabObject == null || !square || falling || needFall || destroying || destroyNext ||
                    MainManager.Instance.DragBlocked)
                {
                    itemPrefabObject.transform.localPosition = startPos;
                    callback();
                    yield break;
                }

                if (distCovered > duration / 10)
                    callback();
                distCovered = (Time.time - startTime);
                if (itemPrefabObject)
                    itemPrefabObject.transform.localPosition =
                        new Vector3(curveX.Evaluate(distCovered), curveY.Evaluate(distCovered), 0);
                else
                    yield break;
                yield return new WaitForFixedUpdate();
                //            if (switchDirection != Vector3.zero)
                //            {
                //                itemPrefabObject.transform.localPosition = Vector3.zero;
                //                yield break;
                //            }
            }
        }

        //Change type if necessary
        public void CheckAndChangeTypes()
        {
            var itemsTypeChange = field.GetItems();
            var listChangingType = itemsTypeChange.Where(i => i.NextType != ItemsTypes.NONE);
            if (MainManager.Instance.gameStatus == GameState.Playing ||
                MainManager.Instance.gameStatus == GameState.PreWinAnimations)
            {
                foreach (var item in listChangingType)
                {
                    item.ChangeType();
                }
            }
        }

        //virtual method for bonus items
        public virtual void Check(Item item1, Item item2)
        {
        }

        //bonus animation after switching
        public void BonusesAnimation(Item item1, Item item2)
        {
            Debug.Log("Item: sallog BonusesAnimation ITEM1: " + item1.debugType + "  sallog BonusesAnimation ITEM2: " +
                      item2.debugType);
            var list = new[] { item1, item2 };
            // if(list.Any(i=>!i.Combinable || !i.CombinableWithBonus)) return;
            bool isMulticolor = list.Any(i => i.currentType == ItemsTypes.DiscoBall);
            if (item1.currentType != ItemsTypes.NONE && item2.currentType != ItemsTypes.NONE || isMulticolor)
            {
                // if (!isMulticolor)
                //     gameObject.AddComponent<GameBlocker>();
                Vector2 middlePos = item2.transform.position +
                                    (item1.transform.position - item2.transform.position).normalized * 0.5f;
                list = list.OrderBy(i => i.GetComponent<ItemCombinations>()?.priority ?? 100).ToArray();
                item1 = list.First();
                item2 = list.Last();
                item1.SprRenderer.FirstOrDefault().sortingOrder = 3;
            }
        }

        /// <summary>
        /// Get matches around this item, local check.
        /// </summary>
        /// <returns>List of combines around this item.</returns>
        public List<CombineClass> GetMatchesAround()
        {
            // Ensure square is not null before proceeding
            if (square == null)
            {
                Debug.LogWarning("Item/GetMatchAround: Square is null, cannot find matches.");
                return new List<CombineClass>();
            }

            // Find matches around the current item's square
            var list = square.FindMatchesAround();
            Debug.Log("Item/GetMatchAround: Found matches around the square.");

            // Ensure the list is not null before converting to combine
            if (list == null)
            {
                Debug.LogWarning("Item/GetMatchAround: Match list is null, cannot convert to combine.");
                return new List<CombineClass>();
            }

            // Convert the list of matches to a combine object
            var combine = new CombineClass().ConvertToCombine(list);
            Debug.Log("Item/GetMatchAround: Converted matches to combine.");

            // Ensure LevelManager and CombineManager are not null
            if (MainManager.Instance == null || MainManager.Instance.CombineManager == null)
            {
                Debug.LogWarning("Item/GetMatchAround: LevelManager or CombineManager is null, cannot check combines.");
                return new List<CombineClass>();
            }

            // Get the combine manager from the level manager
            var combineManager = MainManager.Instance.CombineManager;
            Debug.Log("Item/GetMatchAround: Retrieved combine manager.");

            // Create a dictionary to store items and their corresponding combines
            var dic = new Dictionary<Item, CombineClass>();
            foreach (var item in combine.items)
            {
                if (item != null)
                {
                    dic.Add(item, combine);
                    Debug.Log("Item/GetMatchAround: Added item to dictionary: " + item);
                }
                else
                {
                    Debug.LogWarning("Item/GetMatchAround: Found null item in combine items.");
                }
            }

            // Check the combines using the combine manager
            var combines2 = combineManager.CheckCombines(dic, new List<CombineClass> { combine });
            Debug.Log("Item/GetMatchAround: Checked combines, found " + combines2.Count + " new combines.");

            // Ensure LevelManager.THIS.combo is not null before updating
            if (MainManager.Instance != null)
            {
                MainManager.Instance.combo += combines2.Count;
                Debug.Log("Item/GetMatchAround: Updated combo count to " + MainManager.Instance.combo);
            }
            else
            {
                Debug.LogWarning("Item/GetMatchAround: LevelManager.THIS is null, cannot update combo count.");
            }

            // Return the list of new combines
            return combines2;
        }


        ///is switching item is bonus
        private bool IsSwitchBonus()
        {
            bool switchBonus = false;

            // Check if either current or switch item is spiral type - return false
            if (currentType == ItemsTypes.Eggs || switchItem.currentType == ItemsTypes.Eggs)
                return false;
            if (currentType == ItemsTypes.Pots || switchItem.currentType == ItemsTypes.Pots)
                return false;

            if (currentType > 0 || switchItem.currentType > 0)
                Debug.Log("Item: Condition IsSwitchBonus 2: " + (switchBonus = true));
            else
                Debug.Log("Item: Condition IsSwitchBonus 2: " + switchBonus);

            return switchBonus;
        }


        /// <summary>
        /// Get main item of the current hierarchy
        /// </summary>
        /// <returns></returns>
        public IItemInterface GetTopItemInterface()
        {
            return GetComponent<IItemInterface>();
        }

        /// <summary>
        /// Get item interface
        /// </summary>
        /// <returns></returns>
        public IItemInterface[] GetItemInterfaces()
        {
            var items = transform.GetComponentsInChildren<IItemInterface>();
            return items;
        }

        /// <summary>
        /// change square link and start fall
        /// </summary>
        /// <param name="rectangle"></param>
        public void ReplaceCurrentSquareToFalling(Rectangle rectangle)
        {
            Debug.Log(instanceID + $"Item: {this} replace square from " + this.square.GetPosition() + " to " +
                      rectangle.GetPosition());
            rectangle.Item = this;
            this.square.Item = null;
            previousSquare = this.square;
            this.square = rectangle;
            needFall = true;
            if (!justCreatedItem || currentType != ItemsTypes.NONE)
                StartFalling();
        }


        /// <summary>
        /// checking square below and start fall if square is empty
        /// </summary>
        public void CheckSquareBelow()
        {
            // Check for straight fall only if not already falling/needing to fall
            if (!falling && !needFall && !dontFall && square != null)
            {
                var checkFallOut = square.CheckFallOut();
                if (checkFallOut != null)
                    StartCoroutine(checkFallOut); // This coroutine should set needFall = true if a fall is needed below
            }

            // Check for diagonal fall only if not already falling AND not needing to fall straight down.
            // This prevents trying to side-fall if a straight fall is already pending.
            if (!falling && !needFall)
            {
                CheckNearEmptySquares(); // This might set needFall = true and start StartFallingSides
            }
        }

        /// <summary>
        /// start falling animation
        /// </summary>
        public void StartFalling()
        {
            StartFallingTo(GenerateWaypoints(square));
        }

        WaitForSeconds staggerDelay = new(0.001f);

        public void StartFallingTo(List<WayMakerPoint> generateWaypoints)
        {
            //        if (LevelManager.THIS.StopFall) return;
            if (!falling && needFall && fallingID == 0)
            {
                if (MainManager.Instance.DebugSettings.FallingLog)
                    DebugLogManager.Log(name + "Item:  start fall, target - " + square,
                        DebugLogManager.LogType.Falling);
                falling = true;
                StartCoroutine(FallingCor(generateWaypoints, true, () =>
                {
                    // ProcessMatches();
                    //  falling = false;
                    if (MainManager.Instance.DebugSettings.FallingLog)
                        DebugLogManager.Log(name + "Item:  end fall, target - " + square,
                            DebugLogManager.LogType.Falling);
                }));
                // StartCoroutine(RevisedFallingCor(generateWaypoints, true));
                //StartCoroutine(newFallingCor(generateWaypoints, true));
            }
        }

        /// <summary>
        /// start falling diagonally
        /// </summary>
        private void StartFallingSides()
        {
            if (falling || !needFall || destroying || fallingID > 0) return;
            var waypoints = new List<WayMakerPoint>
            {
                new WayMakerPoint(transform.position, null),
                new WayMakerPoint(square.transform.position, null)
            };
            if (MainManager.Instance.DebugSettings.FallingLog)
                DebugLogManager.Log(name + "Item:  start side fall, target - " + square,
                    DebugLogManager.LogType.Falling);
            falling = true;

            StartCoroutine(FallingCor(waypoints, false, () =>
            {
                // ProcessMatches();
                // falling = false;
                if (MainManager.Instance.DebugSettings.FallingLog)
                    DebugLogManager.Log(name + "Item:  end side fall, target - " + square,
                        DebugLogManager.LogType.Falling);
            }));
        }

        public List<WayMakerPoint> GenerateWaypoints(Rectangle targetRectangle)
        {
            if (skipFalling)
            {
                //Debug.Log($"GenerateWaypoints: Skipping item {this.name} from waypoint generation.");
                return new List<WayMakerPoint>(); // Return empty waypoints if the item is marked to skip
            }

            var waypoints =
                MainManager.Instance.field.GetWaypoints(previousSquare, targetRectangle, new List<Rectangle>());
            if (waypoints.Any()) return waypoints;

            if (!targetRectangle.isEnterPoint)
                waypoints = MainManager.Instance.field.GetWaypoints(targetRectangle.enterRectangle, targetRectangle,
                    new List<Rectangle>());

            if (waypoints.Any()) return waypoints;

            waypoints.Add(new WayMakerPoint(targetRectangle.transform.position + Vector3.back * 0.2f, null));
            waypoints.Add(new WayMakerPoint(targetRectangle.transform.position + Vector3.back * 0.2f, null));
            return waypoints;
        }


        //show teleportation effect
        private void TeleportationEffect(Rectangle rectangle)
        {
            //        _square.teleport.EnableMask(false);
            rectangle.teleport.StartTeleport(this, null);
        }

        public int fallingID;
        private Vector3 defaultTransformPosition;
        private Vector3 defaultTransformScale;
        private Quaternion defaultTransformRotation;
        [HideInInspector] public ColorReciever colorableComponent;

        ///falling item animation
        /// Coroutine for handling the falling item animation with bounce and wave effect
        private IEnumerator FallingCor(List<WayMakerPoint> waypoints, bool animate, Action callback = null)
        {
            //Debug.Log($"FallingCor: {this.GetHashCode()} Coroutine started.");

            // Initialize falling process
            if (InitializeFalling())
                yield break; // Exit if already falling
            // If item is marked to skip falling, exit early
            if (skipFalling)
            {
                // Report stop ONLY if we successfully initia
                FinalizeImmediately();
                yield break;
            }

            // Wait until falling is not stopped
            yield return new WaitWhile(() => MainManager.Instance.StopFall);
            //Debug.Log($"FallingCor: {this.GetHashCode()} Stopped falling, continuing.");

            // Track global start time
            float startTimeGlobal = Time.time;

            // Process each waypoint in the list
            for (int i = 1; i < waypoints.Count; i++)
            {
                yield return ProcessWaypoint(waypoints, i, animate, startTimeGlobal);
            }

            // Finalize the falling process
            StartCoroutine(HandlePostFall(animate, callback));
        }

        /// Initializes the falling process and checks for overlapping falls.
        /// Returns true if the coroutine should exit early.
        private bool InitializeFalling()
        {
            if (fallingID > 0)
            {
                //Debug.Log($"FallingCor: {this.GetHashCode()} Already falling, exiting coroutine.");
                return true; // Exit if already falling
            }

            // LevelManager.THIS.ItemStartsMatch();
            anim.StopPlayback();
            fallingID++; // Set falling ID to avoid overlapping falls
            //Debug.Log($"FallingCor: {this.GetHashCode()} fallingID set to {fallingID}.");

            MainManager.Instance.FindMatches(); // Find and mark matches before falling
            //Debug.Log($"FallingCor: {this.GetHashCode()} Matches found and marked.");

            falling = true;
            needFall = false;

            return false; // Initialization successful
        }

        /// Processes a single waypoint, moving the item towards the destination.
        /// Includes handling of teleportation and potential pauses.
        private IEnumerator ProcessWaypoint(List<WayMakerPoint> waypoints, int i, bool animate, float startTimeGlobal)
        {
            //Debug.Log($"FallingCor: {this.GetHashCode()} Processing waypoint {i}.");
            WayMakerPoint wayMakerPoint = waypoints[i];

            Vector3 startPos = transform.position;
            Vector3 destPos = CalculateDestination(wayMakerPoint);
            float distance = CalculateDistance(startPos, destPos);
            // Apply teleportation effects if applicable
            newApplyTeleportationEffects(wayMakerPoint);

            // if (IsDistanceTooSmall(distance)) yield break; // Skip if distance is too small

            yield return StartMovingTowardsDestination(waypoints, i, wayMakerPoint, startPos, destPos, startTimeGlobal);
        }

        private IEnumerator StartMovingTowardsDestination(List<WayMakerPoint> waypoints, int i,
            WayMakerPoint wayMakerPoint, Vector3 startPos, Vector3 destPos, float startTimeGlobal)
        {
            float startTime = Time.time;
            float pauseTime = Time.time;
            float totalPauseTime = 0.0f;
            float fracJourney = 0.01f; // Small non-zero value to avoid abrupt stop
            // Fraction of journey completed

            // //Debug.Log($"FallingCor: Item: {this.GetHashCode()} Start Moving. Initial fracJourney={fracJourney}, startPos={startPos}, destPos={destPos}");

            while (fracJourney < 1f)
            {
                // Log at the start of the frame
                ////Debug.Log($"FallingCor: Item: {this.GetHashCode()} Loop Start: fracJourney={fracJourney}, totalPauseTime={totalPauseTime}, startTime={startTime}, startTimeGlobal={startTimeGlobal}");

                // bool fallStopped = HandleFallingPause(ref pauseTime);

                // yield return WaitForFallingPause();

                // if (fallStopped)
                // {
                //     totalPauseTime += Time.time - pauseTime; // Update total pause time
                //     UpdateStartTime(ref startTime, ref startTimeGlobal, totalPauseTime);
                //     ////Debug.Log($"FallingCor: Item: {this.GetHashCode()} Fall Resumed: totalPauseTime={totalPauseTime}, startTime={startTime}, startTimeGlobal={startTimeGlobal}");
                // }

                float speed = CalculateSpeed(i, startTimeGlobal);
                //  speed = 10;
                // //Debug.Log($"FallingCor: Item: {this.GetHashCode()} Speed Calculated: speed={speed}");

                // Modified call inside the while loop:
                fracJourney = MoveTowardsDestination_Simplified(startPos, destPos, speed, ref startTime);
// (No need to Clamp here as the simplified function already returns a clamped value 0-1)

                // //Debug.Log($"FallingCor: Item: {this.GetHashCode()} Position Updated: fracJourney={fracJourney}, position={transform.position}");

                // // Check if fracJourney exceeds bounds
                // if (fracJourney > 1f)
                // {
                //     //Debug.LogWarning($"FallingCor: Item: {this.GetHashCode()} Warning: fracJourney={fracJourney} exceeded bounds. Clamping to 1.");
                //     fracJourney = 1f;
                // }

                //     HandleCollisionAndMovement(ref startPos, ref startTimeGlobal, ref startTime, ref fracJourney, waypoint);
                //     RaycastHit2D hit2D = DetectCollisionTowards(destPos, startPos);

                // // Decide how to move based on collision
                // if (CanMoveWithoutStopping(hit2D, startPos, destPos))
                // {
                //     // Move towards the destination
                //     fracJourney = UpdatePositionOverTime_Simplified(startPos, destPos, speed, startTime);
                //     Debug.Log($"[MoveTowardsDestination] Moving without stopping. fracJourney={fracJourney}, startPos={startPos}, destPos={destPos}, speed={speed}");
                // }
                // else if (newShouldSkipItem(hit2D))
                // {
                //     // Reset times if skipping item to attempt a move again
                //     newSkipItem(ref startPos, ref startTimeGlobal, ref startTime);
                //     fracJourney = 0f; // Restart journey fraction
                //     Debug.Log($"[MoveTowardsDestination] Skipping item. Resetting journey. startPos={startPos}, startTimeGlobal={startTimeGlobal}, startTime={startTime}");
                // }

                // Log at the end of the frame
                ////Debug.Log($"FallingCor: Item: {this.GetHashCode()} End of Frame: fracJourney={fracJourney}, position={transform.position}");
                // Check if fracJourney exceeds bounds
                if (fracJourney < 1f)
                {
                    //Debug.LogWarning($"FallingCor: Item: {this.GetHashCode()} Warning: fracJourney={fracJourney} exceeded bounds. Clamping to 1.");
                    // fracJourney = 1f;
                    yield return new WaitForEndOfFrame(); // Wait for next frame
                }
                //    yield return new WaitForEndOfFrame(); // Wait for next frame

                // if (waypoint.instant && IsInstantMoveNeeded(square.transform.position))
                // {
                //     ////Debug.Log($"FallingCor: Item: {this.GetHashCode()} Instant Move Triggered: waypoint={waypoint}");
                //     PerformInstantMove(square, waypoint);
                //     yield break;
                // }

                CheckForNewSquare(ref waypoints, ref startPos, ref fracJourney, wayMakerPoint);
                // //Debug.Log($"FallingCor: Item: {this.GetHashCode()} New Square Check: fracJourney={fracJourney}, startPos={startPos}");
            }

            // //Debug.Log($"FallingCor: Item: {this.GetHashCode()} Finished Movement. Final fracJourney={fracJourney}, final position={transform.position}");
        }


        private IEnumerator WaitForFallingPause()
        {
            yield return new WaitWhile(() => MainManager.Instance.StopFall); // Wait while falling is stopped
        }

        private void LogFallResumed(float totalPauseTime)
        {
            //Debug.Log($"FallingCor: {this.GetHashCode()} Fall resumed, total pause time: {totalPauseTime}");
        }

        // New methods below

        private Vector3 CalculateDestination(WayMakerPoint wayMakerPoint)
        {
            return wayMakerPoint.destPosition + Vector3.back * 0.2f; // Adjust destination position
        }

        private float CalculateDistance(Vector3 startPos, Vector3 destPos)
        {
            return Vector2.Distance(startPos, destPos); // Calculate distance
        }

        private bool IsDistanceTooSmall(float distance)
        {
            if (distance < 0.2f)
            {
                //Debug.Log($"FallingCor: {this.GetHashCode()} Distance too small, skipping to next waypoint.");
                return true; // Distance is too small
            }

            return false;
        }

        private void ApplyTeleportationEffects(WayMakerPoint wayMakerPoint)
        {
            if (wayMakerPoint.Rectangle?.teleportDestination != null)
            {
                //Debug.Log($"FallingCor: {this.GetHashCode()} Applying teleportation effect to destination.");
                TeleportationEffect(wayMakerPoint.Rectangle);
            }
            else if (wayMakerPoint.Rectangle?.teleportOrigin != null)
            {
                //Debug.Log($"FallingCor: {this.GetHashCode()} Applying teleportation effect to origin.");
                TeleportationEffect(wayMakerPoint.Rectangle);
            }
        }

        private bool HandleFallingPause(ref float pauseTime)
        {
            if (MainManager.Instance.StopFall)
            {
                pauseTime = Time.time; // Record pause time
                //Debug.Log($"FallingCor: {this.GetHashCode()} Fall stopped, pausing.");
                return true; // Fall has stopped
            }

            return false; // Fall is not stopped
        }

        private void UpdateStartTime(ref float startTime, ref float startTimeGlobal, float totalPauseTime)
        {
            startTime += totalPauseTime;
            startTimeGlobal += totalPauseTime;
        }

        private float CalculateSpeed(int i, float startTimeGlobal)
        {
            float indexFactor = 1 + (i * 1f); // Adjust this multiplier to control the variance
            int sideFall =
                MainManager.Instance.gameStatus == GameState.PreWinAnimations
                    ? 2
                    : 2; // Adjust speed for different game states
            float speed = MainManager.Instance.fallingCurve.Evaluate(Time.time - startTimeGlobal) * sideFall *
                          indexFactor; // Calculate speed
            //  Debug.Log($"[FallingScript][CalculateSpeed] ItemID={this.GetHashCode()} i={i} Calculated Speed={speed}"); // Log calculated speed and parameters
            return speed; // Return the calculated speed
        }

        private float MoveTowardsDestination(Vector3 startPos, Vector3 destPos, float speed, ref float startTime)
        {
            // Calculate the distance covered based on time and speed
            float distCovered = (Time.time - startTime) * speed;
            float totalDistance = CalculateDistance(startPos, destPos);

            // Ensure we don't divide by zero
            if (totalDistance == 0) return 1f;

            // Fraction of the journey, clamped to avoid overshooting
            float fracJourney = Mathf.Clamp01(distCovered / totalDistance);

            // Use a combination of SmoothStep and Sinusoidal easing for smoother interpolation
            float easedFrac = Mathf.SmoothStep(0, 1, Mathf.Sin(fracJourney * Mathf.PI * 0.5f));

            // Update position with the eased fraction
            transform.position = Vector2.Lerp(startPos, destPos, easedFrac);

            // Log debug information
            //Debug.Log($"FallingCor: {this.GetHashCode()} Moving to position item: {this.GetHashCode()}{transform.position}, eased fraction of journey: {easedFrac}");

            // Return the eased fraction of journey completed
            return easedFrac;
        }

        private float MoveTowardsDestination_Simplified(Vector3 startPos, Vector3 destPos, float speed,
            ref float startTime)
        {
            // Calculate the distance covered based on time and speed
            float distCovered = (Time.time - startTime) * speed;
            // Use Vector3.Distance for clarity, though Vector2.Distance was used before
            float totalDistance = Vector3.Distance(startPos, destPos);

            // Ensure we don't divide by zero
            if (totalDistance <= 0.001f) // Use a small threshold instead of exact zero for float comparison
            {
                transform.position = destPos; // Snap to destination if distance is negligible
                return 1f;
            }

            // Fraction of the journey, clamped to avoid overshooting
            float fracJourney = Mathf.Clamp01(distCovered / totalDistance);

            // --- REMOVED EASING ---
            // float easedFrac = Mathf.SmoothStep(0, 1, Mathf.Sin(fracJourney * Mathf.PI * 0.5f));

            // Update position using standard LINEAR interpolation
            // Note: Lerping Vector2 based on Vector3 positions implicitly ignores Z
            transform.position = Vector2.Lerp(startPos, destPos, fracJourney);

            // Log debug information if needed for testing
            // Debug.Log($"Simplified Move: Item {this.GetInstanceID()} Pos: {transform.position}, Frac: {fracJourney}");

            // Return the raw fraction of journey completed
            return fracJourney;
        }


        // Handles the overall collision detection and movement behavior
        private void HandleCollisionAndMovement(ref Vector3 startPos, ref float startTimeGlobal, ref float startTime,
            ref float fracJourney, WayMakerPoint wayMakerPoint)
        {
            // Step 1: Check for collisions
            RaycastHit2D hit2D = DetectCollision(startPos, wayMakerPoint);

            // Step 2: Determine movement behavior based on collision status
            // if (IsMoveAllowed(hit2D, startPos, waypoint))
            // {
            //     // Movement logic will be handled by another method (e.g., MoveTowardsDestination)
            // }
            if (ShouldSkipItem(hit2D))
            {
                // SkipItem(ref startPos, ref startTimeGlobal, ref startTime);
            }
        }

        // Detects collision with other items using raycasting// Detects collision with other items by performing a raycast
        private RaycastHit2D DetectCollision(Vector3 startPos, WayMakerPoint wayMakerPoint)
        {
            // Step 1: Calculate the movement direction towards the waypoint
            Vector3 direction = CalculateMovementDirection(startPos, wayMakerPoint);

            // Step 2: Perform raycasting to detect collisions
            RaycastHit2D[] raycastHits = PerformRaycast(direction);

            // Step 3: Return the first valid collision result that isn't the current item
            return GetFirstValidHit(raycastHits);
        }

        // Calculates the normalized direction towards the waypoint's destination
        private Vector3 CalculateMovementDirection(Vector3 startPos, WayMakerPoint wayMakerPoint)
        {
            return (CalculateDestination(wayMakerPoint) - startPos).normalized;
        }

        // Performs raycasting to detect possible collisions with items
        private RaycastHit2D[] PerformRaycast(Vector3 direction)
        {
            RaycastHit2D[] raycastHits = new RaycastHit2D[2]; // Array to store raycast results
            Physics2D.RaycastNonAlloc(
                transform.position + direction * -0.5f, // Start position for the raycast (slightly offset)
                direction, // Direction of the raycast
                raycastHits,
                0.8f, // Maximum distance for the raycast
                1 << LayerMask.NameToLayer("Item") // Layer mask to detect only items
            );
            return raycastHits; // Return all raycast hits
        }

        // Returns the first valid hit that isn't the current item
        private RaycastHit2D GetFirstValidHit(RaycastHit2D[] raycastHits)
        {
            return raycastHits.FirstOrDefault(x => x.transform != transform); // Ignore self-collision
        }

        public void MarkForSkipFalling()
        {
            skipFalling = true;
            //Debug.Log($"{this.name} has been marked to skip falling.");
        }


        // Checks if movement is allowed based on collision and current position
        // Determines if movement is allowed based on collision and position checks
        private bool IsMoveAllowed(RaycastHit2D hit2D, Vector3 startPos, WayMakerPoint wayMakerPoint)
        {
            // Step 1: Check if the item can move (no collision or non-falling item)
            if (CanMoveWithoutCollision(hit2D))
            {
                return true;
            }

            // Step 2: Check if the movement direction aligns with the waypoint and current square
            if (!IsDirectionMatching(startPos, wayMakerPoint) || ShouldSkipItem(hit2D))
            {
                return false; // Movement is not allowed
            }

            return true; // Movement is allowed
        }

        // Checks if there is no collision or the hit item is not falling
        private bool CanMoveWithoutCollision(RaycastHit2D hit2D)
        {
            // Allow movement if there is no collision or if the colliding item is not falling
            return !hit2D || !hit2D.transform.GetComponent<Item>().falling;
        }

        // Checks if the movement direction matches the destination and square
        private bool IsDirectionMatching(Vector3 startPos, WayMakerPoint wayMakerPoint)
        {
            if (wayMakerPoint == null || square == null) // Safeguard against null values
            {
                Debug.LogError("Waypoint or square is null. Cannot calculate direction matching.");
                return false;
            }

            Vector2 destinationDir =
                GetDestinationDirection(startPos, wayMakerPoint); // Calculate direction towards waypoint
            Vector2 currentSquareDir = GetSquareDirection(startPos); // Calculate direction from current square

            // Check if the destination direction matches both the square direction and its intended direction
            return (destinationDir == currentSquareDir && destinationDir == square.direction);
        }

        // Helper method to calculate the direction towards the waypoint's destination
        private Vector2 GetDestinationDirection(Vector3 startPos, WayMakerPoint wayMakerPoint)
        {
            return ((Vector2)CalculateDestination(wayMakerPoint) - (Vector2)startPos).normalized;
        }

        // Helper method to calculate the direction based on the square's current position
        private Vector2 GetSquareDirection(Vector3 startPos)
        {
            return ((Vector2)square.transform.position - (Vector2)startPos).normalized;
        }


        // Adjusts position and time variables if an item is skipped
        private void SkipItem(ref Vector3 startPos, ref float startTimeGlobal, ref float startTime)
        {
            startPos = transform.position; // Update start position to current item position
            startTimeGlobal = Time.time; // Reset global start time
            startTime = Time.time; // Reset local start time
            //Debug.Log($"FallingCor: {this.GetHashCode()} Skipping item."); // Log the skip action
        }

        // // Determines if an item should be skipped based on collision information
        // private bool ShouldSkipItem(RaycastHit2D hit2D)
        // {
        //     // Add logic for determining if the item should be skipped (e.g., based on game rules)
        //     // Example: if the item has specific properties or conditions, return true.
        //     return false; // Placeholder logic
        // }


        private bool IsInstantMoveNeeded(Vector3 currentPosition)
        {
            return Vector2.Distance(currentPosition, transform.position) > 2;
        }

        private void PerformInstantMove(Rectangle rectangle, WayMakerPoint wayMakerPoint)
        {
            Vector3 pos = rectangle.GetReverseDirection(); // Calculate reverse position
            transform.position =
                CalculateDestination(wayMakerPoint) + pos * field.squareHeight + Vector3.back * 0.2f; // Adjust position
            JustCreatedItem = true;
            falling = false;
            needFall = true;
            fallingID = 0;
            //Debug.Log($"FallingCor: {this.GetHashCode()} Instant move detected, resetting fall and starting new destination.");

            // Start falling to a new destination
            List<WayMakerPoint> list = new List<WayMakerPoint>
                { new WayMakerPoint(rectangle.transform.position, rectangle) };
            StartFallingTo(list);
        }

        private void CheckForNewSquare(ref List<WayMakerPoint> waypoints, ref Vector3 startPos, ref float fracJourney,
            WayMakerPoint wayMakerPoint)
        {
            if (fracJourney >= 0.5f)
            {
                var squareNew = square?.GetNextSquare() ?? null; // Get the next square with null check
                if (squareNew != null && squareNew.Item == null && squareNew.IsFree())
                {
                    JustCreatedItem = false;
                    square.Item = null;
                    squareNew.Item = this;
                    //Debug.Log($"FallingCor: {this.GetHashCode()} Changed square from {square} to {squareNew}.");

                    square = squareNew;
                    waypoints.Add(new WayMakerPoint(squareNew.transform.position + Vector3.back * 0.2f,
                        squareNew)); // Add new waypoint
                    Vector3 destPos = CalculateDestination(wayMakerPoint);
                    float distance = CalculateDistance(startPos, destPos);
                    //Debug.Log($"FallingCor: {this.GetHashCode()} Added new waypoint. New destination: {destPos}, distance: {distance}");
                }
            }
        }

        /// Handles the steps after the falling animation is complete,
        /// including playing sounds, checking for matches, and finalizing the fall.
        private IEnumerator HandlePostFall(bool animate, Action callback)
        {
            ResetFallFlags();
            Vector3 destPos = CalculateDestinationPosition();

            if (ShouldPlayDropSound(destPos, animate))
            {
                PlayDropSound();
            }

            CheckSquareBelow();
            //  if (!needFall)
            {
                StartCoroutine(HandlePostLandingEffects(animate));
            }
            yield return WaitForFallToFinish();


            callback?.Invoke();

            //PositionItem(destPos);
        }

        private void PositionItem(Vector3 destPos)
        {
            transform.position = destPos;
        }

        private Vector3 CalculateDestinationPosition()
        {
            // Safety check - if square is null, return current position
            if (square == null)
            {
                Debug.LogWarning($"Square is null for item {name}. Using current position.");
                return transform.position;
            }

            return square.transform.position + Vector3.back * 0.2f;
        }


        private void ResetFallFlags()
        {
            JustCreatedItem = false;
            if (previousSquare?.Item == this)
            {
                previousSquare.Item = null;
            }
        }

        private bool ShouldPlayDropSound(Vector3 destPos, bool animate)
        {
            float distance = Vector2.Distance(transform.position, destPos);
            return distance > 0.5f && animate;
        }

        private void PlayDropSound()
        {
            CentralSoundManager.Instance?.PlayOneShot(
                CentralSoundManager.Instance.drop[Random.Range(0, CentralSoundManager.Instance.drop.Length)]);
        }

        // Removed the wait from WaitForFallToFinish as it's now handled conditionally in HandlePostFall
        private IEnumerator WaitForFallToFinish()
        {
            fallingID = 0;
            StopFallFinished();
            //    if (LevelManager.THIS.Falling)
            // {
            //     yield return new WaitUntil(() => !LevelManager.THIS.Falling);
            // }
            yield return new WaitForSeconds(0.2f);
            ProcessMatches(); // Process matches if any formed on landing
            OnStopFall(); // Trigger stop fall event
        }

        private static readonly WaitForEndOfFrame waitForEndOfFrame = new WaitForEndOfFrame();

        private IEnumerator HandlePostLandingEffects(bool animate = true)
        {
            yield return new WaitForSeconds(MainManager.Instance.waitAfterFall);
            // Only apply deformation if the item is truly stopping (not falling or needing to fall further)
            if (currentType == ItemsTypes.NONE && animate == true && !falling && !needFall)
                ApplyDeformationEffect(); // Apply squash effect
            ResetAnimTransform(); // Reset animation transform regardless
            //  ProcessMatches(); // Process matches if any formed on landing
            // OnStopFall(); // Trigger stop fall event
        }


        public void ProcessMatches()
        {
            // Debug.LogError($"ProcessMatches: {this.GetHashCode()} {GetInstanceID()} - No falling neighbors with matching color found. Retrieving matches around. Position: {transform.position}");
            var combines = GetMatchesAround();

            foreach (var combine in combines)
            {
                if (combine.nextType == ItemsTypes.NONE)
                    continue;

                var item = combine.items[2];
                if (item != null && item.NextType == ItemsTypes.NONE)
                {
                    item.NextType = combine.nextType;
                }

                if (combine.items.Contains(this))
                {
                    Debug.Log($"Current item {name} is part of this combine.");
                }
                else
                {
                    Debug.LogError($"Current item {name} is NOT part of this combine.");
                }
            }

            HandleDestructionOfMatchedItems(combines);
        }

        public bool IsBeingProcessedForDestruction { get; set; } = false;

        private void HandleDestructionOfMatchedItems(List<CombineClass> combines)
        {
            if (combines == null || combines.Count == 0)
            {
                return; // Nothing to process
            }

            // --- Step 1: Collect all unique items from combines that are NOT already being processed ---
            HashSet<Item> itemsToPotentiallyDestroy = new HashSet<Item>();
            bool containsAlreadyProcessedItem = false;

            foreach (CombineClass combine in combines)
            {
                if (combine?.items == null) continue;

                foreach (Item item in combine.items)
                {
                    if (item == null) continue;

                    // *** The Core Check ***
                    if (item.IsBeingProcessedForDestruction)
                    {
                        // If ANY item involved in this batch of combines is already being handled,
                        // it's safest to ABORT processing this entire batch for now.
                        // The process that already claimed the item will handle its destruction.
                        Debug.LogWarning(
                            $"[HandleDestruction] Aborting check. Item '{item.gameObject.name}' ({item.GetInstanceID()}) is already being processed by another operation.");
                        containsAlreadyProcessedItem = true;
                        return; // Stop checking items within this combine
                    }

                    // If not being processed, add it to our set for potential destruction
                    itemsToPotentiallyDestroy.Add(item);
                }

                if (containsAlreadyProcessedItem)
                {
                    break; // Stop processing further combines in this batch
                }
            }

            // --- Step 2: If any item was already flagged, stop here ---
            if (containsAlreadyProcessedItem)
            {
                return; // Exit, let the other process complete.
            }

            // --- Step 3: If no items were flagged, and we have items, proceed ---
            if (itemsToPotentiallyDestroy.Count == 0)
            {
                // This might happen if combines contained only null or already processed items
                // (though the previous check should handle the latter).
                return;
            }

            // Convert the HashSet to a List
            List<Item> finalItemsToDestroy = itemsToPotentiallyDestroy.ToList();

            // --- Step 4: CRITICAL - Mark all these items as being processed NOW ---
            // Do this *before* starting the coroutine or direct destruction.
            Debug.Log($"[HandleDestruction] Marking {finalItemsToDestroy.Count} items for destruction.");
            foreach (Item item in finalItemsToDestroy)
            {
                item.IsBeingProcessedForDestruction = true;
                // Optional: Add a visual cue immediately (e.g., slight fade)
                // item.visualController.FadeSlightly();
            }

            // --- Step 5: Execute the Destruction Logic ---
            // Determine if it's a simple match (<=3) or requires DestroyGroup (>3)
            // Your original logic checked combines.Count > 3. Let's stick to that intent for now,
            // assuming a single DestroyGroup handles all items if any combine was > 3,
            // OR maybe you intended total unique items > 3? Let's assume total unique items.
            if (finalItemsToDestroy.Count > 3)
            {
                Debug.Log($"[HandleDestruction] Creating DestroyGroup for {finalItemsToDestroy.Count} items.");
                // Ensure DestroyGroup constructor also respects/sets the flag if needed (belt and braces)
                DestroyGroup destroyGroup = new DestroyGroup(finalItemsToDestroy);

                // Adjust parameters: Often for >3 matches, you might wa nt particles OFF
                // because the MoveToTargetAnim is the main effect, and maybe an explosion effect ON?
                StartCoroutine(destroyGroup.DestroyGroupCor(showScore: true, useParticles: false,
                    useExplosionEffect: true)); // Example params
            }
            else // Handle 3-matches (or fewer if possible)
            {
                Debug.Log($"[HandleDestruction] Destroying {finalItemsToDestroy.Count} items directly.");
                foreach (Item item in finalItemsToDestroy)
                {
                    // Check null again just in case (shouldn't happen here)
                    if (item != null)
                    {
                        // For simple 3-matches, usually show score, use particles, no explosion.
                        item.DestroyItem(showScore: true, particles: true, destroyNeighbours: true);
                        // Item.DestroyItem() MUST reset the IsBeingProcessedForDestruction flag
                    }
                }
                // If destroying directly, ensure falling/matching restarts appropriately after this.
            }
        }


        // #region Initialization and Finalization

        /// <summary>
        /// Initializes the falling process. Returns true if already falling and should exit.
        /// </summary>
        /// <summary>
        /// Immediately finalize falling (if skipping or aborted).
        /// </summary>
        private void FinalizeImmediately()
        {
            falling = false;
            needFall = false;
            fallingID = 0;
        }

        //#endregion

        //#region Waypoint Processing

        /// <summary>
        /// Processes movement to a single waypoint.
        /// Includes movement, speed calculation, teleportation effects, and collision checks.
        /// </summary>
        private IEnumerator newProcessWaypoint(List<WayMakerPoint> waypoints, int index, bool animate,
            float startTimeGlobal)
        {
            // Retrieve the waypoint
            WayMakerPoint wayMakerPoint = waypoints[index];
            Vector3 startPos = transform.position;
            Vector3 destPos = wayMakerPoint.destPosition + Vector3.back * 0.2f;
            float distance = Vector2.Distance(startPos, destPos);

            // If minimal distance, no need to move
            if (distance < 0.2f)
            {
                Log(
                    $"[FallingScript][ProcessWaypoint] Distance too small, skipping. ItemID={GetInstanceID()} Index={index}");
                yield break;
            }

            // Apply teleportation effects if applicable
            newApplyTeleportationEffects(wayMakerPoint);

            // Begin moving towards the destination
            yield return newMoveTowardsDestination(waypoints, index, startTimeGlobal, startPos, destPos, wayMakerPoint);
        }

        /// <summary>
        /// Applies teleportation effects if the waypoint is part of a teleportation system.
        /// </summary>
        private void newApplyTeleportationEffects(WayMakerPoint wayMakerPoint)
        {
            if (wayMakerPoint.Rectangle?.teleportDestination != null || wayMakerPoint.Rectangle?.teleportOrigin != null)
            {
                TeleportationEffect(wayMakerPoint.Rectangle);
                Log(
                    $"[FallingScript][ApplyTeleportationEffects] TeleportationEffect applied. ItemID={GetInstanceID()} Square={wayMakerPoint.Rectangle}");
            }
        }

        // #endregion

        //#region Movement Logic

        /// <summary>
        /// Moves the item from its current position towards the target destination.
        /// Handles pausing, collision checks, instant move conditions, and mid-fall square changes.
        /// </summary>
        private IEnumerator newMoveTowardsDestination(List<WayMakerPoint> waypoints, int i, float startTimeGlobal,
            Vector3 startPos, Vector3 destPos, WayMakerPoint wayMakerPoint)
        {
            float startTime = Time.time;
            float pauseTime = Time.time;
            float totalPauseTime = 0.0f;
            float fracJourney = 0f;
            bool fallStopped = false;

            while (fracJourney < 0.9f)
            {
                // If fall is stopped, record pause time and wait
                if (MainManager.Instance.StopFall)
                {
                    fallStopped = true;
                    pauseTime = Time.time;
                    Log($"[FallingScript][MoveTowardsDestination] Fall stopped. ItemID={GetInstanceID()}");
                }

                yield return new WaitWhile(() => MainManager.Instance.StopFall);

                if (fallStopped && !MainManager.Instance.StopFall)
                {
                    // Fall resumed
                    fallStopped = false;
                    totalPauseTime += Time.time - pauseTime;
                    startTime += totalPauseTime;
                    startTimeGlobal += totalPauseTime;
                    Log(
                        $"[FallingScript][MoveTowardsDestination] Fall resumed. ItemID={GetInstanceID()} totalPauseTime={totalPauseTime}");
                }

                // Calculate speed using improved logic
                //float speed = ApplySpeedCalculation(i, startTimeGlobal);
                float speed = CalculateSpeed(i, startTimeGlobal);
                // Check collisions
                RaycastHit2D hit2D = DetectCollisionTowards(destPos, startPos);

                // Decide how to move based on collision
                if (CanMoveWithoutStopping(hit2D, startPos, destPos))
                {
                    // Move towards the destination
                    // fracJourney = UpdatePositionOverTime(startPos, destPos, speed, startTime);
                    fracJourney = Mathf.Clamp(MoveTowardsDestination(startPos, destPos, speed, ref startTime), 0f, 1f);
                }
                else if (newShouldSkipItem(hit2D))
                {
                    // Reset times if skipping item to attempt a move again
                    newSkipItem(ref startPos, ref startTimeGlobal, ref startTime);
                    fracJourney = 0f; // Restart journey fraction
                }

                // // Check instant move condition (if item is too far from the square)
                // if (waypoint.instant && Vector2.Distance(square.transform.position, transform.position) > 2)
                // {
                //     PerformInstantMove(waypoint);
                //     yield break; // Stop current movement logic
                // }

                // // Attempt a mid-fall square change if fraction >= 0.5
                // if (fracJourney >= 0.5f)
                // {
                //     AttemptMidFallSquareChange(waypoints, ref startPos, destPos, waypoint);
                // }

                // If still not reached destination, wait a frame
                if (fracJourney < 1f)
                    yield return new WaitForEndOfFrame();
            }
        }

        /// <summary>
        /// Applies the improved speed calculation logic.
        /// Integrates new features: speed adjusts based on index and game state.
        /// </summary>
        private float ApplySpeedCalculation(int index, float startTimeGlobal)
        {
            int sideFall = (MainManager.Instance.gameStatus == GameState.PreWinAnimations) ? 3 : 2;
            float indexFactor = 1 + (index * 0.5f); // Slight incremental factor per waypoint
            float speed = MainManager.Instance.fallingCurve.Evaluate(Time.time - startTimeGlobal) * sideFall *
                          indexFactor;
            return speed;
        }

        /// <summary>
        /// Detects collision with other items along the path.
        /// </summary>
        private RaycastHit2D DetectCollisionTowards(Vector3 destPos, Vector3 startPos)
        {
            Vector3 direction = (destPos - startPos).normalized;
            RaycastHit2D[] hits = new RaycastHit2D[2];
            Physics2D.RaycastNonAlloc(transform.position + direction * -0.5f, direction, hits, 0.8f,
                1 << LayerMask.NameToLayer("Item"));
            return hits.FirstOrDefault(x => x.transform != transform);
        }

        /// <summary>
        /// Determines if movement can continue based on collision detection.
        /// Original logic retained, but structured more clearly.
        /// </summary>
        private bool CanMoveWithoutStopping(RaycastHit2D hit2D, Vector3 startPos, Vector3 destPos)
        {
            // If no collision or collided item isn't falling, we can move
            if (!hit2D || !hit2D.transform.GetComponent<Item>().falling) return true;

            // If direction doesn't match expected directions, don't move
            Vector2 direction = ((Vector2)destPos - (Vector2)startPos).normalized;
            if (direction != ((Vector2)square.transform.position - (Vector2)startPos).normalized)
                return false;
            if (direction != square.direction)
                return false;

            // If we should skip item due to logic, movement halts temporarily
            return !newShouldSkipItem(hit2D);
        }

        /// <summary>
        /// Determines if we should skip the item currently blocking the fall.
        /// This logic can be adjusted if the skipping caused issues.
        /// </summary>
        private bool newShouldSkipItem(RaycastHit2D hit2D)
        {
            // In original code, this was a separate condition check.
            // Adjust this as needed. If skip logic caused bugs, we can limit it:
            // Return false to disable skipping or refine logic if needed.
            return false;
        }

        /// <summary>
        /// Resets the start positions and time tracking if we decide to skip an item.
        /// </summary>
        private void newSkipItem(ref Vector3 startPos, ref float startTimeGlobal, ref float startTime)
        {
            startPos = transform.position;
            startTimeGlobal = Time.time;
            startTime = Time.time;
            Log($"[FallingScript][SkipItem] Skipping item. ItemID={GetInstanceID()}");
        }

        /// <summary>
        /// Updates the item position over time based on speed and returns fraction of journey completed.
        /// </summary>
        private float UpdatePositionOverTime(Vector3 startPos, Vector3 destPos, float speed, float startTime)
        {
            float distCovered = (Time.time - startTime) * speed;
            float distance = Vector2.Distance(startPos, destPos);
            if (distance == 0) return 1f;

            float fracJourney = Mathf.Clamp01(distCovered / distance);
            float smoothedFrac = Mathf.SmoothStep(0, 1, fracJourney);
            transform.position = Vector2.Lerp(startPos, destPos, smoothedFrac);

            return fracJourney;
        }

        private float UpdatePositionOverTime_Simplified(Vector3 startPos, Vector3 destPos, float speed, float startTime)
        {
            // Calculate distance covered based on time and speed
            float distCovered = (Time.time - startTime) * speed;
            // Calculate total distance
            float distance = Vector2.Distance(startPos, destPos); // Original uses Vector2 distance

            // Handle negligible distance with a threshold and snap position
            if (distance <= 0.001f) // Use a small threshold for float comparison
            {
                transform.position = destPos; // Snap to destination
                return 1f; // Indicate journey is complete
            }

            // Calculate the raw fraction of the journey, clamped
            float fracJourney = Mathf.Clamp01(distCovered / distance);

            // --- REMOVED EASING ---
            // float smoothedFrac = Mathf.SmoothStep(0, 1, fracJourney);

            // Update position using standard LINEAR interpolation with the raw fraction
            transform.position = Vector2.Lerp(startPos, destPos, fracJourney);

            // Log debug information if needed
            // Debug.Log($"Simplified UpdatePos: Item {this.GetInstanceID()} Pos: {transform.position}, Frac: {fracJourney}");

            // Return the raw fraction of journey completed (as the original did)
            return fracJourney;
        }

        /// <summary>
        /// Performs an instant move if the item is too far from its target position (teleport scenario).
        /// </summary>
        private void PerformInstantMove(WayMakerPoint wayMakerPoint)
        {
            Vector3 pos = square.GetReverseDirection();
            Vector3 instantDest = wayMakerPoint.destPosition + Vector3.back * 0.2f + pos * field.squareHeight;
            transform.position = instantDest;
            JustCreatedItem = true;
            falling = false;
            needFall = true;
            fallingID = 0;

            Log(
                $"[FallingScript][PerformInstantMove] Instant move triggered. ItemID={GetInstanceID()} Pos={transform.position}");

            List<WayMakerPoint> newWaypoints = new List<WayMakerPoint>
                { new WayMakerPoint(square.transform.position, square) };
            StartFallingTo(newWaypoints);
        }

        /// <summary>
        /// Attempts to change the square mid-fall if we are halfway through the journey.
        /// If next square is free, move the item to that square and add a new waypoint.
        /// </summary>
        private void AttemptMidFallSquareChange(List<WayMakerPoint> waypoints, ref Vector3 startPos, Vector3 destPos,
            WayMakerPoint wayMakerPoint)
        {
            Rectangle rectangleNew = square.GetNextSquare(true);
            if (rectangleNew != null && rectangleNew.Item == null && rectangleNew.IsFree())
            {
                JustCreatedItem = false;
                square.Item = null;
                rectangleNew.Item = this;
                Log(
                    $"[FallingScript][AttemptMidFallSquareChange] Changed square. ItemID={GetInstanceID()} OldSquare={square} NewSquare={rectangleNew}");

                square = rectangleNew;
                waypoints.Add(new WayMakerPoint(rectangleNew.transform.position + Vector3.back * 0.2f, rectangleNew));
                // Recalculate distance if needed
                destPos = wayMakerPoint.destPosition + Vector3.back * 0.2f;
                float distance = Vector2.Distance(startPos, destPos);
                Log($"[FallingScript][AttemptMidFallSquareChange] Added new waypoint. distance={distance}");
            }
        }

        // #endregion

        // #region Post-Fall Handling

        /// <summary>
        /// Handles all logic after the item finishes falling:
        /// - Playing sounds
        /// - Checking matches and destroying items
        /// - Applying post-fall effects (e.g. squash)
        /// - Resetting animations
        /// </summary>
        private IEnumerator newHandlePostFall(bool animate, Action callback)
        {
            JustCreatedItem = false;
            if (previousSquare?.Item == this) previousSquare.Item = null;

            Vector3 destPos = square.transform.position + Vector3.back * 0.2f;
            float distanceFromEnd = Vector2.Distance(transform.position, destPos);

            // Play drop sound if we moved a significant distance
            if (distanceFromEnd > 0.5f && animate)
            {
                CentralSoundManager.Instance?.PlayOneShot(
                    CentralSoundManager.Instance.drop[
                        UnityEngine.Random.Range(0, CentralSoundManager.Instance.drop.Length)]);
            }

            fallingID = 0;
            StopFallFinished();

            // Wait until falling fully stops
            yield return new WaitWhile(() => falling);
            yield return new WaitForSeconds(MainManager.Instance.waitAfterFall);

            // Align to final position
            transform.position = destPos;
            CheckSquareBelow();

            // Invoke callback if provided
            callback?.Invoke();

            // If no further falling needed, apply post-fall effects
            if (!needFall)
            {
                ResetAnimTransform();

                // Small delay to ensure everything settled
                yield return new WaitForSeconds(0.2f);

                // Check for matches and destroy items
                yield return HandleMatchesAndDestroy();

                // If it's a normal item, apply squash effect (new feature)
                if (currentType == ItemsTypes.NONE)
                {
                    ApplyDeformationEffect(); // Post-fall squash effect
                    Log($"[FallingScript][HandlePostFall] Deformation effect applied. ItemID={GetInstanceID()}");
                }

                OnStopFall();
            }
        }

        /// <summary>
        /// Finds and destroys matched items. Integrate improved destruction logic here.
        /// </summary>
        private IEnumerator HandleMatchesAndDestroy()
        {
            // Check if any neighbors are still falling with the same color.
            // If none, proceed to find and handle matches.
            bool anyFallingNeighborSameColor =
                square.GetAllNeghborsCross().Any(i => i.Item && i.Item.falling && i.Item.color == color);
            if (anyFallingNeighborSameColor)
            {
                yield break; // Wait for them to settle
            }

            // Retrieve matches
            var combines = GetMatchesAround();
            Log($"[FallingScript][HandleMatchesAndDestroy] Found {combines.Count} combines. ItemID={GetInstanceID()}");

            // If Script 2 introduced a more advanced destruction logic, apply it here.
            // For demonstration, if more than 3 items in total, use a DestroyGroup, else destroy individually.
            var allMatchedItems = combines.SelectMany(c => c.items).Distinct().ToList();


            foreach (var combine in combines)
            {
                if (combine.nextType != ItemsTypes.NONE)
                {
                    var firstItem = combine.items.FirstOrDefault();
                    if (firstItem != null && firstItem.NextType == ItemsTypes.NONE)
                    {
                        firstItem.NextType = combine.nextType;
                    }
                }
            }


            if (allMatchedItems.Count > 3)
            {
                // Use a DestroyGroup approach if available
                DestroyGroup destroyGroup = new DestroyGroup(allMatchedItems);
                yield return destroyGroup.DestroyGroupCor(showScore: false, useParticles: true,
                    useExplosionEffect: false);
                Log(
                    $"[FallingScript][HandleMatchesAndDestroy] DestroyGroup used. ItemID={GetInstanceID()} ItemsCount={allMatchedItems.Count}");
            }
            else
            {
                // Destroy items individually
                allMatchedItems.ForEach(x => x?.DestroyItem(false));
                Log(
                    $"[FallingScript][HandleMatchesAndDestroy] Individual destruction. ItemID={GetInstanceID()} ItemsCount={allMatchedItems.Count}");
            }


            if (square != null && square.type == LevelTargetTypes.ExtraTargetType2)
            {
                MainManager.Instance.levelData.GetTargetObject()
                    .CheckSquares(allMatchedItems.Select(i => i.square).ToArray());
            }
        }

        public bool isLoggingEnabled = true; // Enable/disable logging

        private void Log(string message)
        {
            // if (!isLoggingEnabled) return;
            Debug.Log(message);
        }


        bool IsNeighbor(Item other)
        {
            return square.GetAllNeghborsCross().Contains(other.square);
        }

        bool IsPartOfMatch(CombineClass combineClass)
        {
            return combineClass.items.Any(i => i.color == color);
        }

        private Coroutine deformationCoroutine;

        private void ApplyDeformationEffect()
        {
            // If there is a running coroutine, stop it to prevent overlapping coroutines
            if (deformationCoroutine != null)
            {
                StopCoroutine(deformationCoroutine);
            }

            // Start a new coroutine and store the reference to it
            deformationCoroutine = StartCoroutine(DeformationCoroutine());
        }

        // Optional: Make the landing deformation even faster or remove it if it feels like a pause
        private IEnumerator DeformationCoroutine()
        {
            // Consider reducing these durations significantly for a "slippier" feel
            // float deformationDuration = 0.01f; // Faster squash/stretch // REVERTED
            // float jumpDuration = 0.01f;       // Faster jump // REVERTED

            // --- Rest of the DeformationCoroutine logic remains the same ---
            //Debug.Log($"FallingCor: {this.GetHashCode()} Desinformation Cor started");
            float deformationAmount = 0.9f; // Scale factor for the squash (compression)
            float stretchAmount = 1f; // Slight stretch factor after squash
            float deformationDuration = 0.03f; // Duration for squash and stretch effect // RESTORED ORIGINAL VALUE
            float jumpHeight = 0.03f; // Height for the jump
            float jumpDuration = 0.03f; // Duration of the jump // RESTORED ORIGINAL VALUE

            Vector3 originalScale = transform.localScale;
            Vector3 squashedScale = new Vector3(originalScale.x, originalScale.y * deformationAmount, originalScale.z);
            Vector3 originalPosition = transform.position;
            Vector3 loweredPosition = new Vector3(originalPosition.x,
                originalPosition.y - (originalScale.y - squashedScale.y), originalPosition.z);
            Vector3 jumpPosition = new Vector3(originalPosition.x, originalPosition.y + jumpHeight, originalPosition.z);

            // Squash phase
            float elapsed = 0f;
            while (elapsed < deformationDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / deformationDuration);
                transform.localScale = Vector3.Lerp(originalScale, squashedScale, t);
                transform.position = Vector3.Lerp(originalPosition, loweredPosition, t);
                yield return null;
            }

            transform.localScale = squashedScale;
            transform.position = loweredPosition;

            // Return to original scale and position
            elapsed = 0f;
            while (elapsed < deformationDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / deformationDuration);
                transform.localScale = Vector3.Lerp(squashedScale, originalScale, t);
                transform.position = Vector3.Lerp(loweredPosition, originalPosition, t);
                yield return null;
            }

            transform.localScale = originalScale;
            transform.position = originalPosition;

            // Jump phase
            elapsed = 0f;
            while (elapsed < jumpDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / jumpDuration);
                transform.position = Vector3.Lerp(originalPosition, jumpPosition, t);
                yield return null;
            }

            transform.position = jumpPosition;

            // Return to original position after the jump
            elapsed = 0f;
            while (elapsed < deformationDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / deformationDuration);
                transform.position = Vector3.Lerp(jumpPosition, originalPosition, t);
                yield return null;
            }

            transform.position = originalPosition;

            // Mark the coroutine as completed
            deformationCoroutine = null;
        }

        /// Coroutine to trigger the wave effect
        private IEnumerator TriggerWaveEffect(Rectangle rectangle)
        {
            float waveDelay = 0.05f; // Delay between each wave step
            Rectangle currentRectangle = rectangle.GetNextSquare();

            while (currentRectangle != null && currentRectangle.Item != null)
            {
                //currentSquare.Item.StartCoroutine(currentSquare.Item.ApplyDeformationEffect());
                yield return new WaitForSeconds(waveDelay);
                currentRectangle = currentRectangle.GetNextSquare();
            }
        }

        /// <summary>
        /// Determines if the current item should skip falling based on comparison with a collided item.
        /// The item with the larger magnitude should fall first.
        /// </summary>
        /// <param name="hit2D">The RaycastHit2D result from collision detection.</param>
        /// <returns>True if the current item should skip, otherwise false.</returns>
        private bool ShouldSkipItem(RaycastHit2D hit2D)
        {
            if (hit2D.transform == null) // Safeguard against null values
            {
                Debug.LogError("Hit2D transform is null. Cannot process item.");
                return false;
            }

            // Get the collided item and its square
            Item collidedItem = hit2D.transform.GetComponent<Item>();
            if (collidedItem == null || collidedItem.square == null || square == null) // Safeguard against null values
            {
                Debug.LogError("Collided item or square is null. Cannot determine skipping condition.");
                return false;
            }

            Vector3 collidedItemPosition = collidedItem.square.transform.position;

            // Compare magnitudes (distance from current item)
            bool isCollidedItemFurther = (collidedItemPosition - transform.position).magnitude >
                                         (square.transform.position - transform.position).magnitude;

            // Check if both items are moving in the same direction
            bool isSameDirection = square.direction == collidedItem.square.direction;

            // Skip the current item if the collided item is further and moving in the same direction
            return isCollidedItemFurther && isSameDirection;
        }


        /// <summary>
        /// fall finished events
        /// </summary>
        // Ensure StopFallFinished correctly sets falling = false
        public void StopFallFinished()
        {
            // Report stop falling
            falling = false; // Set falling flag AFTER reporting stop
            if (MainManager.Instance != null && MainManager.Instance.DebugSettings != null &&
                MainManager.Instance.DebugSettings.FallingLog)
                DebugLogManager.Log(name + " stop fall " + square + " pos " + transform.position,
                    DebugLogManager.LogType.Falling);
            // if (square != null && square.Item != this)
            // DestroyBehaviour();
        }

        /// <summary>
        /// On stop after fall event for Ingredient
        /// </summary>
        public virtual void OnStopFall()
        {
        }

        public void ResetPackageAnimation(float t)
        {
            canBePooled = false;
            Invoke("ResetAnimTransform", t);
        }

        public void ResetAnimTransform()
        {
            canBePooled = true;
            itemAnimTransform.transform.localPosition = defaultTransformPosition;
            itemAnimTransform.transform.localScale = defaultTransformScale;
            itemAnimTransform.transform.localRotation = defaultTransformRotation;
        }

        /// <summary>
        /// check near diagonally square
        /// </summary>
        public bool CheckNearEmptySquares()
        {
            var nearEmptySquareDetected = false;
            if (!(square?.CanGoOut() ?? false) || MainManager.Instance.StopFall)
                return false; // Return false if cannot check
            // if (square.nextSquare && square.nextSquare.Item && (square.nextSquare.Item.falling || square.nextSquare.Item.needFall || square.nextSquare.Item.destroying)) return;
            Vector2 lookingDirection1 = new Vector2(1, 1);
            float dirAngle = Vector2.Angle(Vector2.down, square.direction);
            dirAngle = Mathf.Sign(Vector3.Cross(lookingDirection1, square.direction).z) < 0
                ? (360 - dirAngle) % 360
                : dirAngle;
            lookingDirection1 = (Quaternion.Euler(0f, 0f, dirAngle) * lookingDirection1);
            //        if (square.row < LevelManager.This.levelData.maxRows - 1 && square.col < LevelManager.This.levelData.maxCols)
            {
                var checkingSquare = field.GetSquare(square.GetPosition() + lookingDirection1);
                if (checkingSquare && (!checkingSquare.IsItemAbove() && GetBarrierBefore(checkingSquare)))
                    nearEmptySquareDetected = CheckNearSquare(nearEmptySquareDetected, checkingSquare);
                if (nearEmptySquareDetected) return true; // Exit early if side fall started
            }

            //        if (square.row < LevelManager.This.levelData.maxRows - 1 && square.col > 0)
            {
                var checkingSquare = field.GetSquare((Vector3)square.GetPosition() +
                                                     Quaternion.Euler(0f, 0f, 90f) * lookingDirection1);
                if (checkingSquare && (!checkingSquare.IsItemAbove() && GetBarrierBefore(checkingSquare)))
                    nearEmptySquareDetected = CheckNearSquare(nearEmptySquareDetected, checkingSquare);
                if (nearEmptySquareDetected) return true; // Exit early if side fall started
            }
            //        if(LevelManager.This.gameStatus == GameState.Playing)
            //            square.GetPreviousSquare()?.GetAllNeghborsCross().Where(i=>i!=square).Select(i=>i.Item).WhereNotNull().Where(i=>!i.destroying && !i.falling).ToList().ForEach(i=>i?.CheckNearEmptySquares());
            return nearEmptySquareDetected; // Return final status
        }

        private bool GetBarrierBefore(Rectangle checkingRectangle)
        {
            Rectangle[] orderedEnumerable = checkingRectangle.sequenceBeforeThisSquare;
            if (orderedEnumerable.Count() == 0) return true;
            foreach (var sq in orderedEnumerable)
            {
                if (!sq.IsFree()) return true;
                if (sq.isEnterPoint) return false;
                if (!sq.linkedEnterSquare) return true;
            }

            return false;
        }


        private bool CheckNearSquare(bool nearEmptySquareDetected, Rectangle checkingRectangle)
        {
            if (nearEmptySquareDetected) return true; // Already detected, no need to check further

            if (!checkingRectangle.IsNone() && checkingRectangle.CanGoInto() && checkingRectangle.Item == null &&
                !falling)
            {
                if ((checkingRectangle.Item == null || (bool)checkingRectangle.Item?.destroying ||
                     (bool)checkingRectangle.Item?.falling) && checkingRectangle.GetItemAbove() == null)
                {
                    if (square.Item != this)
                        return false; // Cannot move if the item isn't in the current square anymore
                    square.Item = null;
                    previousSquare = square;
                    checkingRectangle.Item = this;
                    square = checkingRectangle;
                    needFall = true;
                    StartFallingSides(); // This initiates the diagonal fall
                    return true; // Indicate side fall started
                }
            }

            return false; // No side fall initiated from this check
        }

        public Item GetLeftItem()
        {
            var sq = square.GetNeighborLeft();
            if (sq != null)
            {
                if (sq.Item != null)
                    return sq.Item;
            }

            return null;
        }

        public Item GetTopItem()
        {
            var sq = square.GetNeighborTop();
            if (sq != null)
            {
                if (sq.Item != null)
                    return sq.Item;
            }

            return null;
        }

        /// <summary>
        /// Change item type methods
        /// </summary>
        /// <param name="newType"></param>
        /// <param name="callback"></param>
        public void SetType(ItemsTypes newType, Action<Item> callback, bool destroyBlock = true)
        {
            NextType = newType;
            ChangeType(callback, false, destroyBlock);
        }

        public void ChangeType(Action<Item> callback = null, bool dontDestroyForThisCombine = true,
            bool destroyBlock = true)
        {
            //Debug.Log($"ChangeType: Starting ChangeType method. NextType={NextType}, Item={this.name}");

            if (NextType != ItemsTypes.NONE)
            {
                //Debug.Log($"ChangeType: NextType is not NONE, starting ChangeTypeCor. NextType={NextType}, dontDestroyForThisCombine={dontDestroyForThisCombine}, destroyBlock={destroyBlock}");
                StartCoroutine(ChangeTypeCor(callback, dontDestroyForThisCombine, destroyBlock));
            }
        }

        // Enumerator for changing the type of an item
        IEnumerator ChangeTypeCor(Action<Item> callback = null, bool dontDestroyForThisCombine = true,
            bool destroyBlock = true)
        {
            // Exit if the next item type is NONE
            if (NextType == ItemsTypes.NONE) yield break;

            // Play sound if the game is not in preparation mode
            if (MainManager.GetGameStatus() != GameState.PrepareGame)
                CentralSoundManager.Instance?.PlayLimitSound(CentralSoundManager.Instance.appearStipedColorBomb);

            // Generate a new item with the next type
            Item newItem = square.GenItem(false, NextType, color);

            // Set combination ID if there's an exploded item
            if (explodedItem != null)
            {
                newItem.combinationID = explodedItem.GetHashCode();
            }

            // Set properties for the new item
            newItem.dontDestroyForThisCombine = dontDestroyForThisCombine;
            newItem.transform.position = transform.position;
            newItem.debugType = newItem.currentType;

            // Log the item type change if debug settings allow
            // if (LevelManager.THIS.DebugSettings.BonusCombinesShowLog)
            Debug /*LogKeeper*/.Log(
                "What set " + NextType + " " + name + " to " +
                newItem.name /*, DebugLogKeeper.LogType.BonusAppearance*/);

            // Check if the new item has a square below it
            List<WayMakerPoint> waypoints = new List<WayMakerPoint>();
            newItem.CheckSquareBelow();
            newItem.square.Item = newItem;
            newItem.transform.position = newItem.square.transform.position;
            NextType = ItemsTypes.NONE;

            // Destroy the block if specified
            if (destroyBlock)
            {
                square.DestroyBlock(destroyNeighbour: false);
                Debug.Log("Block destroyed"); // Add debug log for block destruction
            }

            // Set the square's item to null if it matches the current item
            if (square && this == square.Item)
            {
                square.Item = null;
                Debug.Log("Square item set to null"); // Add debug log for setting square item to null
            }

            // Put the current game object back to the object pool if not destroying
            if (!destroying)
            {
                ObjectPoolManager.Instance.PutBack(gameObject);
                Debug.Log("Object put back to pool"); // Add debug log for putting object back to pool
            }

            // Invoke callback with the new item
            callback?.Invoke(newItem);

            // Pause execution for a short time
            yield return new WaitForSeconds(0.3f);

            // Additional code can be added here if needed
        }


        /// <summary>
        /// animation event trigger after destroy
        /// </summary>
        public void SetAnimationDestroyingFinished()
        {
            Debug.Log("sallog setAnimationdestroy finished " + this.name);
            animationFinished = true;
        }

        /// <summary>
        /// hide item
        /// </summary>
        /// <param name="hide"></param>
        public void Hide(bool hide)
        {
            gameObject.SetActive(!hide);
        }

        /// <summary>
        /// hide sprites
        /// </summary>
        /// <param name="hide"></param>
        public void HideSprites(bool hide)
        {
            GetComponentsInChildren<SpriteRenderer>().ForEachY(i => i.enabled = !hide);
        }

        #region Destroying

        void OnNextMove()
        {
            dontDestroyOnThisMove = false;
            MainManager.OnTurnEnd -= OnNextMove;
        }

        [HideInInspector] public Item explodedItem;
        private bool destroycoroutineStarted;
        public static Item usedItem;
        public int spiral2Counter = 0; // Static counter for SPIRAL2 type

        /// <summary>
        /// destroying item
        /// </summary>
        /// <param name="showScore"></param>
        /// <param name="particles"></param>
        /// <param name="explodedItem"></param>
        /// <param name="explEffect"></param>
        ///
        public void DestroyItem(bool showScore = false, bool particles = true, Item explodedItem = null,
            bool explEffect = false, int bunch = 3, bool WithoutShrink = false,
            bool destroyNeighbours = true, bool CheckJustInItem = true, bool multicolorEffect = false, int color = 0,
            bool isCombinedwithMulti = false)
        {
            if (this.IsBeingProcessedForDestruction && !destroying) // Avoid re-entry somewhat, but primary lock is the flag
            {
                // Potentially log a warning if called again while already processing,
                // but the flag check in HandleDestruction should prevent this mostly.
                Debug.LogWarning(
                    $"Item: {this} is already being processed for destruction. Exiting DestroyItem method.");
            }

            if (justCreatedItem || JustIntItem)
            {
                multicolorEffect = false;
                explEffect = false;
                particles = false;
            }

            MainManager.Instance.StopedAI();
            // destroyNeighbours = true;
            //Debug.Log($"Item: is item {this} JustIntItem is {JustIntItem}");
            //Debug.Log($"Item: sallog sa start destroy with bunch {bunch} and game object {gameObject}" +
            // $" - type {currentType}, nextType {NextType}, ID {GetInstanceID()}, Position {transform.position}");

            if (!gameObject.activeSelf)
            {
                //Debug.Log($"Item: Game object is not active. Exiting DestroyItem method. ID {GetInstanceID()}, Position {transform.position}");
                return;
            }

            // if (square.type == SquareTypes.WireBlock)
            // {
            //     Debug.Log("Item: Square type is WireBlock. Destroying block and exiting DestroyItem method. Position: " + transform.position);
            //     square.DestroyBlock();
            //     StopDestroy();
            //     return;
            // }

            this.explodedItem = explodedItem;

            if (!explodedItem && (!ChopperTarget || (!ChopperTarget?.activeSelf ?? false)) &&
                (combinationID == explodedItem?.GetHashCode() || dontDestroyOnThisMove || needFall) || !Explodable)
            {
                //Debug.Log($"Item: Conditions for not destroying the item are met. Exiting DestroyItem method. Position: {transform.position}");
                StopDestroy();
                return;
            }

            if (MainManager.Instance.gameStatus == GameState.PreWinAnimations && currentType == ItemsTypes.DiscoBall)
            {
                //Debug.Log($"Item: Conditions for not destroying the item are met. Exiting DestroyItem method. Position: {transform.position}");
                StopDestroy();
                return;
            }
            // if ((explodedItem?.currentType == ItemsTypes.PACKAGE || explodedItem?.currentType == ItemsTypes.MULTICOLOR)&& currentType == ItemsTypes.MULTICOLOR)
            // {
            //     //Debug.Log($"Item: Conditions for not destroying the item are met. Exiting DestroyItem method. multicolor hit by bomb , exploded item is {explodedItem}");
            //     StopDestroy();
            //     return;
            // }

            if (currentType == ItemsTypes.Gredient && square.nextRectangle != null)
            {
                Debug.Log(
                    "Item: Current item type is INGREDIENT and next square is not null. Exiting DestroyItem method. Position: " +
                    transform.position);
                return;
            }

            if (destroying)
            {
                //Debug.Log($"Item: Item is already being destroyed. Exiting DestroyItem method. ID {GetInstanceID()}, Position {transform.position}");
                return;
            }

            if (JustIntItem && CheckJustInItem)
            {
                //Debug.Log($"item: justCreated");
                return;
            }

            if (this == null)
            {
                //Debug.Log($"Item: Item is null. Exiting DestroyItem method. ID {GetInstanceID()}, Position {transform.position}");
                return;
            }

            if (currentType == ItemsTypes.Pots)
            {
                if (currentType == ItemsTypes.Pots)
                {
                    Debug.Log("Item: Spiral2 type detected. Handling non-spiral effects.");
                    var spiralEffect = ObjectPoolManager.Instance.GetPooledObject("FireworkSplashSpir2", this);
                    if (spiralEffect != null)
                    {
                        spiralEffect.transform.position = transform.position;
                        spiralEffect.GetComponent<SplashEffectParticles>().SetColor(0);
                    }
                }

                spiral2Counter++; // Increment counter

                if (spiral2Counter < 2)
                {
                    var spiralAnim = GetTopItemInterface().GetGameobject().GetComponent<FallingTarget>().animator;
                    spiralAnim.SetTrigger("Broke");
                    //Debug.Log($"Item: DestroyCor coroutine already started. Exiting DestroyItem method. ID {GetInstanceID()}, Position {transform.position}");
                    return; // Exit if counter hasn't reached 2
                }

                Debug.Log("Item: Spiral2 counter reached 2. Resetting counter.");
            }

            Debug.Log("Item: Stopping idle animation coroutine. Position: " + transform.position);
            StopCoroutine(AnimIdleStart());

            destroying = true;
            //Debug.Log($"Item: Marked as destroying. ID {GetInstanceID()}, Position {transform.position}");

            // LevelManager.THIS.FindMatches();

            if (MainManager.Instance.DebugSettings.DestroyLog)
                DebugLogManager.Log(
                    $"Item: start destroy - type {currentType}, nextType {NextType}, ID {GetInstanceID()}, Position {transform.position}",
                    DebugLogManager.LogType.Destroying);

            if (!destroycoroutineStarted)
            {
                //Debug.Log($"Item: Starting DestroyCor coroutine. ID {GetInstanceID()}, Position {transform.position} and destroyNeighboours is {destroyNeighbours}");
                destroycoroutineStarted = true; // Mark the coroutine as started

                // LevelManager.THIS.ItemEndsMatch();

                StartCoroutine(DestroyCor(showScore, particles, explodedItem, explEffect, bunch, WithoutShrink,
                    destroyNeighboours: destroyNeighbours, multicolorEffect: multicolorEffect,
                    isCombinedwithMulti: isCombinedwithMulti));
            }
            else
            {
                //Debug.Log($"Item: DestroyCor coroutine already started. Exiting DestroyItem method. ID {GetInstanceID()}, Position {transform.position}");
            }

        }

        private IEnumerator DestroyCor(bool showScore = false, bool particles = true, Item explodedItem = null,
            bool explEffect = false, int bunch = 0, bool WithoutShrink = false,
            bool destroyNeighboours = true, bool multicolorEffect = false, bool isCombinedwithMulti = false)
        {
            //Debug.Log($"sallog color = {color}");
            destroycoroutineStarted = true;

            if (explodedItem != null)
            {
                switchItem = explodedItem;
            }

            // Play sound (execute immediately if no pause is needed)
            //PlayDestroySound();

            // Handle explosion effect
            // HandleExplosionEffect(false);

            // Handle item destruction animations and particles
            if (particles)
            {
                if ((currentType == ItemsTypes.NONE && NextType == ItemsTypes.NONE) || currentType == ItemsTypes.Eggs)
                {
                    if (particles)
                    {
                        if (!WithoutShrink)
                        {
                            PlayDestroyAnimation("destroy");
                            //yield return new WaitWhile(() => !animationFinished);
                            yield return waitForSeconds;
                            yield return null;
                        }


                        // HideSprites(true);
                        animationFinished = false;
                    }

                    // Use object pooling for particle effects
                    if (currentType == ItemsTypes.Eggs)
                    {
                        var spiralEffect = ObjectPoolManager.Instance.GetPooledObject("FireworkSplashSpir", this);
                        if (spiralEffect != null)
                        {
                            spiralEffect.transform.position = transform.position;
                            spiralEffect.GetComponent<SplashEffectParticles>().SetColor(0);
                        }
                    }

                    if (currentType == ItemsTypes.Pots)
                    {
                        Debug.Log("Item: Spiral2 type detected. Handling non-spiral effects.");
                        var spiralEffect = ObjectPoolManager.Instance.GetPooledObject("FireworkSplashSpir2", this);
                        if (spiralEffect != null)
                        {
                            spiralEffect.transform.position = transform.position;
                            spiralEffect.GetComponent<SplashEffectParticles>().SetColor(0);
                        }
                    }
                    else
                    {
                        HandleNonSpiralEffects(multicolorEffect);
                    }
                }
            }

            // Handle plus time object
            // HandlePlusTimeObject();

            // Show score popup
            if (showScore)
            {
                MainManager.Instance.ShowPopupScore(scoreForItem, transform.position, color);
            }

            if (currentType == ItemsTypes.Eggs || currentType == ItemsTypes.Pots)
            {
                // Update score
                MainManager.Score += scoreForItem;
                MainManager.Instance.CheckStars();
                MainManager.Instance.levelData.GetTargetObject().CheckItems(new[] { this });
            }

            this.IsBeingProcessedForDestruction = false; // Good practice just in case
            // Destroy item animation
            DestroyItemAnimation();

            this.colorableComponent.ActivateShadow(false);
            // Destroy the item using top item interface
            DestroyTopItemInterface(destroyNeighboours, color, isCombinedwithMulti);
        }

        public void MoveToTargetCor(Item targetItem)
        {
            MoveToTargetAnim(targetItem);
        }

        public void MoveToTargetAnim(Item targetItem, float distancePercentage = 0.7f, Action action = null)
        {
            //  this.destroying = true;
            // Validate parameters
            if (targetItem == null || targetItem.transform == null)
            {
                Debug.LogWarning("MoveToTargetAnim aborted: Target item is null or destroyed.");
                return;
            }

            // Clamp distance percentage
            distancePercentage = Mathf.Clamp01(distancePercentage);

            // Create a copy of the sprite
            GameObject spriteCopy = Instantiate(colorableComponent.directSpriteRenderer?.gameObject, transform.position,
                Quaternion.identity);
            if (spriteCopy == null)
            {
                Debug.LogWarning("MoveToTargetAnim aborted: Sprite copy could not be created.");
                return;
            }

            // Configure sprite copy
            spriteCopy.transform.localScale = new Vector3(0.42f, 0.42f, 0);

            // Disable the original sprite
            if (colorableComponent.directSpriteRenderer != null)
            {
                colorableComponent.directSpriteRenderer.enabled = false;
            }

            // Declare tween before assigning it
            Tweener tween = null;

            // Assign the tween with animation
            tween = spriteCopy.transform.DOMove(targetItem.transform.position, 0.1f)
                .SetEase(DG.Tweening.Ease.InOutQuad)
                .OnUpdate(() =>
                {
                    if (tween != null && tween.IsActive() && tween.ElapsedPercentage() > distancePercentage)
                    {
                        tween.Kill(); // Stop animation early
                        if (spriteCopy != null) Destroy(spriteCopy);
                        action?.Invoke();
                    }
                })
                .OnComplete(() =>
                {
                    if (spriteCopy != null) Destroy(spriteCopy);
                    action?.Invoke();
                });
        }

        private void DestroyCopySprite()
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PlayDestroySound()
        {
            var soundInstance = CentralSoundManager.Instance;
            if (soundInstance != null)
            {
                var destroySounds = soundInstance.destroy;
                soundInstance.PlayOneShot(destroySounds[Random.Range(0, destroySounds.Length)]);
            }

            Debug.Log("PlayDestroySound: Played destroy sound. Position: " + transform.position);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void HandleExplosionEffect(bool explEffect)
        {
            //Debug.Log($"Item: HandleExplosionEffect >> expel is {explEffect} and globalexpl is {globalExplEffect}");
            // if (explEffect || globalExplEffect)
            // {
            //     Debug.LogError("HandleExplosionEffect");
            //     var explosionPrefab = Resources.Load<GameObject>("Prefabs/Effects/Replace");
            //     if (explosionPrefab != null)
            //     {
            //         var partcl1 = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
            //         Destroy(partcl1, 1f);
            //     }
            // }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void HandlePlusTimeObject()
        {
            if (MainManager.Instance.levelData.limitType == LIMIT.TIME && plusTimeObj != null)
            {
                plusTimeObj.GetComponent<BonusFive>()?.Destroy();
            }
        }

        private static readonly WaitForSeconds waitForSeconds = new WaitForSeconds(0.01f);


        // [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void HandleNonSpiralEffects(bool multicolorEffect)
        {
            if (!multicolorEffect)
            {
                var packageExpl = ObjectPoolManager.Instance.GetPooledObject("Simpleparticle", this);
                if (packageExpl != null)
                {
                    packageExpl.transform.position = transform.position;
                }
            }
            else
            {
                var packageExpl = ObjectPoolManager.Instance.GetPooledObject("Simpleparticle", this);
                if (packageExpl != null)
                {
                    packageExpl.transform.position = transform.position;
                }
            }

            // Pool the color-specific particle based on the color index
            string particleName = $"FireworkSplashColor{color}";
            var fireworkSplash = ObjectPoolManager.Instance.GetPooledObject(particleName, this);
            if (fireworkSplash != null)
            {
                fireworkSplash.transform.position = transform.position;
                if (fireworkSplash.TryGetComponent<SplashEffectParticles>(out var splashParticles))
                {
                    splashParticles.SetColor(0);
                    splashParticles.RandomizeParticleSeed();
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DestroyItemAnimation()
        {
            var itemAnim = itemAnimTransform.GetComponent<Animator>();
            if (itemAnim != null)
            {
                Destroy(itemAnim);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DestroyTopItemInterface(bool destroyNeighboours = true, int color = 0,
            bool isCombinedwithMulti = false)
        {
            var topItem = GetTopItemInterface();
            if (topItem != null)
            {
                //Debug.Log($"sall top interface {topItem}");
                topItem.Destroy(this, null, destroyNeighboours: destroyNeighboours, color: color, isCombinedwithMulti: isCombinedwithMulti);
            }
        }

        public void StopDestroy()
        {
            if (MainManager.Instance.DebugSettings.DestroyLog)
                DebugLogManager.Log("Stop destroy " + GetInstanceID(), DebugLogManager.LogType.Destroying);
            destroying = false;
            destroyNext = false;
            destroycoroutineStarted = false;
        }

        private void PlayDestroyAnimation(string anim_name)
        {
            if (anim != null && anim.runtimeAnimatorController != null)
            {
                Debug.Log("Item: sallog  anim.SetTrigger(anim_name);");
                anim.SetTrigger("destroy");
            }
            else
            {
                Debug.LogWarning("Animator or AnimatorController not set for destruction animation.");
            }
        }

        public void SmoothDestroy()
        {
            //Debug.Log($"Item: SmoothDestroy {this}");
            if (gameObject.activeSelf)
                StartCoroutine(SmoothDestroyCor());
        }

        public void NoAnimSmoothDestroy()
        {
            //Debug.Log($"Item: NoAnimSmoothDestroy {this}");
            if (gameObject.activeSelf)
                NoAnimSmoothDestroyCor();
        }

        private IEnumerator SmoothDestroyCor()
        {
            anim.SetTrigger("destroy");

            yield return new WaitForSeconds(0.6f);
            square.Item = null;
            HideSprites(true);
            DestroyBehaviour();
        }

        private void NoAnimSmoothDestroyCor()
        {
            //  anim.SetTrigger("destroy");
            // if (currentType == ItemsTypes.MULTICOLOR)
            // {
            //     //var partcl2 = ObjectPooler.Instance.GetPooledObject("FireworkSplashMulticolor", this);
            //     //partcl2.transform.position = transform.position;
            // }

            //yield return new WaitForSeconds(0.0f);
            square.Item = null;
            //HideSprites(true);
            DestroyBehaviour();
        }

        #endregion

        public Sprite GetSprite()
        {
            return GetComponent<SpriteRenderer>() != null
                ? GetComponent<SpriteRenderer>().sprite
                : transform.GetComponentInChildren<SpriteRenderer>()?.sprite;
        }

        public SpriteRenderer[] GetSpriteRenderers()
        {
            return SprRenderer.WhereNotNull().ToArray();
        }

        public SpriteRenderer GetSpriteRenderer()
        {
            return SprRenderer.FirstOrDefault();
        }

        public FieldBoard GetField()
        {
            return field;
        }

        public class WayMakerPoint
        {
            public Vector3 destPosition;
            public Rectangle Rectangle;
            public bool instant;

            public WayMakerPoint(Vector3 vector, Rectangle rectangle)
            {
                destPosition = vector;
                Rectangle = rectangle;
                if (Rectangle?.teleportOrigin != null) instant = true;
            }
        }

        public Item DeepCopy()
        {
            var other = (Item)MemberwiseClone();
            return other;
        }

        public void OnColorChanged(int color)
        {
            COLOR = color;
        }

        public GameObject GetChopperTarget
        {
            get => ChopperTarget;
            set => ChopperTarget = value;
        }

        public GameObject GetGameObject => gameObject;
        public Item GetItem => this;
        public int combinationID { get; set; }

        public int TargetByChopperIndex
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public bool IsBottom() => square != null && square.IsBottom();

        LevelTargetTypes ISquareItemCommon.GetType()
        {
            // throw new NotImplementedException();
            return LevelTargetTypes.NONE;
        }

        public void DestroyByStriped(bool WithoutShrink = false, bool destroyNeighbours = false)
        {
            //throw new NotImplementedException();
            DestroyItem(destroyNeighbours: false, WithoutShrink: true);
        }
    }

    [Serializable]
    public class SpawnObjCounter
    {
        public int SpawnAmount;
    }
}
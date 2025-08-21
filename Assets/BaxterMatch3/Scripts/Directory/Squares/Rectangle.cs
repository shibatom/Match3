

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Internal.Scripts.System;
using UnityEngine;
using Random = UnityEngine.Random;
using DG.Tweening;
using Internal.Scripts.Effects;
using Internal.Scripts.Items;
using Internal.Scripts.Items.Interfaces;
using Internal.Scripts.Level;
using Internal.Scripts.Spawner;
using Internal.Scripts.System.Pool;
using Internal.Scripts.TargetScripts.TargetSystem;
using UnityEngine.Serialization;

namespace Internal.Scripts.Blocks
{
    public enum LevelTargetTypes
    {
        NONE = 0,
        EmptySquare = 1,
        Grass = 2,
        ExtraTargetType1 = 3,
        BreakableBox = 4,
        GrowingGrass = 5,
        ExtraTargetType2 = 6,
        Eggs = 7,
        Pots = 8,
        ExtraTargetType3 = 9,
        ExtraTargetType4 = 10,
        ExtraTargetType5 = 11,
        ExtraTargetType6 = 12,
        PlateCabinet = 13,
        GrassType2 = 14,
        Mails = 15,
        PotionCabinet = 16,
        HoneyBlock = 17
    }

    //Added_feature
    public enum Spawners
    {
        None,
        Eggs,
        Pots,
        TimeBomb,
        Ingredient,
        Rocket_Horizontal,
        Rocket_Vertical,
        Chopper,
        DiscoBall
    }

    public enum FindSeparating
    {
        None = 0,
        Horizontal,
        Vertical
    }

    /// <summary>
    /// Handles square behaviour like destroying, generating item if the square empty, determines items directions, and obstacles behaviour
    /// </summary>
    public class Rectangle : MonoBehaviour, ISquareItemCommon, IField, IChopperTargetable, IDestroyPipelineStripedShow
    {
        [Header("Score for destroying")] public int score;

        /// Item occupies this square
        public Item item;

        Coroutine _checkNullItem;

        public Item Item
        {
            get { return item; }
            set
            {
                if (value == null)
                {
                    if (MainManager.Instance.DebugSettings.FallingLog) DebugLogManager.Log(item + " set square " + name + " empty ", DebugLogManager.LogType.Falling);
                }
                else
                {
                    if (MainManager.Instance.DebugSettings.FallingLog) DebugLogManager.Log(value + " set square " + name, DebugLogManager.LogType.Falling);
                }

                item = value;
                if (item != null)
                {
                    if (GetSubSquare().aboveItem && (item.GetSpriteRenderer().sortingOrder >= GetSpriteRenderer().sortingOrder ||
                                                     subSquares.Count > 1 && subSquares[^2].GetComponent<SpriteRenderer>().sortingOrder >= item.GetSpriteRenderer().sortingOrder))
                    {
                        GetSpriteRenderer().sortingOrder += 2;
                        item.GetComponent<IItemInterface>().SetOrder(GetSpriteRenderer().sortingOrder - 2);
                    }
                    else if (!GetSubSquare().aboveItem && item.GetSpriteRenderer().sortingOrder <= GetSpriteRenderer().sortingOrder)
                    {
                        item.GetComponent<IItemInterface>().SetOrder(GetSpriteRenderer().sortingOrder + 1);
                    }
                }

                if (item == null && gameObject.activeSelf && isEnterPoint && MainManager.GetGameStatus() != GameState.RegenLevel)
                    StartCoroutine(CheckNullItem());
            }
        }

        /// position on the field
        public int row;

        public int col;

        /// sqaure type
        public LevelTargetTypes type;

        /// sprite of border
        private Sprite borderSprite;

        [Header("can item move inside the square")]
        public bool canMoveIn;

        [Header("can item fall out of the square")]
        public bool canMoveOut;

        [Header("can item gen into square")] public bool cantGenIn;

        [Header("cannot be destroyed by Item")]
        public bool cannotBeDestroy;

        [HideInInspector] public bool undestroyable;
        [Header("Size in squares")] public Vector2Int sizeInSquares = Vector2Int.one;
        [Header("Block overlap an item")] public bool aboveItem;

        public Animator animator;

        /// EDITOR: direction of items
        [HideInInspector] public Vector2 direction;

        /// EDITOR: true - square is begging of the items flow
        [HideInInspector] public bool isEnterPoint;

        ///EDITOR: enter square for current sequence of the squares, Sequence is array of squares along with direction (all squares by column)
        [FormerlySerializedAs("enterSquare")] [HideInInspector]
        public Rectangle enterRectangle;

        /// Next square by direction flow
        [FormerlySerializedAs("nextSquare")] [HideInInspector]
        public Rectangle nextRectangle;

        /// teleport destination position
        [HideInInspector] public Vector2Int teleportDestinationCoord;

        /// teleport destionation square
        [HideInInspector] public Rectangle teleportDestination;

        /// teleport started square
        [HideInInspector] public Rectangle teleportOrigin;

        /// current field
        [HideInInspector] public FieldBoard field;

        /// mask for the top squares
        public GameObject mask;

        /// teleport effect gameObject
        public GameObject teleportEffect;

        /// teleport square object
        public TeleportHandler teleport;

        [FormerlySerializedAs("mainSquareInGrid")]
        public Rectangle mainRectangleInGrid; // main square in grid for big blocks

        //Add_feature
        [HideInInspector] public bool isSpawnerPoint;
        [HideInInspector] public SingularSpawn SpawnerType;
        [HideInInspector] public float SpawnPersentage = 0;
        [HideInInspector] public float SpawnPersentage_2 = 0;
        [HideInInspector] public bool isHoneyBlock = false;
        Spawners Prev = Spawners.None;
        public GameObject SpawnerObject;
        private Disperser Spawner;
        public Rectangle boxParent;
        public int boxChildPos;

        public Rectangle[] boxChilds;
        public int hitCount;

        /// subsquares of the current square, obstacle or TargetType2
        public List<Rectangle> subSquares = new List<Rectangle>();

        /// if true - this square has enter square upper by items flow
        [HideInInspector] public bool linkedEnterSquare;

        private void Awake() //init some objects
        {
            GetInstanceID();
            border = Resources.Load<GameObject>("Border");
        }

        // Use this for initialization
        void Start() //init some objects
        {
            if (mainRectangle == null)
                name = "Square_" + col + "_" + row;
            if ((LevelData.THIS.IsTargetByActionExist(CollectingTypes.ReachBottom) || direction != Vector2.down) && enterRectangle && type != LevelTargetTypes.NONE)
            {
                if (orderInSequence == 0 && field.fieldData.levelSquares[row * field.fieldData.maxCols + col].separatorIndexes[3] == false)
                    CreateArrow(isEnterPoint);
                else if (direction != Vector2.down && isEnterPoint)
                    CreateArrow(isEnterPoint);
            }

            _isSquareToClear = MainManager.Instance.levelData.IsTargetByActionExist(CollectingTypes.Clear) && MainManager.Instance.levelData.GetTargetSprites().Any(i => i.name ==
                GetComponent<SpriteRenderer>().sprite.name);

            isHoneyBlock = type == LevelTargetTypes.HoneyBlock;
        }

        /// <summary>
        /// Create animated arrow for bottom row
        /// </summary>
        /// <param name="enterPoint"></param>
        void CreateArrow(bool enterPoint)
        {
            var obj = Instantiate(Resources.Load("Prefabs/Arrow")) as GameObject;
            obj.transform.SetParent(transform);
            obj.transform.localScale = Vector3.one;
            obj.transform.localPosition = Vector3.zero + Vector3.down * 0.5f;
            if (enterPoint)
                obj.transform.localPosition = Vector3.zero - Vector3.down * 2f;
            var angle = Vector3.Angle(Vector2.down, direction);
            angle = Mathf.Sign(Vector3.Cross(Vector2.down, direction).z) < 0 ? (360 - angle) % 360 : angle;
            Vector2 pos = obj.transform.localPosition;
            pos = Quaternion.Euler(0, 0, angle) * pos;
            obj.transform.localPosition = pos;
            obj.transform.rotation = Quaternion.Euler(0, 0, angle);
            ParticleSystem.MainModule mainModule = obj.GetComponent<ParticleSystem>().main;
            mainModule.startRotation = -angle * Mathf.Deg2Rad;
            if (teleportDestination == null && teleportOrigin == null && !enterPoint)
                bottomRow = true;
        }

        /// <summary>
        /// Check is the square is empty
        /// </summary>
        /// <returns></returns>
        IEnumerator CheckNullItem()
        {
            GenItem();
            yield return new WaitForEndOfFrame();
        }

        //Added_feature
        private bool SpawnOneByOne = false;

        private enum IngredientType
        {
            ingredient_01,
            ingredient_02
        }

        private IngredientType CurrentIngredientType = IngredientType.ingredient_01;


        //two function for creating the spawners like genitem
        //gen specific ingredient (only one ingredient spawn)
        private void GenIngredientOneByOne(IngredientType ingredientType)
        {
            //Check for item in field
            var allIngredientTargets = MainManager.Instance.levelData.GetTargetCounters()
                .Where(i => i.collectingAction == CollectingTypes.ReachBottom && !i.IsTotalTargetReached()).ToArray();
            if (allIngredientTargets.Length == 0)
            {
                GenItem();
                return;
            }

            TargetCounter selectedTarget = null;
            if (allIngredientTargets.Length > 0)
            {
                //selectedTarget = allIngredientTargets[Random.Range(0, allIngredientTargets.Length)];
                var TargetsList = allIngredientTargets.Where(i => i.extraObject.name == "" + ingredientType).ToList();
                if (TargetsList.Count > 0)
                    selectedTarget = TargetsList[0];
            }

            var restCount = selectedTarget?.count ?? 0;
            var fieldCount = MainManager.Instance.field.GetItems()?.Where(i => selectedTarget != null && selectedTarget.extraObjects.Any(x => x == i.GetSprite()))?.Count() ?? 0;
            fieldCount += MainManager.Instance.animateItems.Where(i => selectedTarget != null && selectedTarget.extraObjects.Any(x => x == i.GetComponent<SpriteRenderer>().sprite)).Count();
            //var spawnAmount = selectedTarget?.targetPrefab.GetComponent<Item>().SpawnAmount.SpawnAmount ?? 0;

            if (fieldCount == 0)
            {
                if (!IsBottom())
                {
                    if (restCount - fieldCount > 0)
                    {
                        GenItem(itemType: ItemsTypes.Gredient);
                    }
                    else
                    {
                        GenItem();
                    }
                }
                else
                {
                    GenItem();
                }
            }
            else
            {
                GenItem();
            }
        }

        //gen random among any ingredient (if two ingredient spawn)
        private void GenIngredientOneByOne()
        {
            //Check for item in field
            var allIngredientTargets = MainManager.Instance.levelData.GetTargetCounters()
                .Where(i => i.collectingAction == CollectingTypes.ReachBottom && !i.IsTotalTargetReached()).ToArray();
            if (allIngredientTargets.Length == 0)
            {
                GenItem();
                return;
            }

            TargetCounter selectedTarget = null;
            if (allIngredientTargets.Length > 0)
            {
                selectedTarget = allIngredientTargets[Random.Range(0, allIngredientTargets.Length)];
                CurrentIngredientType = (IngredientType)Enum.Parse(typeof(IngredientType), selectedTarget.extraObject.name);
                //var TargetsList = allIngredientTargets.ToList();
                //if (TargetsList.Count > 0)
                //    selectedTarget = TargetsList[Random.Range(0, allIngredientTargets.Length)];
            }

            var restCount = selectedTarget?.count ?? 0;
            var fieldCount = MainManager.Instance.field.GetItems()?.Where(i => selectedTarget != null && selectedTarget.extraObjects.Any(x => x == i.GetSprite()))?.Count() ?? 0;
            fieldCount += MainManager.Instance.animateItems.Where(i => selectedTarget != null && selectedTarget.extraObjects.Any(x => x == i.GetComponent<SpriteRenderer>().sprite)).Count();
            //var spawnAmount = selectedTarget?.targetPrefab.GetComponent<Item>().SpawnAmount.SpawnAmount ?? 0;

            if (fieldCount == 0)
            {
                if (!IsBottom())
                {
                    if (restCount - fieldCount > 0)
                    {
                        GenItem(itemType: ItemsTypes.Gredient);
                    }
                    else
                    {
                        GenItem();
                    }
                }
                else
                {
                    GenItem();
                }
            }
            else
            {
                GenItem();
            }
        }


        private Spawners getSpawnersProbability(SingularSpawn singularSpawn)
        {
            int SpawnersCount = 0;
            Spawners res = Spawners.None;
            Dictionary<float, Spawners> resValue = new Dictionary<float, Spawners>()
            {
                { (singularSpawn.SpawnPersentage == 0) ? (singularSpawn.SpawnPersentage + 0.0001f) : (singularSpawn.SpawnPersentage - 0.0001f), singularSpawn.SpawnersType },
                { singularSpawn.SpawnPersentage_2, singularSpawn.SpawnersType_2 }
            };

            foreach (KeyValuePair<float, Spawners> item in resValue.OrderBy(key => key.Value))
            {
                //Debug.Log("Key: " + author.Key + " , Value: " + author.Value);
                if (item.Value != Spawners.None)
                    SpawnersCount++;
            }

            float Rand = Random.value;

            if (Rand < resValue.Last().Key)
            {
                res = resValue.Last().Value;
            }
            else if (Rand < resValue.First().Key + resValue.Last().Key)
            {
                res = resValue.First().Value;
            }

            if (SpawnersCount >= 2)
                if (res == Prev)
                {
                    res = Spawners.None;
                }
                else
                {
                    Prev = res;
                }

            return res;
        }

        /// <summary>
        /// set mask for top squares
        /// </summary>
        public void SetMask()
        {
            if (isEnterPoint || teleportOrigin)
            {
                var m = Instantiate(mask, transform.position + Vector3.up * (field.squareHeight + 3.05f), Quaternion.identity, transform);
                var angle = Vector3.Angle(Vector2.down, direction);
                angle = Mathf.Sign(Vector3.Cross(Vector2.down, direction).z) < 0 ? (360 - angle) % 360 : angle;
                Vector2 pos = m.transform.localPosition;
                pos = Quaternion.Euler(0, 0, angle) * pos;
                m.transform.localPosition = pos;
                m.transform.rotation = Quaternion.Euler(0, 0, angle);
            }
        }

        /// <summary>
        /// Generate new item
        /// </summary>
        /// <returns>The item.</returns>
        /// <param name="falling">If set to <c>true</c> prepare for falling animation.</param>
        /// <param name="itemType"></param>
        /// <param name="color"></param>
        /// <param name="squareBlockItem"></param>
        public Item GenItem(bool falling = true, ItemsTypes itemType = ItemsTypes.NONE, int color = -1, EditorItems squareBlockItem = null, bool noAnim = false)
        {
            // Debug.Log("Square/GenItem: CanGenInto() >> " + CanGenInto());

            if ((IsNone() && !CanGoInto()))
                return null;

            if (itemType == ItemsTypes.NONE && CanGenInto())
                return null;

            GameObject item = null;

            if (itemType == ItemsTypes.NONE)
            {
                if (LevelData.THIS.IsTargetByActionExist(CollectingTypes.ReachBottom) && !IsObstacle() &&
                    (!falling && !field.IngredientsByEditor || (falling && field.IngredientsByEditor)))
                {
                    var wantsToSetIngredientOnLevelDefault =
                        (MainManager.Instance.levelData.generateIngredientOnlyFromSpawner && MainManager.Instance.levelData.SpawnerExits) ? false : true;

                    if (wantsToSetIngredientOnLevelDefault)
                    {
                        item = GetIngredientItem();
                        // Debug.Log("Square/GenItem: Generated ingredient item.");
                    }
                }

                if (!item)
                {
                    item = ObjectPoolManager.Instance.GetPooledObject("Item", this);
                    // Debug.Log("Square/GenItem: Pooled generic item.");
                }

                if (MainManager.Instance.DebugSettings.FallingLog)
                {
                    DebugLogManager.Log(item + " type by " + itemType, DebugLogManager.LogType.Falling, true);
                }
            }
            else
            {
                item = ObjectPoolManager.Instance.GetPooledObject(itemType.ToString(), this);
                // Debug.Log("Square/GenItem: Pooled item of type " + itemType);

                if (!noAnim)
                {
                    if (itemType == ItemsTypes.DiscoBall)
                    {
                        RemoveComponent<In_GameBlocker>(item);
                        var multicolor = item.GetComponent<Item>().GetTopItemInterface().GetGameobject().GetComponent<DiscoBallItem>();
                        multicolor.skeletonAnimation.gameObject.SetActive(true);
                        // multicolor.skeletonAnimation.AnimationState.SetAnimation(0, "appear", false);
                        // Debug.Log("Square/GenItem: Set multicolor item animation.");
                        MeshRenderer skeletonRenderer;
                        skeletonRenderer = multicolor.skeletonAnimation.GetComponent<MeshRenderer>();


                        if (skeletonRenderer != null)
                        {
                            // Set the layer in the SkeletonAnimation's Skeleton
                            skeletonRenderer.sortingLayerName = "Spine";
                            skeletonRenderer.sortingOrder = 49;
                        }
                        else
                        {
                            Debug.LogWarning("SkeletonAnimation component not found on object.");
                        }
                    }
                    else if (itemType == ItemsTypes.Chopper)
                    {
                        var chopper = item.GetComponent<Item>().GetTopItemInterface().GetGameobject().GetComponent<ChopperItem>();

                        chopper.gameObject.SetActive(true);
                        chopper.HeliAnimationSpine.gameObject.SetActive(true);
                        chopper.choppers[0].transform.localScale = Vector3.one;
                        if (chopper.choppers.Count > 1)
                            chopper.choppers[1].transform.localScale = Vector3.one;
                        chopper.transform.GetChild(0).transform.GetChild(0).localScale = Vector3.one;

                        MeshRenderer skeletonRenderer;
                        skeletonRenderer = chopper.HeliAnimationSpine.GetComponent<MeshRenderer>();


                        if (skeletonRenderer != null)
                        {
                            // Set the layer in the SkeletonAnimation's Skeleton
                            skeletonRenderer.sortingLayerName = "Spine";
                            skeletonRenderer.sortingOrder = 27;
                        }
                        else
                        {
                            Debug.LogWarning("SkeletonAnimation component not found on object.");
                        }
                    }
                    else if (itemType == ItemsTypes.RocketHorizontal || itemType == ItemsTypes.RocketVertical)
                    {
                        RemoveComponent<In_GameBlocker>(item);
                        // item.GetComponent<Item>().gameObject.transform.GetChild(0).transform.localScale = new Vector3 (1.15f,1.15f,0);
                        var stripe = item.GetComponent<Item>().GetTopItemInterface().GetGameobject().GetComponent<RocketItem>();
                        stripe.skeletonAnimation.gameObject.SetActive(true);
                        stripe.skeletonAnimation.AnimationState.SetAnimation(0, "In", false);
                        stripe.spriteRenderer.enabled = false;

                        //item?.GetComponent<Item>().anim?.SetTrigger("bonus_appear");
                        // Debug.Log("Square/GenItem: Set stripe animation.");
                    }
                    else if (itemType == ItemsTypes.Bomb)
                    {
                        RemoveComponent<In_GameBlocker>(item);
                        var package = item.GetComponent<Item>().GetTopItemInterface().GetGameobject().GetComponent<BombItem>();
                        package.PackageAnimator.gameObject.SetActive(true);
                        package.PackageAnimator.enabled = true;

                        //bomb.spriteRenderer.enabled = false;
                        // bomb.spriteRenderer2.enabled = false;
                    }
                }
            }

            if (item == null)
            {
                if (squareBlockItem != null)
                {
                    item = ObjectPoolManager.Instance.GetPooledObject(squareBlockItem.Item.name);
                    // Debug.Log("Square/GenItem: Pooled square block item.");
                }

                if (itemType == ItemsTypes.Gredient && MainManager.Instance.levelData.SpawnerExits)
                {
                    var ingredientType = (CurrentIngredientType == IngredientType.ingredient_01) ? "Ingredient1" : "Ingredient2";
                    item = ObjectPoolManager.Instance.GetPooledObject(ingredientType);
                    // Debug.Log("Square/GenItem: Pooled ingredient item of type " + ingredientType);
                }

                if (item == null)
                {
                    Debug.LogError("Square/GenItem: There is no " + itemType + " in pool.");
                    return null;
                }
            }

            item.transform.localScale = Vector2.one * 0.42f;
            // item.GetComponent<Item>().GetSpriteRenderer().transform.localScale = Vector3.one;
            Item itemComponent = item.GetComponent<Item>();

            if (MainManager.Instance.levelData.GetTargetCounters().Any(i =>
                    (i.collectingAction == CollectingTypes.Destroy || i.collectingAction == CollectingTypes.ReachBottom) && i.extraObject == itemComponent.GetSpriteRenderer().sprite))
            {
                item.AddComponent<TargetComponent>();
                // Debug.Log("Square/GenItem: Added TargetComponent to item.");
            }

            itemComponent.square = this;
            itemComponent.CheckPlusFive();
            ColorReciever colorReciever = item.GetComponent<ColorReciever>();

            if (color == -1)
            {
                itemComponent.GenColor(ColorGettable);
                // Debug.Log("Square/GenItem: Generated color for item.");
            }
            else if (colorReciever != null)
            {
                colorReciever.SetColor(color);
                // Debug.Log("Square/GenItem: Set item color to " + color);
            }

            itemComponent.field = field;
            itemComponent.needFall = falling;

            if (!falling)
            {
                item.transform.position = (Vector3)((Vector2)transform.position) + Vector3.back * 0.2f;
                // Debug.Log("Square/GenItem: Set item position without falling.");
            }

            if (MainManager.Instance.gameStatus != GameState.Playing && MainManager.Instance.gameStatus != GameState.Tutorial)
            {
                Item = itemComponent;
            }
            else if (!IsHaveFallingItemsAbove())
            {
                Item = itemComponent;
            }

            if (falling)
            {
                Vector3 startPos = GetReverseDirection();
                item.transform.position = transform.position + startPos * field.squareHeight + Vector3.back * 0.2f;
                itemComponent.JustCreatedItem = true;
                // Debug.Log("Square/GenItem: Set item position for falling.");
            }
            else
            {
                itemComponent.JustCreatedItem = false;
            }

            if (MainManager.Instance.DebugSettings.DestroyLog)
            {
                DebugLogManager.Log(name + " gen item " + item.name + " pos " + item.transform.position, DebugLogManager.LogType.Destroying);
            }

            itemComponent.needFall = falling;
            itemComponent.StartFallingTo(itemComponent.GenerateWaypoints(this));
            // this is the last commit before bug
            //this is the bug
            // Debug.Log("Square/GenItem: Started falling to waypoints.");

            return itemComponent;
        }

        //         / <summary>
        // / Generate a new item.
        // / </summary>
        // / <param name="falling">If set to <c>true</c>, prepare for falling animation.</param>
        // / <param name="itemType">The type of the item.</param>
        // / <param name="color">The color of the item.</param>
        // / <param name="squareBlockItem">Optional block item for the square.</param>
        // / <param name="noAnim">If set to <c>true</c>, disable animations.</param>
        // / <returns>The generated item, or null if generation fails.</returns>
        public Item newGenItem(bool falling = true, ItemsTypes itemType = ItemsTypes.NONE, int color = -1, EditorItems squareBlockItem = null, bool noAnim = false)
        {
            // Debug.Log($"Square/GenItem: CanGenInto() >> {CanGenInto()}");

            if ((IsNone() && !CanGoInto()) || (itemType == ItemsTypes.NONE && CanGenInto()))
                return null;

            GameObject item = GenerateItemObject(itemType, squareBlockItem, falling);

            if (item == null)
            {
                Debug.LogError($"Square/GenItem: There is no {itemType} in pool.");
                return null;
            }

            InitializeItem(item, falling, color, itemType, noAnim);

            return item.GetComponent<Item>();
        }

        private GameObject GenerateItemObject(ItemsTypes itemType, EditorItems squareBlockItem, bool falling)
        {
            if (itemType == ItemsTypes.NONE)
            {
                if (ShouldGenerateIngredient(falling))
                {
                    var ingredientItem = GetIngredientItem();
                    if (ingredientItem != null)
                    {
                        // Debug.Log("Square/GenItem: Generated ingredient item.");
                        return ingredientItem;
                    }
                }

                var genericItem = ObjectPoolManager.Instance.GetPooledObject("Item", this);
                // Debug.Log("Square/GenItem: Pooled generic item.");
                return genericItem;
            }

            if (squareBlockItem != null)
            {
                var blockItem = ObjectPoolManager.Instance.GetPooledObject(squareBlockItem.Item.name);
                // Debug.Log("Square/GenItem: Pooled square block item.");
                return blockItem;
            }

            return ObjectPoolManager.Instance.GetPooledObject(itemType.ToString(), this);
        }

        private bool ShouldGenerateIngredient(bool falling)
        {
            return LevelData.THIS.IsTargetByActionExist(CollectingTypes.ReachBottom) &&
                   !IsObstacle() &&
                   ((!field.IngredientsByEditor && !falling) ||
                    (field.IngredientsByEditor && falling));
        }

        private void InitializeItem(GameObject item, bool falling, int color, ItemsTypes itemType, bool noAnim)
        {
            SetItemTransform(item, falling);

            var itemComponent = item.GetComponent<Item>();
            itemComponent.square = this;
            itemComponent.field = field;
            itemComponent.needFall = falling;
            itemComponent.JustCreatedItem = falling;

            ConfigureItemAppearance(itemComponent, itemType, color);

            if (falling)
            {
                Vector3 startPos = GetReverseDirection();
                item.transform.position = transform.position + startPos * field.squareHeight + Vector3.back * 0.2f;
                itemComponent.StartFallingTo(itemComponent.GenerateWaypoints(this));
                // Debug.Log("Square/GenItem: Started falling to waypoints.");
            }

            HandleDebugLogs(item, itemComponent);
        }

        private void SetItemTransform(GameObject item, bool falling)
        {
            item.transform.localScale = Vector2.one * 0.42f;
            if (!falling)
            {
                item.transform.position = (Vector3)((Vector2)transform.position) + Vector3.back * 0.2f;
                // Debug.Log("Square/GenItem: Set item position without falling.");
            }
        }

        private void ConfigureItemAppearance(Item itemComponent, ItemsTypes itemType, int color)
        {
            if (itemType != ItemsTypes.NONE)
            {
                SetupSpecialItemAppearance(itemComponent, itemType);
            }

            ColorReciever colorReciever = itemComponent.GetComponent<ColorReciever>();
            if (color == -1)
            {
                itemComponent.GenColor(ColorGettable);
                // Debug.Log("Square/GenItem: Generated color for item.");
            }
            else if (colorReciever != null)
            {
                colorReciever.SetColor(color);
                // Debug.Log($"Square/GenItem: Set item color to {color}");
            }
        }

        private void SetupSpecialItemAppearance(Item itemComponent, ItemsTypes itemType)
        {
            switch (itemType)
            {
                case ItemsTypes.DiscoBall:
                    ConfigureMulticolorItem(itemComponent);
                    break;
                case ItemsTypes.Chopper:
                    ConfigureChopperItem(itemComponent);
                    break;
                case ItemsTypes.RocketHorizontal:
                case ItemsTypes.RocketVertical:
                    ConfigureStripedItem(itemComponent);
                    break;
                case ItemsTypes.Bomb:
                    ConfigurePackageItem(itemComponent);
                    break;
                default:
                    break;
            }
        }

        private void ConfigureMulticolorItem(Item itemComponent)
        {
            var multicolor = itemComponent.GetComponent<DiscoBallItem>();
            multicolor.skeletonAnimation.gameObject.SetActive(true);
            SetSortingLayer(multicolor.skeletonAnimation.GetComponent<MeshRenderer>(), "Spine", 27);
        }

        private void ConfigureChopperItem(Item itemComponent)
        {
            var chopper = itemComponent.GetComponent<ChopperItem>();
            chopper.HeliAnimationSpine.gameObject.SetActive(true);
            SetSortingLayer(chopper.HeliAnimationSpine.GetComponent<MeshRenderer>(), "Spine", 27);
        }

        private void ConfigureStripedItem(Item itemComponent)
        {
            var stripe = itemComponent.GetComponent<RocketItem>();
            stripe.skeletonAnimation.gameObject.SetActive(true);
            stripe.skeletonAnimation.AnimationState.SetAnimation(0, "In", false);
        }

        private void ConfigurePackageItem(Item itemComponent)
        {
            var package = itemComponent.GetComponent<BombItem>();
            package.PackageAnimator.enabled = true;
        }

        private void SetSortingLayer(MeshRenderer renderer, string layerName, int order)
        {
            if (renderer != null)
            {
                renderer.sortingLayerName = layerName;
                renderer.sortingOrder = order;
            }
            else
            {
                Debug.LogWarning("MeshRenderer component not found on object.");
            }
        }

        private void HandleDebugLogs(GameObject item, Item itemComponent)
        {
            if (MainManager.Instance.DebugSettings.DestroyLog)
            {
                DebugLogManager.Log($"{name} gen item {item.name} pos {item.transform.position}", DebugLogManager.LogType.Destroying);
            }
        }


        /// <summary>
        /// Generates ingredient
        /// </summary>
        /// <returns></returns>
        private GameObject GetIngredientItem()
        {
            GameObject item = null;
            var allIngredientTargets = MainManager.Instance.levelData.GetTargetCounters()
                .Where(i => i.collectingAction == CollectingTypes.ReachBottom && !i.IsTotalTargetReached()).ToArray();
            if (allIngredientTargets.Length == 0) return item;
            TargetCounter selectedTarget = null;
            if (allIngredientTargets.Length > 0)
            {
                selectedTarget = allIngredientTargets[Random.Range(0, allIngredientTargets.Length)];
            }

            var restCount = selectedTarget?.count ?? 0;
            var fieldCount = MainManager.Instance.field.GetItems()?.Where(i => selectedTarget != null && selectedTarget.extraObjects.Any(x => x == i.GetSprite()))?.Count() ?? 0;
            fieldCount += MainManager.Instance.animateItems.Where(i => selectedTarget != null && selectedTarget.extraObjects.Any(x => x == i.GetComponent<SpriteRenderer>().sprite)).Count();
            var spawnAmount = selectedTarget.targetPrefab.GetComponent<Item>().SpawnAmount.SpawnAmount;
            if (fieldCount < spawnAmount)
            {
                if (!IsBottom())
                {
                    if (restCount - fieldCount > 0)
                    {
                        if (Random.Range(0, MainManager.Instance.levelData.limit / 3) == 0)
                        {
                            item = ObjectPoolManager.Instance.GetPooledObject(selectedTarget.targetPrefab.name, this);
                            item.GetComponent<Item>().SprRenderer[0].sprite = selectedTarget.extraObject;
                        }
                    }
                }
            }

            return item;
        }


        /// <summary>
        /// set square with teleport
        /// </summary>
        public void SetTeleports()
        {
            if (teleportDestinationCoord != Vector2Int.one * -1)
            {
                var sq = field.GetSquare(teleportDestinationCoord);
                teleportDestination = sq;
                sq.teleportOrigin = this;
            }
        }

        /// set square with teleport
        public void CreateTeleports()
        {
            if (teleportDestination != null || teleportOrigin != null)
            {
                teleport = Instantiate(teleportEffect).GetComponent<TeleportHandler>();
                teleport.transform.SetParent(transform);
                teleport.SetTeleport(teleportDestination != null && teleportOrigin == null);
                var pos = new Vector2(0, -0.92f);
                var angle = Vector3.Angle(Vector2.down, direction);
                angle = Mathf.Sign(Vector3.Cross(Vector2.down, direction).z) < 0 ? (360 - angle) % 360 : angle;
                if (teleportOrigin != null) angle += 180;
                pos = Quaternion.Euler(0, 0, angle) * pos;
                teleport.transform.localPosition = pos;
                teleport.transform.rotation = Quaternion.Euler(0, 0, angle);
                CreateArrow(teleportOrigin);
            }
        }

        //Added_feature
        public void CreateSpawner()
        {
            if (SpawnerType.SpawnersType != Spawners.None)
            {
                Spawner = Instantiate(MainManager.Instance.dispenserSpawner).GetComponent<Disperser>();
                Spawner.transform.SetParent(transform);
                Spawner.transform.localScale = new Vector3(1.6f, 1.6f, 1);
                Spawner.transform.localPosition = new Vector3(0, 1.6f, 0);
                Spawner.spawn = SpawnerType;
                Spawner.SetSpawnRenderer(SpawnerType);
                Spawner.SetRotation(SpawnerType.rotationType);
            }
        }

        /// <summary>
        /// set square direction on field creation
        /// </summary>
        public void SetDirection()
        {
            Rectangle nextSq = null;
            if (teleportDestination != null) nextSq = teleportDestination;
            else if (direction == Vector2.down) nextSq = GetNeighborBottom(true);
            else if (direction == Vector2.up) nextSq = GetNeighborTop(true);
            else if (direction == Vector2.left) nextSq = GetNeighborLeft(true);
            else if (direction == Vector2.right) nextSq = GetNeighborRight(true);
            //        if (!nextSq?.IsNone() ?? false)
            {
                nextRectangle = nextSq;
            }
            CreateTeleports();
            //Added_feature
            CreateSpawner();
        }

        public AnimationCurve scaleCurve; // Animation curve for the scale
        public float bounceHeight = 1.2f; // Height multiplier for the bounce

        public AnimationClip animationClip; // Animation clips to add to the blend tree
        public string parameterName = "BlendParameter"; // Name of the blend parameter
        public string blendTreeName = "RuntimeBlendTree"; // Name of the blend tree

        /// <summary>
        /// Changes the square type.
        /// </summary>
        /// <param name="sqBlock"></param>
        public void SetType(RectangleBlocks sqBlock)
        {
            if (sqBlock.blocks.Count == 0)
            {
                SetType(sqBlock.block, sqBlock.blockLayer, sqBlock.obstacle, sqBlock.obstacleLayer);
            }
            else
            {
                UpdateSubblocks(sqBlock);
                SetupAnimation();
            }

            pointNumber = 0;
        }

        private void UpdateSubblocks(RectangleBlocks sqBlock)
        {
            int typeCounter = 0;

            // Use a for loop to allow modifications of the collection
            for (int i = 0; i < sqBlock.blocks.Count; i++)
            {
                var square = sqBlock.blocks[i];
                // Debug.Log($"square blocks: {square.squareType}");

                // Check the type of the square and adjust the type counter accordingly
                typeCounter = (i > 0 && square.levelTargetType == sqBlock.blocks[i - 1].levelTargetType)
                    ? typeCounter + 1
                    : 0;

                // Create subblocks with the appropriate prefab and index
                var prefabs = GetBlockPrefab(square.levelTargetType);
                CreateSubblock(square.levelTargetType, prefabs[Mathf.Clamp(typeCounter, 0, prefabs.Length - 1)], i, square);

                // Reset to NONE if the type is IcicleSpread
                if (square.levelTargetType == LevelTargetTypes.ExtraTargetType6)
                    square.levelTargetType = LevelTargetTypes.NONE;
            }

            // Debug.Log($"TypeCounter: {typeCounter}");
        }

        private void SetupAnimation()
        {
            // Setup animation component and clip
            var animation = GetComponent<Animation>();
            var clip = new AnimationClip { legacy = true };

            // Define scale curve for bouncing effect
            scaleCurve = CreateBounceCurve();

            // Apply the scale curve to all axes of the transform
            ApplyScaleCurveToClip(clip);

            // Add the animation clip and optionally play it
            animation.AddClip(clip, "NewClip");
            // animation.Play("NewClip"); // Uncomment to play the animation
        }

        private AnimationCurve CreateBounceCurve()
        {
            return new AnimationCurve(
                new Keyframe(0f, 1.4f),
                new Keyframe(0.25f, bounceHeight),
                new Keyframe(0.5f, 1.4f),
                new Keyframe(0.75f, bounceHeight),
                new Keyframe(1f, 1.4f)
            );
        }

        private void ApplyScaleCurveToClip(AnimationClip clip)
        {
            clip.SetCurve("Cake1/Cake3", typeof(Transform), "localScale.x", scaleCurve);
            clip.SetCurve("Cake1/Cake3", typeof(Transform), "localScale.y", scaleCurve);
            clip.SetCurve("Cake1/Cake3", typeof(Transform), "localScale.z", scaleCurve);
        }


        // Create a bouncing scale animation curve
        private void CreateBouncingScaleCurve()
        {
            // Define the scale curve
            scaleCurve = new AnimationCurve();
            scaleCurve.AddKey(0f, 1f); // Start at scale of 1
            scaleCurve.AddKey(0.25f, bounceHeight); // Bounce up to the specified height
            scaleCurve.AddKey(0.5f, 1f); // Bounce back down to 1 scale
            scaleCurve.AddKey(0.75f, bounceHeight); // Bounce up again
            scaleCurve.AddKey(1f, 1f); // End at 1 scale
        }

        public void SetType(LevelTargetTypes _type, int sqLayer, LevelTargetTypes obstacleType, int obLayer)
        {
            // Debug.Log($"SetType: Starting - type:{_type}, sqLayer:{sqLayer}, obstacleType:{obstacleType}, obLayer:{obLayer}");

            if (mainRectangle == null)
            {
                mainRectangle = this;
                // Debug.Log("SetType: Set mainSquare to this");
            }

            var prefabs = GetBlockPrefab(_type);
            // Debug.Log($"SetType: Got {prefabs.Length} prefabs for type {_type}");

            if (type != _type && type != LevelTargetTypes.BreakableBox && type != LevelTargetTypes.ExtraTargetType1 && !IsTypeExist(_type) && IsAvailable())
            {
                // Debug.Log("SetType: Conditions met for creating new squares");

                for (var i = 0; i < sqLayer; i++)
                {
                    var prefab = prefabs[i];
                    if (prefab != null && _type != LevelTargetTypes.EmptySquare && _type != LevelTargetTypes.NONE)
                    {
                        // Debug.Log($"SetType: Creating square layer {i} with prefab {prefab.name}");

                        var b = Instantiate(prefab);
                        MainManager.Instance.levelData.SetSquareTarget(b, _type, prefab);
                        b.transform.SetParent(mainRectangle.transform);
                        var square = b.GetComponent<Rectangle>();
                        square.field = field;
                        b.transform.localScale = Vector2.one;
                        square.hashCode = GetHashCode();
                        b.transform.localPosition = new Vector3(0, 0, -0.01f);
                        b.GetComponent<SpriteRenderer>().sortingOrder = 0 + i + GetComponent<SpriteRenderer>().sortingOrder;

                        if (_type != LevelTargetTypes.ExtraTargetType2)
                        {
                            mainRectangle.subSquares.Add(square);
                            // Debug.Log($"SetType: Added square to subSquares, total count: {mainSquare.subSquares.Count}");
                        }
                        else
                        {
                            mainRectangle.subSquares.Insert(0, square);
                        }

                        type = GetSubSquare().type;
                        // Debug.Log($"SetType: Updated type to {type}");
                    }
                }
            }
            else
            {
                // Debug.Log($"SetType: Conditions not met - current type:{type}, IsTypeExist:{IsTypeExist(_type)}, IsAvailable:{IsAvailable()}");
            }

            if (obstacleType != LevelTargetTypes.NONE)
            {
                // Debug.Log($"SetType: Creating obstacle of type {obstacleType}");
                CreateObstacle(obstacleType, obLayer);
            }

            // Debug.Log("SetType: Completed");
        }

        /// <summary>
        /// Is type exist among sub-squares
        /// </summary>
        /// <param name="_type"></param>
        /// <returns></returns>
        private bool IsTypeExist(LevelTargetTypes _type)
        {
            return subSquares.Count(i => i.type == _type) > 0;
        }

        /// <summary>
        /// create obstacle on the square
        /// </summary>
        /// <param name="obstacleType"></param>
        /// <param name="obLayer"></param>
        public void CreateObstacle(LevelTargetTypes obstacleType, int obLayer)
        {
            if (obstacleType == LevelTargetTypes.Eggs)
            {
                GenSpiral();
                return;
            }

            if (obstacleType == LevelTargetTypes.Pots)
            {
                GenSpiral();
                return;
            }

            var prefabs = GetBlockPrefab(obstacleType);
            for (var i = 0; i < obLayer; i++)
            {
                var prefab = prefabs[i];
                var b = Instantiate(prefab);
                b.transform.SetParent(transform);
                Rectangle rectangle = b.GetComponent<Rectangle>();
                rectangle.field = field;
                rectangle.mainRectangle = this;
                rectangle.hashCode = GetHashCode();
                b.transform.localPosition = new Vector3(0, 0, -0.5f);
                b.transform.localScale = Vector2.one;
                MainManager.Instance.levelData.SetSquareTarget(b, obstacleType, prefab);
                // if (prefab != null && obstacleType == SquareTypes.ThrivingBlock)
                //     Destroy(prefab.gameObject);
                if (obstacleType != LevelTargetTypes.ExtraTargetType2)
                    subSquares.Add(b.GetComponent<Rectangle>());
                else
                    subSquares.Insert(0, b.GetComponent<Rectangle>());
                type = GetSubSquare().type;
                if (type == LevelTargetTypes.PlateCabinet && i == 0)
                {
                    // Debug.Log("sallog");
                    animator = b.GetComponent<Animator>();
                }

                undestroyable = rectangle.undestroyable;
            }
        }

        private int pointNumber = 0;

        private void CreateSubblock(LevelTargetTypes type, GameObject prefab, int index, SquareTypeLayer layerSquare)
        {
            if (!IsValidSubblock(type, prefab)) return;

            if (type == LevelTargetTypes.Eggs)
            {
                GenSpiral();
                return;
            }

            if (type == LevelTargetTypes.Pots)
            {
                GenSpiral2();
                return;
            }

            if (!layerSquare.anotherSquare)
            {
                InstantiateAndConfigureSubblock(type, prefab, index, layerSquare);
            }
            else
            {
                AddExistingSquare(layerSquare);
            }
        }

        private void InstantiateAndConfigureSubblock(LevelTargetTypes type, GameObject prefab, int index, SquareTypeLayer layerSquare)
        {
            // Debug.Log($"CreateSubblock: Instantiating prefab {prefab.name}");
            pointNumber++;

            var squareObject = Instantiate(prefab, transform, true);
            var square = ConfigureSquare(squareObject, index);

            ReparentChildren();
            // Debug.Log($"sallog index is {index} and type is {type} and also layerSquare is {layerSquare} and prefab is {prefab}");

            PositionAndScaleSubblock(type, prefab, index, squareObject, square);

            ApplyRotationIfNeeded(layerSquare, squareObject, square);

            SetSortingOrder(squareObject, index);

            AddToLevelTargets(type, prefab, squareObject);

            AddToSubSquares(type, square);

            UpdateMainSquareProperties(squareObject, index, square, prefab);
        }

        private void PositionAndScaleSubblock(LevelTargetTypes type, GameObject prefab, int index, GameObject squareObject, Rectangle rectangle)
        {
            if (type == LevelTargetTypes.PlateCabinet && index > 1)
            {
                PositionAndScaleForBox(squareObject, index);
            }
            else if (type == LevelTargetTypes.PotionCabinet && index > 1)
            {
                PositionAndScaleForBox2(squareObject, index);
            }
            else if (type == LevelTargetTypes.BreakableBox && index > 1)
            {
                PositionAndScaleForBreakables(squareObject, index, prefab);
            }
            else
            {
                PositionAndScaleDefault(squareObject, rectangle);
            }
        }

        private bool IsValidSubblock(LevelTargetTypes type, GameObject prefab)
        {
            return prefab != null && type != LevelTargetTypes.EmptySquare && type != LevelTargetTypes.NONE;
        }

        private Rectangle ConfigureSquare(GameObject squareObject, int index)
        {
            var square = squareObject.GetComponent<Rectangle>();
            square.field = field;
            square.mainRectangle = this;
            square.hashCode = GetHashCode();

            if (index == 1)
                animator = squareObject.GetComponent<Animator>();

            return square;
        }

        private void ReparentChildren()
        {
            foreach (Transform child in transform)
            {
                child.SetParent(transform.GetChild(0));
            }
        }

        private void PositionAndScaleForBox(GameObject squareObject, int index)
        {
            float xOffset = -1.5f; // Starting x position
            float spacing = 0.5f; // Space between items
            float yPositionTop = 0.7f; // Y position for top row
            float yPositionBottom = -0.9f; // Y position for bottom row

            int baseOrder = 2; // Base sorting order
            int orderOffset; // Order offset based on position

            // For indices 1-6, position in top row
            if (index <= 6)
            {
                // Calculate order - items closer to center get higher order
                // Index 1,2,3 -> order increases towards center
                // Index 4,5,6 -> order decreases from center
                orderOffset = (index <= 3) ? index : (7 - index);

                squareObject.transform.localPosition = new Vector3(
                    xOffset + (spacing * (index - 1)), // Calculate x position based on index
                    yPositionTop // Top row y position
                );
            }
            // For indices 7-12, position in bottom row
            else
            {
                // Similar ordering for bottom row
                // Index 7,8,9 -> order increases towards center
                // Index 10,11,12 -> order decreases from center
                orderOffset = (index <= 9) ? (index - 6) : (12 - index);

                squareObject.transform.localPosition = new Vector3(
                    xOffset + (spacing * (index - 6)), // Calculate x position, reset count for bottom row
                    yPositionBottom // Bottom row y position
                );
            }

            // Set sorting order - base order plus calculated offset
            var renderer = squareObject.transform.GetChild(0).GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                SetSortingLayer(renderer, "Spine", baseOrder + orderOffset);
            }

            squareObject.transform.localScale = Vector2.one * 1.25f;
            squareObject.transform.GetComponent<SpriteRenderer>().enabled = false;
            SetBox2x2Type();
        }

        private void PositionAndScaleForBox2(GameObject squareObject, int index)
        {
            // Specific positions for 2x2 grid based on requirements
            Vector3[] positions = new Vector3[]
            {
                new Vector3(-0.64f, 0.6f, 0), // Top Left (1)
                new Vector3(0.65f, 0.6f, 0), // Top Right (2)
                new Vector3(-0.65f, -0.8f, 0), // Bottom Left (3)
                new Vector3(0.65f, -0.8f, 0) // Bottom Right (4)
            };

            if (index >= 2 && index <= 5) // Only position indices 2-5 (4 items)
            {
                int posIndex = index - 2; // Convert to 0-based index for our positions array

                // Set position using exact coordinates
                squareObject.transform.localPosition = positions[posIndex];

                // Set sorting order - higher numbers are in front
                var renderer = squareObject.transform.GetChild(0).GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    SetSortingLayer(renderer, "Spine", 2 + posIndex);
                }

                // Set scale and hide sprite renderer
                squareObject.transform.localScale = Vector2.one * 1.25f;
                squareObject.transform.GetComponent<SpriteRenderer>().enabled = false;
                SetBox2x2Type2();
            }
        }

        private void SetBox2x2Type()
        {
            boxParent = this;
            boxChildPos = 1;

            boxChilds = new[]
            {
                GetNeighborRight(),
                GetNeighborBottom(),
                GetNeighborRight()?.GetNeighborBottom()
            };

            int position = 2; // Start with position 2
            foreach (var boxChild in boxChilds)
            {
                if (boxChild != null)
                {
                    var square = boxChild.GetComponent<Rectangle>();
                    square.type = LevelTargetTypes.PlateCabinet;
                    square.boxParent = this;

                    // Assign positions:
                    // Right square gets 2
                    // Bottom square gets 3
                    // Bottom-right square gets 4
                    square.boxChildPos = position++;
                }
            }
        }

        private void SetBox2x2Type2()
        {
            boxParent = this;
            boxChildPos = 1; // Original square is 1

            boxChilds = new[]
            {
                GetNeighborRight(),
                GetNeighborBottom(),
                GetNeighborRight()?.GetNeighborBottom()
            };

            int position = 2; // Start with position 2
            foreach (var boxChild in boxChilds)
            {
                if (boxChild != null)
                {
                    var square = boxChild.GetComponent<Rectangle>();
                    square.type = LevelTargetTypes.PotionCabinet;
                    square.boxParent = this;

                    // Assign positions:
                    // Right square gets 2
                    // Bottom square gets 3
                    // Bottom-right square gets 4
                    square.boxChildPos = position++;
                }
            }
        }

        private void PositionAndScaleForBreakables(GameObject squareObject, int index, GameObject prefab)
        {
            if (prefab.name == "WoodPart1")
            {
                squareObject.transform.localPosition = new Vector3(0, 0.32f, 0);
                squareObject.transform.localScale = Vector2.one * 1.3f;
            }

            if (prefab.name == "WoodPart2")
            {
                squareObject.transform.localPosition = new Vector3(0, -0.32f, 0);
                squareObject.transform.localScale = Vector2.one * 1.3f;
            }

            if (prefab.name == "BreakableBox")
            {
                squareObject.transform.localPosition = new Vector3(0.0f, 0, 0);
                squareObject.transform.localScale = Vector2.one;
            }
        }

        private void PositionAndScaleDefault(GameObject squareObject, Rectangle rectangle)
        {
            // Debug.Log("sallog PositionAndScaleDefault");

            squareObject.transform.localPosition = new Vector3(
                1.95f * (rectangle.sizeInSquares.x - 1),
                -(1.95f * (rectangle.sizeInSquares.y - 1))
            ) / 2;
            squareObject.transform.localScale = Vector2.one;
        }

        private void ApplyRotationIfNeeded(SquareTypeLayer layerSquare, GameObject squareObject, Rectangle rectangle)
        {
            if (layerSquare.rotate)
            {
                squareObject.transform.Rotate(Vector3.back * 90, Space.Self);
                squareObject.transform.localPosition = new Vector3(
                    1.95f * (rectangle.sizeInSquares.y - 1),
                    -(1.95f * (rectangle.sizeInSquares.x - 1))
                ) / 2;
            }
        }

        private void SetSortingOrder(GameObject squareObject, int index)
        {
            var spriteRenderer = squareObject.GetComponent<SpriteRenderer>();
            spriteRenderer.sortingOrder = index + GetComponent<SpriteRenderer>().sortingOrder;
        }

        private void AddToLevelTargets(LevelTargetTypes type, GameObject prefab, GameObject squareObject)
        {
            if (type != LevelTargetTypes.EmptySquare)
            {
                MainManager.Instance.levelData.SetSquareTarget(squareObject, type, prefab);
            }
        }

        private void AddToSubSquares(LevelTargetTypes type, Rectangle rectangle)
        {
            if (type != LevelTargetTypes.ExtraTargetType2)
            {
                subSquares.Add(rectangle);
            }
            else
            {
                subSquares.Insert(0, rectangle);
            }
        }

        private void UpdateMainSquareProperties(GameObject squareObject, int index, Rectangle rectangle, GameObject prefab)
        {
            type = GetSubSquare().type;
            squareObject.name = $"{prefab.name}";
            undestroyable = rectangle.undestroyable;
        }

        private void AddExistingSquare(SquareTypeLayer layerSquare)
        {
            var originalSquare = field
                .GetSquare(layerSquare.originalPos)
                .subSquares
                .FirstOrDefault(i => i.type == layerSquare.levelTargetType);

            if (originalSquare != null)
            {
                subSquares.Add(originalSquare);
            }
        }


        /// <summary>
        /// generate spiral item
        /// </summary>
        void GenSpiral()
        {
            MainManager.OnLevelLoaded += () =>
            {
                if (Item != null) Item.transform.position = transform.position;
            };
            GenItem(false, ItemsTypes.Eggs);
        }

        void GenSpiral2()
        {
            MainManager.OnLevelLoaded += () =>
            {
                if (Item != null) Item.transform.position = transform.position;
            };
            GenItem(false, ItemsTypes.Pots);
        }

        /// <summary>
        /// check is which direction is restricted
        /// </summary>
        /// <param name="dir"></param>
        /// <returns></returns>
        public bool IsDirectionRestricted(Vector2 dir)
        {
            foreach (var restriction in directionRestriction)
            {
                if (restriction == dir) return true;
            }

            return false;
        }


        /// <summary>
        /// Get neighbor methods
        /// </summary>
        /// <param name="considerRestrictions"></param>
        /// <param name="safe"></param>
        /// <returns></returns>
        public Rectangle GetNeighborLeft(bool considerRestrictions = true, bool safe = false)
        {
            if (considerRestrictions && (IsDirectionRestricted(Vector2.left))) return null;
            if (col == 0 && !safe)
                return null;
            var square = field.GetSquare(col - 1, row, safe);
            // if (considerRestrictions && (square?.IsNone() ?? false)) return null;
            return square;
        }

        public Rectangle GetNeighborRight(bool considerRestrictions = true, bool safe = false)
        {
            if (considerRestrictions && (IsDirectionRestricted(Vector2.right))) return null;
            if (col >= field.fieldData.maxCols && !safe)
                return null;
            var square = field.GetSquare(col + 1, row, safe);
            // if (considerRestrictions && (square?.IsNone() ?? false)) return null;
            return square;
        }

        // Add these methods to the Square class
        public Rectangle GetNeighborTopLeft(bool considerRestrictions = true, bool safe = false)
        {
            if (considerRestrictions && (IsDirectionRestricted(new Vector2(-1, 1)))) return null;
            if ((col == 0 || row == field.fieldData.maxRows - 1) && !safe)
                return null;
            return field.GetSquare(col - 1, row + 1, safe);
        }

        public Rectangle GetNeighborTopRight(bool considerRestrictions = true, bool safe = false)
        {
            if (considerRestrictions && (IsDirectionRestricted(new Vector2(1, 1)))) return null;
            if ((col == field.fieldData.maxCols - 1 || row == field.fieldData.maxRows - 1) && !safe)
                return null;
            return field.GetSquare(col + 1, row + 1, safe);
        }

        public Rectangle GetNeighborBottomLeft(bool considerRestrictions = true, bool safe = false)
        {
            if (considerRestrictions && (IsDirectionRestricted(new Vector2(-1, -1)))) return null;
            if ((col == 0 || row == 0) && !safe)
                return null;
            return field.GetSquare(col - 1, row - 1, safe);
        }

        public Rectangle GetNeighborBottomRight(bool considerRestrictions = true, bool safe = false)
        {
            if (considerRestrictions && (IsDirectionRestricted(new Vector2(1, -1)))) return null;
            if ((col == field.fieldData.maxCols - 1 || row == 0) && !safe)
                return null;
            return field.GetSquare(col + 1, row - 1, safe);
        }

        public Rectangle GetNeighborTop(bool considerRestrictions = true, bool safe = false)
        {
            if (considerRestrictions && (IsDirectionRestricted(Vector2.up))) return null;
            if (row == 0 && !safe)
                return null;
            var square = field.GetSquare(col, row - 1, safe);
            // if (considerRestrictions && (square?.IsNone() ?? false)) return null;
            return square;
        }

        public Rectangle GetNeighborBottom(bool considerRestrictions = true, bool safe = false)
        {
            if (considerRestrictions && (IsDirectionRestricted(Vector2.down))) return null;
            if (row >= field.fieldData.maxRows && !safe)
                return null;
            var square = field.GetSquare(col, row + 1, safe);
            // if (considerRestrictions && (square?.IsNone() ?? false)) return null;
            return square;
        }

        /// <summary>
        /// Get next square along with direction
        /// </summary>
        /// <param name="safe"></param>
        /// <returns></returns>
        public Rectangle GetNextSquare(bool safe = false)
        {
            return nextRectangle;
        }

        /// <summary>
        /// Get previous square along with direction
        /// </summary>
        /// <param name="safe"></param>
        /// <returns></returns>
        public Rectangle GetPreviousSquare(bool safe = false)
        {
            if (teleportOrigin != null) return teleportOrigin;
            if (GetNeighborBottom(true, safe)?.direction == Vector2.up) return GetNeighborBottom(true, safe);
            if (GetNeighborTop(true, safe)?.direction == Vector2.down) return GetNeighborTop(true, safe);
            if (GetNeighborLeft(true, safe)?.direction == Vector2.right) return GetNeighborLeft(true, safe);
            if (GetNeighborRight(true, safe)?.direction == Vector2.left) return GetNeighborRight(true, safe);

            return null;
        }

        /// <summary>
        /// Get squares sequence before this square
        /// </summary>
        /// <returns></returns>
        public Rectangle[] GetSeqBeforeFromThis()
        {
            var sq = sequence; //field.GetCurrentSequence(this);
            return sq.Where(i => i.orderInSequence > orderInSequence).OrderBy(i => i.orderInSequence).ToArray();
        }

        /// <summary>
        /// Check is there any falling items in this sequence
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public bool AnyFallingItemsInSeq(int color = -1)
        {
            var sq = sequence; //field.GetCurrentSequence(this);
            if (color > -1)
            {
                var anyFallingItemsInSeq = sq.Any(i => i.Item != null && i.Item?.color == color && i.Item.falling);
                return anyFallingItemsInSeq;
            }

            return sq.Any(i => i.Item?.falling ?? false);
        }

        /// <summary>
        /// Check any falling items around the square
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public bool AnyFallingItemsAround(int color = -1)
        {
            return GetAllNeghborsCross().Any(i => i.AnyFallingItemsInSeq(color));
        }

        /// <summary>
        /// Get item above the square by flow
        /// </summary>
        /// <returns></returns>
        public Item GetItemAbove()
        {
            if (isEnterPoint) return null;
            var sq = sequence; //field.GetCurrentSequence(this);
            var list = sq.Where(i => i.orderInSequence > orderInSequence).OrderBy(i => i.orderInSequence);
            //		var list = GetSeqBeforeFromThis();
            foreach (var square in list)
            {
                if (square.IsFree() && square.Item)
                    return square.Item;
                if (!square.IsFree())
                    return null;
            }

            if (list.Count() == 0) return GetPreviousSquare()?.Item;
            return null;
        }

        /// <summary>
        /// Get reverse direction
        /// </summary>
        /// <returns></returns>
        public Vector2 GetReverseDirection()
        {
            Vector3 pos = Vector2.up;
            if (direction == Vector2.down) pos = Vector2.up;
            if (direction == Vector2.up) pos = Vector2.down;
            if (direction == Vector2.left) pos = Vector2.right;
            if (direction == Vector2.right) pos = Vector2.left;
            return pos;
        }

        /// <summary>
        /// Gets the match color around to set right color for Generated item
        /// </summary>
        /// <returns>The count match color around.</returns>
        public int GetMatchColorAround(int col)
        {
            var matches = 0;
            if ((GetNeighborBottom()?.Item?.color ?? -1) == col)
                matches = 1;
            if (((GetNeighborBottom()?.GetNeighborBottom())?.Item?.color ?? -1) == col)
                matches++;
            if ((GetNeighborTop()?.Item?.color ?? -1) == col)
                matches = 1;
            if (((GetNeighborTop()?.GetNeighborTop())?.Item?.color ?? -1) == col)
                matches++;
            if ((GetNeighborRight()?.Item?.color ?? -1) == col)
                matches = 1;
            if (((GetNeighborRight()?.GetNeighborRight())?.Item?.color ?? -1) == col)
                matches++;
            if ((GetNeighborLeft()?.Item?.color ?? -1) == col)
                matches = 1;
            if (((GetNeighborLeft()?.GetNeighborLeft())?.Item?.color ?? -1) == col)
                matches++;
            return matches;
        }

        /// <summary>
        /// Match 3 search methods
        /// </summary>
        /// <param name="spr_COLOR"></param>
        /// <param name="countedSquares"></param>
        /// <param name="separating"></param>
        /// <param name="countedSquaresGlobal"></param>
        /// <returns></returns>
        Hashtable FindMoreMatches(int spr_COLOR, Hashtable countedSquares, FindSeparating separating, Hashtable countedSquaresGlobal = null)
        {
            //var globalCounter = true;
            if (countedSquaresGlobal == null)
            {
                //globalCounter = false;
                countedSquaresGlobal = new Hashtable();
            }

            if (Item == null || Item.destroying || Item.falling)
                return countedSquares;
            //    if (LevelManager.This.countedSquares.ContainsValue(this.item) && globalCounter) return countedSquares;
            if (Item.color == spr_COLOR && !countedSquares.ContainsValue(Item) && Item.currentType != ItemsTypes.Gredient && Item.currentType != ItemsTypes.DiscoBall && !Item
                    .falling && Item.Combinable)
            {
                if (MainManager.Instance.onlyFalling && Item.JustCreatedItem)
                    countedSquares.Add(countedSquares.Count - 1, Item);
                else if (!MainManager.Instance.onlyFalling)
                    countedSquares.Add(countedSquares.Count - 1, Item);
                else
                    return countedSquares;

                //			if (separating == FindSeparating.VERTICAL)
                {
                    if (GetNeighborTop() != null)
                        countedSquares = GetNeighborTop().FindMoreMatches(spr_COLOR, countedSquares, FindSeparating.Vertical);
                    if (GetNeighborBottom() != null)
                        countedSquares = GetNeighborBottom().FindMoreMatches(spr_COLOR, countedSquares, FindSeparating.Vertical);
                }
                //			else if (separating == FindSeparating.HORIZONTAL)
                {
                    if (GetNeighborLeft() != null)
                        countedSquares = GetNeighborLeft().FindMoreMatches(spr_COLOR, countedSquares, FindSeparating.Horizontal);
                    if (GetNeighborRight() != null)
                        countedSquares = GetNeighborRight().FindMoreMatches(spr_COLOR, countedSquares, FindSeparating.Horizontal);
                }
            }

            return countedSquares;
        }

        public List<Item> FindMatchesAround(FindSeparating separating = FindSeparating.None, int matches = 3, Hashtable countedSquaresGlobal = null)
        {
            //var globalCounter = true;
            var newList = new List<Item>();
            if (countedSquaresGlobal == null)
            {
                //globalCounter = false;
                countedSquaresGlobal = new Hashtable();
            }

            var countedSquares = new Hashtable();
            countedSquares.Clear();
            if (Item == null)
                return newList;
            if (!Item.Combinable)
                return newList;
            //		if (separating != FindSeparating.HORIZONTAL)
            //		{
            countedSquares = FindMoreMatches(Item.color, countedSquares, FindSeparating.Vertical, countedSquaresGlobal);
            //		}

            foreach (DictionaryEntry de in countedSquares)
            {
                field.countedSquares.Add(field.countedSquares.Count - 1, de.Value);
            }

            foreach (DictionaryEntry de in countedSquares)
            {
                field.countedSquares.Add(field.countedSquares.Count - 1, de.Value);
            }

            if (countedSquares.Count < matches)
                countedSquares.Clear();

            foreach (DictionaryEntry de in countedSquares)
            {
                newList.Add((Item)de.Value);
            }

            // print(countedSquares.Count);
            return newList.Distinct().ToList();
        }

        /// <summary>
        /// Check should Item fall down
        /// </summary>
        public IEnumerator CheckFallOut()
        {
            if (MainManager.Instance.StopFall)
            {
                yield break;
            }

            if (Item != null && CanGoOut())
            {
                var nxtSq = Vector2.Distance(this.transform.position, Item.transform.position) > 0.3f ? this : GetNextSquareRecursively(this); //GetNextSquareRecursively();
                if (nxtSq)
                {
                    if (nxtSq.CanGoInto())
                    {
                        if (nxtSq.Item == null)
                        {
                            Item.ReplaceCurrentSquareToFalling(nxtSq);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Get next square with Item
        /// </summary>
        /// <returns></returns>
        public Rectangle GetNextSquareRecursively(Rectangle originRectangle)
        {
            var sq = GetNextSquare();
            if (sq != null)
            {
                if (sq.IsNone())
                    return this;
                if (sq.Item != null || !sq.CanGoInto() || (Vector2.Distance(sq.GetPosition(), GetPosition()) >= 2 && originRectangle.orderInSequence - sq.orderInSequence > 1))
                    return this;
                sq = sq.GetNextSquareRecursively(originRectangle);
            }
            else
                sq = this;

            return sq;
        }

        /// <summary>
        /// true if square disabled in editor
        /// </summary>
        /// <returns></returns>
        public bool IsNone()
        {
            return type == LevelTargetTypes.NONE;
        }

        /// <summary>
        /// true if square available and non Undestroyable
        /// </summary>
        /// <returns></returns>
        public bool IsAvailable()
        {
            return !IsNone() && !IsUndestroyable();
        }

        /// <summary>
        /// true if has destroyable obstacle
        /// </summary>
        /// <returns></returns>
        public bool IsHaveDestroybleObstacle()
        {
            return !IsUndestroyable() && (!GetSubSquare()?.canMoveIn ?? false);
        }

        /// <summary>
        /// true if the square has obstacle
        /// </summary>
        /// <returns></returns>
        public bool IsObstacle()
        {
            return (!GetSubSquare().CanGoInto() || !GetSubSquare().CanGoOut() || IsUndestroyable());
        }

        public bool IsGrassObstacle()
        {
            return (GetSubSquare().type == LevelTargetTypes.Grass || GetSubSquare().type == LevelTargetTypes.GrassType2);
        }

        public bool IsUndestroyable()
        {
            return GetSubSquare()?.undestroyable ?? false;
        }


        public Dictionary<int, int> CalculateLayersPerRow()
        {
            Dictionary<int, int> layersPerRow = new Dictionary<int, int>();

            for (int row = 0; row < field.fieldData.maxRows; row++)
            {
                int layerCount = 0;

                for (int col = 0; col < field.fieldData.maxCols; col++)
                {
                    Rectangle rectangle = field.GetSquare(col, row);
                    if (rectangle != null)
                    {
                        layerCount += rectangle.GetLayersCount();
                    }
                }

                layersPerRow[row] = layerCount;
            }

            return layersPerRow;
        }


        /// <summary>
        /// true if the square available for Item
        /// </summary>
        /// <returns></returns>
        public bool IsFree()
        {
            return (!IsNone() && !IsObstacle());
        }

        /// <summary>
        /// Can item fall out of the square
        /// </summary>
        /// <returns></returns>
        public bool CanGoOut()
        {
            return (GetSubSquare()?.canMoveOut ?? false) && (!IsUndestroyable());
        }

        /// <summary>
        /// can item fall to the square
        /// </summary>
        /// <returns></returns>
        public bool CanGoInto()
        {
            return (GetSubSquare()?.canMoveIn ?? false) && (!IsUndestroyable()); //TODO: none square falling through
        }

        /// <summary>
        /// can item fall to the square
        /// </summary>
        /// <returns></returns>
        public bool CanGenInto()
        {
            return (GetSubSquare()?.cantGenIn ?? false) && (!IsUndestroyable()); //TODO: none square falling through
        }

        /// <summary>
        /// Next move event
        /// </summary>
        void OnNextMove()
        {
            //dontDestroyOnThisMove = false;
            MainManager.OnTurnEnd -= OnNextMove;
        }

        public void SetDontDestroyOnMove()
        {
            //dontDestroyOnThisMove = true;
            MainManager.OnTurnEnd += OnNextMove;
        }

        public void DestroyBlock(bool destroyTarget = false, bool destroyNeighbour = true, int color = 0)
        {
            // Debug.Log($"square: color is my {color}");
            // Debug.Log("Square: DestroyBlock method called - destroyTarget: " + destroyTarget + ", destroyIteration: " + destroyIteration);

            // Debug.Log($"Square: DestroyBlock method start  square is {this} and type is {type}");
            if (!InitializeDestroyBlock())
                return;


            // if (!CheckIfDestroyable(destroyTarget))
            // {
            //// Debug.Log($"Square: DestroyBlock !CheckIfDestroyable(destroyTarget)");
            //     return;
            // }


            HandleNeighbourDestruction(destroyNeighbour, color: color);

            HandleItemDestruction();

            if (subSquares.Count > 0)
            {
                HandleSubSquares(destroyTarget, color: color);
            }

            FinalizeDestruction();
            //Debug.LogError($" getting {isHoneyBlock} ");
            if (isHoneyBlock)
            {
                isHoneyBlock = false;
                SpawnFirework("CFXR2Honey");
                UpdateNeighboringHoneyTiles();
            }


            // Debug.Log("Square: DestroyBlock method end");
        }

        private void UpdateNeighboringHoneyTiles()
        {
            Debug.LogError("getting neighbors");
            var neighbors = new List<Rectangle>
            {
                GetNeighborTop(),
                GetNeighborBottom(),
                GetNeighborLeft(),
                GetNeighborRight(),
                GetNeighborTop()?.GetNeighborLeft(),
                GetNeighborTop()?.GetNeighborRight(),
                GetNeighborBottom()?.GetNeighborLeft(),
                GetNeighborBottom()?.GetNeighborRight()
            };

            foreach (var neighbor in neighbors)
            {
                if (neighbor != null)
                {
                    var honeyBlock = neighbor.GetComponentInChildren<HoneyBlock>();
                    if (honeyBlock != null)
                    {
                        honeyBlock.InitializeTile();
                    }
                    else
                    {
                        Debug.LogError($"getting HoneyBlock component not found in neighbor at position ({neighbor.row}, {neighbor.col})");
                    }
                }
                else
                {
                    Debug.LogError("getting Neighbor is null");
                }
            }
        }

        private bool InitializeDestroyBlock()
        {
            if (destroyIteration == 0)
            {
                // Debug.Log("Square: First destroy iteration");
                destroyIteration = MainManager.Instance.destLoopIterations;
                MainManager.Instance.delayedCall(0.2f, () => { destroyIteration = 0; });
                return true;
            }

            // Debug.Log("Square: Destroy iteration already in progress, returning - destroyIteration: " + destroyIteration);
            return false;
        }

        private bool CheckIfDestroyable(bool destroyTarget)
        {
            if (IsUndestroyable() || cannotBeDestroy)
            {
                // Debug.Log("Square: Block is undestroyable or cannot be destroyed, returning");
                return false;
            }

            return true;
        }

        private void HandleNeighbourDestruction(bool destroyNeighbour, int color = 0)
        {
            if (GetSubSquare().CanGoInto() && destroyNeighbour)
            {
                // Debug.Log("Square: Sub-square can go into, handling neighbours");
                foreach (var sq in GetAllNeghborsCross())
                {
                    if (!sq.GetSubSquare().CanGoInto() || ((sq.Item?.DestroyByNeighbour ?? false) && !(Item?.DestroyByNeighbour ?? false)))
                    {
                        sq.DestroyBlock(color: color);
                    }
                }
            }
        }

        private void HandleItemDestruction()
        {
            // if(isHoneyBlock){
            //     return;
            // }
            if (Item?.DestroyByNeighbour ?? false)
            {
                Item.DestroyItem();
            }
        }

        private void HandleSubSquares(bool destroyTarget, int color = 0)
        {
            // Determine the appropriate subSquare type
            Rectangle subRectangle = (type == LevelTargetTypes.PlateCabinet) ? GetBoxSubSquare() : GetSubSquare();
            // Debug.Log($"Square: squaretype is {type}");

            // Handle mailbox logic, if applicable
            if (HandleMailBox())
            {
                ProcessMailBox(subRectangle);
                return;
            }

            // Handle Box block logic, if applicable
            if (HandleBoxDoorBlock()) return;

            // Check if the subSquare can be destroyed
            if (!CanDestroySubSquare(subRectangle, destroyTarget)) return;

            // Play destruction effects
            PlayDestroyEffects(subRectangle);

            // Attempt to remove the subSquare and process it if successful
            if (RemoveSubSquare(ref subRectangle, color))
            {
                UpdateType();
                ProcessSquare(subRectangle);
            }
        }

        // Handles the mailbox-related logic
        private void ProcessMailBox(Rectangle subRectangle)
        {
            // Debug.Log($"Square: squaretype is {type}");
            MainManager.Instance.levelData.GetTargetObject().CheckSquare(new[] { subRectangle });
            MainManager.Instance.ShowPopupScore(subRectangle.score, transform.position, 0);
            MainManager.Score += score;
        }

        // Processes the removed subSquare for score and targets
        private void ProcessSquare(Rectangle subRectangle)
        {
            Debug.LogError($"Square: Processing square - type: {type}, score: {subRectangle}");
            if (subRectangle.type == LevelTargetTypes.BreakableBox)
                CentralSoundManager.Instance.PlayOneShot(CentralSoundManager.Instance.woodSmashEffect);

            else if (type is LevelTargetTypes.PlateCabinet or LevelTargetTypes.PotionCabinet)
                CentralSoundManager.Instance.PlayOneShot(CentralSoundManager.Instance.windowBreakEffect);
            MainManager.Instance.levelData.GetTargetObject().CheckSquare(new[] { subRectangle });
            MainManager.Instance.ShowPopupScore(subRectangle.score, transform.position, 0);
            MainManager.Score += score;
        }

        // Removes the subSquare from the square if conditions are met
        private bool RemoveSubSquare(ref Rectangle subRectangle, int color = 0)
        {
            if (type is LevelTargetTypes.PlateCabinet or LevelTargetTypes.PotionCabinet)
            {
                StartCoroutine(AnimateBoxShake());
            }

            // Handle standard or matching subSquare removal
            if (type != LevelTargetTypes.PotionCabinet)
            {
                return RemoveStandardSubSquare(ref subRectangle);
            }
            else
            {
                return RemoveMatchingSubSquare(ref subRectangle, color);
            }
        }

        // Removes a standard subSquare
        private bool RemoveStandardSubSquare(ref Rectangle subRectangle)
        {
            subSquares.Remove(subRectangle);
            // Debug.Log($"Square: Removing sub-square - subSquares count: {subSquares.Count}");

            if (subRectangle.name == "WoodPart1" || subRectangle.name == "WoodPart2")
            {
                AnimateSubSquare(subRectangle);
            }
            else
            {
                if (type == LevelTargetTypes.PlateCabinet && !boxProcessingQueue.Contains(this))
                {
                    EnqueueBoxProcessing();
                }

                Destroy(subRectangle.gameObject);
            }

            return true;
        }

        // Removes a subSquare that matches a given color
        private bool RemoveMatchingSubSquare(ref Rectangle subRectangle, int color)
        {
            if (type != LevelTargetTypes.PotionCabinet) return false;

            var availableSubSquares = subSquares.Skip(1).ToList(); // Exclude the first background subSquare
            if (color == 0 && availableSubSquares.Any())
            {
                return RemoveRandomSubSquare(ref subRectangle, availableSubSquares);
            }

            return RemoveSubSquareByColor(ref subRectangle, availableSubSquares, color);
        }

        // Randomly removes one available subSquare
        private bool RemoveRandomSubSquare(ref Rectangle subRectangle, List<Rectangle> availableSubSquares)
        {
            subRectangle = availableSubSquares[Random.Range(0, availableSubSquares.Count)];
            subSquares.Remove(subRectangle);
            Destroy(subRectangle.gameObject);
            // Debug.Log("Removed random SubSquare");
            EnqueueBoxProcessing();
            return true;
        }

        // Removes a subSquare based on matching color
        private bool RemoveSubSquareByColor(ref Rectangle subRectangle, List<Rectangle> availableSubSquares, int color)
        {
            foreach (var subSq in availableSubSquares)
            {
                if (subSq.GetComponent<SubRectangle>()?.BottleColor == color)
                {
                    subRectangle = subSq;
                    subSquares.Remove(subSq);
                    Destroy(subSq.gameObject);
                    // Debug.Log($"Removed SubSquare with matching color {color}");
                    EnqueueBoxProcessing();
                    return true;
                }
            }

            return false;
        }

        // Adds this square to the processing queue for Box handling
        private void EnqueueBoxProcessing()
        {
            if (!boxProcessingQueue.Contains(this))
            {
                boxProcessingQueue.Enqueue(this);
                StartCoroutine(ProcessBoxQueue());
                // Debug.Log($"Square: Remaining subSquares count: {subSquares.Count}");
            }
        }

        // Processes the Box queue with a slight delay between each square
        private IEnumerator ProcessBoxQueue()
        {
            while (boxProcessingQueue.Count > 0)
            {
                boxProcessingQueue.Dequeue().HandleBoxSquare();
                yield return new WaitForSeconds(0.1f);
            }
        }

        // Handles the animation of subSquare destruction
        private void AnimateSubSquare(Rectangle subRectangle)
        {
            GameObject subSquareClone = InstantiateSubSquareClone(subRectangle); // Clone for animation
            SetSortingLayer(subSquareClone); // Adjust rendering order
            ApplyExplosionAnimation(subSquareClone, subRectangle); // Apply destruction effect
            Destroy(subRectangle.gameObject); // Destroy original subSquare
        }

        private bool HandleMailBox()
        {
            if (type == LevelTargetTypes.Mails)
            {
                // Debug.Log($"Square: DestroyBlock type is Email");
                subSquares[0].GetComponentInChildren<Animator>().SetTrigger("Hit");
                return true;
            }

            return false;
        }

        private bool HandleBoxDoorBlock()
        {
            if (type == LevelTargetTypes.PlateCabinet)
            {
                if (subSquares.Count == 11 && subSquares[0].hitCount < 1)
                {
                    AnimateBoxOpening();
                    subSquares[0].hitCount++;
                    AnimateDoor(subSquares[0].GetComponent<BreakableSquare>().Door[0], Vector3.left);
                    AnimateDoor(subSquares[0].GetComponent<BreakableSquare>().Door[1], Vector3.right);
                    // StartCoroutine(PlaycupAnimation());
                    return true;
                }
            }

            return false;
        }

        private void AnimateBoxOpening()
        {
            var animatorCab = subSquares?[0].transform.GetChild(2).GetComponent<Animator>();
            animatorCab?.SetTrigger("OpenDoor");

            StartCoroutine(AnimateBoxShake());
            var renderer = subSquares?[0].transform.GetChild(2).GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                SetSortingLayer(renderer, "Spine", 0);
            }
        }

        // Add this static field at class level
        private bool isAnySquareShaking = false;
        private bool isShakeAnimating = false;

        private IEnumerator AnimateBoxShake()
        {
            // Prevent multiple animations from running simultaneously
            if (isShakeAnimating || isAnySquareShaking)
            {
                yield break;
            }

            // Ensure subSquares list exists and is not empty
            if (subSquares == null || subSquares.Count == 0)
            {
                yield break;
            }

            isShakeAnimating = true;
            isAnySquareShaking = true;

            // Filter out destroyed objects safely
            Rectangle[] squaresToAnimate = subSquares
                .Where(x => x != null && x.gameObject != null)
                .ToArray();

            foreach (var subSquare in squaresToAnimate)
            {
                // Check again in case object was destroyed during iteration
                if (subSquare == null || subSquare.gameObject == null || subSquare.transform == null)
                    continue;

                int childOrder = subSquare.name == "CakeBg" ? 2 : 0;

                // Ensure child exists and is valid
                if (subSquare.transform.childCount > childOrder)
                {
                    Transform child = subSquare.transform.GetChild(childOrder);
                    if (child != null && child.gameObject.activeInHierarchy)
                    {
                        Animator animatorPlate = child.GetComponent<Animator>();

                        // Ensure the animator exists and hasn't been destroyed
                        if (animatorPlate != null && animatorPlate.gameObject != null)
                        {
                            // Debug.Log("sallog animator");

                            // Wrap the SetTrigger call in a try–catch (without a yield inside the block)
                            try
                            {
                                animatorPlate.SetTrigger("Shake");
                            }
                            catch (MissingReferenceException e)
                            {
                                Debug.LogWarning("Animator was destroyed before SetTrigger: " + e.Message);
                                continue;
                            }

                            // Yield outside of try–catch
                            yield return new WaitForSeconds(0.01f);

                            // Wrap the ResetTrigger call similarly
                            try
                            {
                                // Check again in case it was destroyed during the wait
                                if (animatorPlate != null)
                                {
                                    animatorPlate.ResetTrigger("Shake");
                                }
                            }
                            catch (MissingReferenceException e)
                            {
                                Debug.LogWarning("Animator was destroyed before ResetTrigger: " + e.Message);
                            }
                        }
                    }
                }

                yield return new WaitForSeconds(0.01f);
            }

            isShakeAnimating = false;
            isAnySquareShaking = false;
        }

        private bool CanDestroySubSquare(Rectangle subRectangle, bool destroyTarget)
        {
            if (subRectangle.cannotBeDestroy && !destroyTarget)
            {
                return false;
            }

            return true;
        }

        private void PlayDestroyEffects(Rectangle subRectangle)
        {
            switch (type)
            {
                case LevelTargetTypes.Grass:
                case LevelTargetTypes.GrassType2:
                    SpawnFirework("FireworkSplash3");
                    break;
                case LevelTargetTypes.BreakableBox:
                    SpawnFirework("FireworkSplash2");
                    break;
            }
        }

        private void SpawnFirework(string fireworkType)
        {
            var partcl = ObjectPoolManager.Instance.GetPooledObject(fireworkType);
            if (partcl != null)
            {
                partcl.transform.position = transform.position;
                partcl.GetComponent<SplashEffectParticles>().SetColor(0);
                partcl.GetComponent<SplashEffectParticles>().RandomizeParticleSeed();
            }
        }

        private Queue<Rectangle> boxProcessingQueue = new Queue<Rectangle>();


        private GameObject InstantiateSubSquareClone(Rectangle subRectangle)
        {
            GameObject subSquareClone = Instantiate(subRectangle.gameObject, subRectangle.transform.position, subRectangle.transform.rotation);
            subSquareClone.transform.localScale = new Vector3(0.7f, 0.7f, 0); // Match scale
            subSquareClone.GetComponent<Rectangle>().enabled = false; // Disable Square script on clone
            return subSquareClone;
        }

        private void SetSortingLayer(GameObject subSquareClone)
        {
            SpriteRenderer spriteRenderer = subSquareClone.GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.sortingLayerName = "Spine"; // Set to the desired sorting layer name
                spriteRenderer.sortingOrder = 100; // Adjust the sorting order if needed
            }
        }

        private void ApplyExplosionAnimation(GameObject subSquareClone, Rectangle subRectangle)
        {
            Vector3 startPos = subSquareClone.transform.position;
            subSquareClone.GetComponent<HelperScripts.FakeShadow>().StartShadowAnimation();

            // Set the fixed radius for consistent throw distance
            float radius = 1.1f; // Ensure the object reaches this distance

            // Generate a random angle to reach the circle boundary
            float angle = Random.Range(0f, Mathf.PI * 2);

            // Calculate the throw direction to ensure it reaches the circle's surface
            Vector3 circularDirection = new Vector3(
                Mathf.Cos(angle) * radius, // X component
                Mathf.Sin(angle) * radius, // Y component
                0 // No Z movement in 2D
            );

            // Calculate peak and landing positions based on circular direction
            Vector3 peak = startPos + circularDirection * 0.6f; // Higher point in trajectory
            peak.y += Random.Range(1.5f, 2f); // Ensure upward boost

            Vector3 landing = startPos + circularDirection * 2f; // Further landing point
            landing.y = -8f; // Below start position for falling effect

            float throwDuration = Random.Range(0.9f, 1.1f); // Consistent throw timing

            var sequence = DOTween.Sequence();

            // Rotate continuously with synchronized timing
            sequence.Append(subSquareClone.transform.DORotate(new Vector3(0, 0, Random.Range(500, 1000)), throwDuration, RotateMode.FastBeyond360)
                    .SetEase(DG.Tweening.Ease.Linear)) // Linear for constant spin
                .Join(subSquareClone.transform
                    .DOJump(landing, 4.5f, 1, throwDuration)
                    .SetEase(DG.Tweening.Ease.InOutSine)) // Smooth rise and fall
                .OnKill(() => Destroy(subSquareClone)); // Cleanup
        }


        private bool _isHandlingBox = false;

        private void HandleBoxSquare()
        {
            if (_isHandlingBox) return; // Prevent duplicate execution
            _isHandlingBox = true;

            if (subSquares.Count == 1)
            {
                if (mainRectangleInGrid.boxChilds != null)
                {
                    foreach (var child in mainRectangleInGrid.boxChilds)
                    {
                        if (child != null)
                        {
                            child.type = LevelTargetTypes.EmptySquare;
                            child.DestroyBlock(true, color: 10);
                        }
                    }
                }

                SpawnFirework("FireworkSplash4");
                mainRectangleInGrid.type = LevelTargetTypes.EmptySquare;
                subSquares.Clear();
                Destroy(mainRectangleInGrid.transform.GetChild(0).gameObject);
                mainRectangleInGrid.DestroyBlock(color: 10);
            }

            _isHandlingBox = false; // Reset flag after completion
        }

        private void HandleBoxSquare2(int color = 0)
        {
            if (subSquares.Count == 1 && (type == LevelTargetTypes.PotionCabinet))
            {
                // Change type of each BoxChild square to EmptySquare
                if (mainRectangleInGrid.boxChilds != null)
                {
                    foreach (var child in mainRectangleInGrid.boxChilds)
                    {
                        if (child != null)
                        {
                            child.type = LevelTargetTypes.EmptySquare;
                            child.DestroyBlock(true);
                        }
                    }
                }

                SpawnFirework("FireworkSplash4");
                mainRectangleInGrid.type = LevelTargetTypes.EmptySquare;
                // Debug.Log("Square: Box square destroyed");

                //   Destroy(mainSquareInGrid);
                mainRectangleInGrid.DestroyBlock();
                subSquares.Clear();
            }
        }

        private void HandleEmptySquare()
        {
            if (subSquares.Count == 0)
            {
                type = LevelTargetTypes.EmptySquare;
            }
        }


        private void FinalizeDestruction()
        {
            CheckBigBlockCleared();

            if (IsFree() && Item == null)
            {
                Item = null;
            }
        }

        public float jumpHeight = 1.6f; // Height of the jump
        public float jumpDuration = 0.7f; // Duration of the jump
        public float fallDistance = 7f; // Distance to the left for falling
        public float rotationDuration = 2f; // Duration of the rotation

        private void AnimateDoor(GameObject door, Vector3 direction)
        {
            // Instantiate a copy of the door
            GameObject doorCopy = Instantiate(door, door.transform.position, door.transform.rotation);
            doorCopy.GetComponent<HelperScripts.FakeShadow>().StartShadowAnimation();
            doorCopy.transform.SetParent(door.transform.parent.parent);
            doorCopy.transform.localScale = new Vector3(1.6f, 1.6f, 1.6f);
            doorCopy.SetActive(true);
            door.SetActive(false);

            Vector3 startPos = doorCopy.transform.position;

            // Set up circular motion parameters
            float radius = 1.1f;
            float angle = direction.x > 0 ? 0 : Mathf.PI; // Angle based on direction

            // Calculate throw direction using circular motion
            Vector3 circularDirection = new Vector3(
                Mathf.Cos(angle) * radius,
                Mathf.Sin(angle) * radius,
                0
            );

            // Calculate final positions
            Vector3 peak = startPos + circularDirection * 0.6f;
            peak.y += 2f; // Add upward boost

            Vector3 landing = startPos + circularDirection * 2f;
            landing.y = -8f; // Set final landing position below

            float throwDuration = .9f;

            var sequence = DOTween.Sequence();

            // Create rotation and jump animation
            sequence.Append(doorCopy.transform.DORotate(new Vector3(0, 0, direction.x > 0 ? 460 : -460), throwDuration, RotateMode.FastBeyond360)
                    .SetEase(DG.Tweening.Ease.Linear))
                .Join(doorCopy.transform
                    .DOJump(landing, 5.5f, 1, throwDuration)
                    .SetEase(DG.Tweening.Ease.InOutSine))
                .OnComplete(() =>
                {
                    doorCopy.SetActive(false);
                    Destroy(doorCopy);
                });
        }

        private IEnumerator PlaycupAnimation()
        {
            if (subSquares.Count != 0)
            {
                var subSquare = GetSubSquare();
                if (subSquare.type == LevelTargetTypes.PlateCabinet)
                {
                    animator = subSquares?[0].GetComponentInChildren<Animator>();
                    // Debug.Log("sallog animator ");
                    animator?.SetTrigger("Broke");

                    foreach (Rectangle square in subSquares)
                    {
                        square?.GetComponent<Animator>().SetTrigger("ShakePlate");
                    }
                }
            }

            yield return new WaitForSeconds(1.06f);
        }

        private Rectangle UpdateType()
        {
            Rectangle subRectangle;
            subRectangle = GetSubSquare();
            if (subSquares.Count > 0)
                type = subRectangle.type;

            if (subSquares.Count == 0)
                type = LevelTargetTypes.EmptySquare;

            if (subRectangle.CanGoInto() && Item == null)
                Item = null;
            return subRectangle;
        }

        public void CheckBigBlockCleared()
        {
            //Debug.Log("Square : Check big block");
            if (GetSubSquare()._isSquareToClear)
                StartCoroutine(CheckClearTarget());
        }

        private IEnumerator CheckClearTarget()
        {
            // // Debug.Log("Square : Check big block");
            MainManager.Instance.checkTarget = true;
            yield return new WaitForSeconds(0.5f);
            if (GetSubSquare().IsCleared())
                MainManager.Instance.levelData.GetTargetsByAction(CollectingTypes.Clear).ForEachY(i =>
                {
                    if (i.extraObject == GetSprite())
                    {
                        i.CheckTarget(new[] { this }, false);
                        if (GetSubSquare() != null)
                            GetSubSquare().enabled = false;
                        destroyIteration = 0;
                        DestroyBlock(true);
                    }
                });
            MainManager.Instance.checkTarget = false;
        }

        public bool IsCleared()
        {
            var enumerable = FindObjectsOfType<Rectangle>().Where(i => i.subSquares.Contains(this));
            if (enumerable.Count() == 0) return false;
            var isCleared = enumerable.All(i => i.subSquares.Last() == this && i.Item?.currentType != ItemsTypes.Eggs && i.type != LevelTargetTypes.Mails);
            return isCleared;
        }

        public void OnCackeAnimEnd()
        {
            // Debug.Log("sallog animator  end");
        }

        /// <summary>
        /// Get sub-square (i.e. layered obstacles)
        /// </summary>
        /// <returns></returns>
        public Rectangle GetSubSquare()
        {
            if (subSquares.Count == 0)
                return this;

            return subSquares?.LastOrDefault();
        }

        public Rectangle GetBoxSubSquare()
        {
            // Early return if no subsquares
            if (!subSquares?.Any() ?? true)
                return this;

            // Define Box position ranges
            var positionRanges = new Dictionary<int, (int start, int length)>
            {
                { 1, (1, 3) }, // Main/center position
                { 2, (4, 2) }, // Right position
                { 3, (6, 2) }, // Bottom position
                { 4, (8, 3) } // Bottom-right position
            };

            // Get range for current Box position
            if (positionRanges.TryGetValue(boxChildPos, out var range))
            {
                return GetLastSquareInRange(range.start, range.length);
            }

            // Fallback to last subsquare if position not found
            return subSquares.LastOrDefault();
        }

        private Rectangle GetLastSquareInRange(int start, int length)
        {
            // Validate range
            if (start >= subSquares.Count)
                return subSquares.LastOrDefault();

            // Get squares in range
            var rangeSquares = subSquares
                .Skip(start)
                .Take(length)
                .LastOrDefault();

            return rangeSquares ?? subSquares.LastOrDefault();
        }

        /// <summary>
        /// Get sub-square (i.e. layered obstacles)
        /// </summary>
        /// <returns></returns>
        public Rectangle GetCupHolderSubSquare()
        {
            if (subSquares.Count == 0)
                return this;

            return subSquares?.LastOrDefault();
        }

        /// <summary>
        /// Get group of squares for cloud animation on different direction levels
        /// </summary>
        /// <param name="groups"></param>
        /// <param name="group"></param>
        /// <param name="forPair"></param>
        /// <returns></returns>
        public List<List<Rectangle>> GetGroupsSquare(List<List<Rectangle>> groups, List<Rectangle> group = null, bool forPair = true)
        {
            var list = GetAllNeghborsCross();
            if (forPair)
            {
                list = list.Where(i => i.direction == direction).ToList();
                if (direction.y == 0)
                    list = list.Where(i => i.col == col).ToList();
                else
                    list = list.Where(i => i.row == row).ToList();
            }
            else
            {
                list = list.Where(i => i.direction == direction).ToList();
            }

            if (group == null)
            {
                foreach (var sq in list)
                {
                    group = groups.Find(i => i.Contains(sq));
                }

                if (group == null)
                {
                    group = new List<Rectangle>();
                    groups.Add(group);
                }
            }

            if (!group.Contains(this))
                group.Add(this);
            list.RemoveAll(i => group.Any(x => x.Equals(i)));
            foreach (var sq in list)
            {
                groups = sq.GetGroupsSquare(groups, group);
            }

            return groups;
        }


        // [HideInInspector]
        public List<Rectangle> squaresGroup;

        /// <summary>
        /// Get square position on the field
        /// </summary>
        /// <returns></returns>
        public Vector2 GetPosition()
        {
            return new Vector2(col, row);
        }

        /// <summary>
        /// check square is bottom
        /// </summary>
        /// <returns></returns>
        public bool IsBottom()
        {
            return orderInSequence == 0;
        }

        public List<Rectangle> GetVerticalNeghbors()
        {
            var sqList = new List<Rectangle>();
            Rectangle nextRectangle = null;
            nextRectangle = GetNeighborBottom();
            if (nextRectangle != null)
                sqList.Add(nextRectangle);
            nextRectangle = GetNeighborTop();
            if (nextRectangle != null)
                sqList.Add(nextRectangle);
            return sqList;
        }

        public List<Rectangle> GetAllNeghborsCross()
        {
            var sqList = new List<Rectangle>();
            Rectangle nextRectangle = null;
            nextRectangle = GetNeighborBottom();
            if (nextRectangle != null && !nextRectangle.IsNone())
                sqList.Add(nextRectangle);
            nextRectangle = GetNeighborTop();
            if (nextRectangle != null && !nextRectangle.IsNone())
                sqList.Add(nextRectangle);
            nextRectangle = GetNeighborLeft();
            if (nextRectangle != null && !nextRectangle.IsNone())
                sqList.Add(nextRectangle);
            nextRectangle = GetNeighborRight();
            if (nextRectangle != null && !nextRectangle.IsNone())
                sqList.Add(nextRectangle);
            return sqList;
        }

        /// <summary>
        /// Have square solid obstacle above, used for diagonally falling items animation
        /// </summary>
        /// <returns></returns>
        public bool IsHaveSolidAbove()
        {
            if (isEnterPoint) return false;
            var seq = sequenceBeforeThisSquare;
            return seq.Any(square => square != this && (square.GetSubSquare().CanGoOut() == false || square.GetSubSquare().CanGoInto() == false ||
                                                        IsUndestroyable() || square.IsNone()));
        }

        /// <summary>
        /// Have square item above
        /// </summary>
        /// <returns></returns>
        public bool IsHaveItemsAbove()
        {
            if (isEnterPoint) return false;
            var seq = sequenceBeforeThisSquare;
            foreach (var square in seq)
            {
                if (square.Item ?? false)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Have square falling item above
        /// </summary>
        /// <returns></returns>
        public bool IsHaveFallingItemsAbove()
        {
            if (isEnterPoint) return false;
            var seq = sequenceBeforeThisSquare;
            foreach (var square in seq)
            {
                if (square.Item && !square.Item.falling && !square.Item.needFall)
                    return true;
            }

            return false;
        }

        /// Have square falling item above
        public bool IsItemAbove()
        {
            if (isEnterPoint) return false;
            var seq = sequenceBeforeThisSquare;
            foreach (var square in seq)
            {
                if (!square.IsFree())
                    return false;
                if (square.Item)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Methods for the editor
        /// </summary>
        /// <param name="sqType"></param>
        /// <returns></returns>
        public static GameObject[] GetBlockPrefab(LevelTargetTypes sqType, int layer = -1)
        {
            var list = new List<GameObject>();
            var item1 = Resources.Load("Blocks/" + sqType) as GameObject;
            var layeredBlock = item1?.GetComponent<LayeredBlock>();
            if (layeredBlock != null)
            {
                int range = layer == -1 ? layeredBlock.layers.Length : layer + 1;
                list.AddRange(layeredBlock.layers.Select(i => i.gameObject).Take(range));
            }

            if (list.Count() == 0 && item1 != null) list.Add(item1);
            return list.ToArray();
        }

        public static int GetLayersCount(LevelTargetTypes sqType)
        {
            var layers = 1;
            var item1 = Resources.Load("Blocks/" + sqType) as GameObject;
            layers = item1?.GetComponent<LayeredBlock>()?.layers?.Length ?? 0;
            if (layers == 0) layers = 1;
            return layers;
        }

        public int GetLayersCount()
        {
            int layers = 0;

            if (subSquares != null && subSquares.Count > 0)
            {
                layers = subSquares.Count;
            }
            else
            {
                layers = 1; // Considering the base layer
            }

            return layers;
        }


        public static Texture2DSize GetSquareTexture(LevelTargetTypes sqType)
        {
            var blockPrefab = GetBlockPrefab(sqType);
            if (blockPrefab.Any())
            {
                var obj = blockPrefab?.First();
                return new Texture2DSize(obj?.GetComponent<SpriteRenderer>()?.sprite?.texture, obj.GetComponent<Rectangle>().sizeInSquares);
            }

            return new Texture2DSize(null, Vector2Int.zero);
        }

        public static List<Texture2DSize> GetSquareTextures(RectangleBlocks sqBlock, int layer)
        {
            var resultList = new List<Texture2DSize>();
            sqBlock.SortMergeBlocks();
            Tuple<GameObject, int>[] listBlocks = new Tuple<GameObject, int>[0];
            if (!sqBlock.blocks.Any())
            {
                resultList.AddRange(GetBlockPrefab(sqBlock.block)
                    .Select(i => new Texture2DSize(i.GetComponent<SpriteRenderer>().sprite.texture, i.GetComponent<Rectangle>().sizeInSquares, 0)).ToArray());
                if (sqBlock.obstacle != LevelTargetTypes.NONE)
                {
                    resultList.AddRange(GetBlockPrefab(sqBlock.obstacle)
                        .Select(i => new Texture2DSize(i.GetComponent<SpriteRenderer>().sprite.texture, i.GetComponent<Rectangle>().sizeInSquares, 0)).ToArray());
                }
            }
            else
            {
                List<Tuple<GameObject, int>> list1 = new List<Tuple<GameObject, int>>();
                int typeCounter = 0;
                for (int index = 0; index <= Mathf.Clamp(layer, 0, sqBlock.blocks.Count - 1); index++)
                {
                    var sqBlockBlock = sqBlock.blocks[index];
                    if (index > 0 && sqBlockBlock.levelTargetType == sqBlock.blocks[index - 1].levelTargetType) typeCounter++;
                    else typeCounter = 0;
                    var blockPrefab = GetBlockPrefab(sqBlockBlock.levelTargetType);
                    // show only 2*2 bg in level editor for Box item
                    if (sqBlockBlock.levelTargetType == LevelTargetTypes.PlateCabinet)
                    {
                        resultList.Add(new Texture2DSize(blockPrefab[0].GetComponent<SpriteRenderer>().sprite.texture,
                            blockPrefab[0].GetComponent<Rectangle>().sizeInSquares, index, sqBlockBlock.rotate));
                    }
                    else
                    {
                        if (typeCounter < blockPrefab.Length && !sqBlockBlock.anotherSquare)
                        {
                            resultList.Add(new Texture2DSize(blockPrefab[typeCounter].GetComponent<SpriteRenderer>().sprite.texture,
                                blockPrefab[typeCounter].GetComponent<Rectangle>().sizeInSquares,
                                index, sqBlockBlock.rotate));
                        }
                    }
                }

                if (list1.Any()) listBlocks = list1.ToArray();
            }

            return resultList;
        }

        public Sprite GetSprite()
        {
            subSquares = subSquares.WhereNotNull().ToList();
            if (subSquares.Count > 0)
                return subSquares.Last().GetComponent<SpriteRenderer>().sprite;
            return GetComponent<SpriteRenderer>().sprite;
        }

        public SpriteRenderer[] GetSpriteRenderers()
        {
            IEnumerable<SpriteRenderer> sq = null;
            sq = subSquares.Count > 0 ? subSquares.WhereNotNull().Select(i => i.GetComponent<SpriteRenderer>()) : new[] { GetComponent<SpriteRenderer>() };
            return sq.ToArray();
        }

        public SpriteRenderer GetSpriteRenderer()
        {
            if (subSquares.LastOrDefault() != null && subSquares.Count > 0)
                return subSquares.LastOrDefault()?.GetComponent<SpriteRenderer>();
            return GetComponent<SpriteRenderer>();
        }

        public void SetBorderDirection()
        {
            var square = GetNeighborRight();
            if (IsNone()) return;
            if (direction + (square?.direction ?? direction) == Vector2.zero && (!square?.IsNone() ?? false))
            {
                SetBorderDirection(Vector2.right);
            }

            square = GetNeighborBottom();
            if (direction + (square?.direction ?? direction) == Vector2.zero && (!square?.IsNone() ?? false))
            {
                SetBorderDirection(Vector2.down);
            }
        }

        public List<Vector2> directionRestriction = new List<Vector2>();
        public int orderInSequence; //latest square has 0, enter square has last number
        public List<Rectangle> sequence = new List<Rectangle>();

        [HideInInspector] public int destroyIteration;

        //private bool dontDestroyOnThisMove;
        public Rectangle[] sequenceBeforeThisSquare;

        public GameObject border;

        //Added_feature
        [FormerlySerializedAs("mainSquare")] public Rectangle mainRectangle;
        private GameObject _chopperTarget;
        private bool _isSquareToClear;
        public int hashCode;
        [HideInInspector] public bool destroyedTarget;
        public Sprite separatorSprite;
        [HideInInspector] public bool bottomRow;
        public IColorGettable ColorGettable;

        private void SetBorderDirection(Vector2 dir)
        {
            var border = new GameObject();
            var spr = border.AddComponent<SpriteRenderer>();
            spr.sortingOrder = 1;
            spr.sprite = borderSprite;
            border.transform.SetParent(transform);
            border.transform.localScale = Vector3.one;
            if (dir != Vector2.down && dir != Vector2.up) border.transform.rotation = Quaternion.Euler(0, 0, 180);
            border.transform.localPosition = Vector2.zero + dir * (MainManager.Instance.squareWidth + 0.1f);
            SetSquareRestriction(dir);
        }

        public void SetSquareRestriction(Vector2 dir, bool neibhour = false)
        {
            directionRestriction.Add(dir);
            if (neibhour)
                return;
            if (dir == Vector2.right)
                GetNeighborRight(false)?.SetSquareRestriction(Vector2.left, true);
            if (dir == Vector2.left)
                GetNeighborLeft(false)?.SetSquareRestriction(Vector2.right, true);
            if (dir == Vector2.down)
                GetNeighborBottom(false)?.SetSquareRestriction(Vector2.up, true);
            if (dir == Vector2.up)
                GetNeighborTop(false)?.SetSquareRestriction(Vector2.down, true);
        }

        public void SetSeparators()
        {
            var squareBlocks = field.fieldData.levelSquares[row * field.fieldData.maxCols + col];
            ;
            for (int i = 0; i < squareBlocks.separatorIndexes.Length; i++)
            {
                if (!squareBlocks.separatorIndexes[i]) continue;
                var border = new GameObject();
                var spr = border.AddComponent<SpriteRenderer>();
                spr.sortingOrder = 2;
                spr.sprite = separatorSprite;
                border.transform.SetParent(transform);
                border.transform.localScale = Vector3.one;
                var dir = squareBlocks.GetSeparatorOffsetSimple(i);
                border.transform.localPosition = Vector2.zero + dir * (MainManager.Instance.squareWidth + 0.1f);

                if (i == 0)
                {
                    border.transform.rotation = Quaternion.Euler(0, 0, 90);
                }

                if (i == 1)
                {
                    border.transform.rotation = Quaternion.Euler(0, 0, 90);
                }

                SetSquareRestriction(dir);
            }
        }

        public FieldBoard GetField()
        {
            return field;
        }

        void OnDrawGizmos()
        {
            if (nextRectangle != null)
            {
                if (teleportDestination == null)
                    SetNextArrow();
                else
                    SetNextArrowTeleport();
            }
        }

        private void SetNextArrowTeleport()
        {
            Gizmos.color = Color.green;
            var pivot = nextRectangle.transform.position + Vector3.left * .1f;
            Gizmos.DrawLine(transform.position + Vector3.left * .1f, pivot);
            RotateGizmo(pivot, 20);
            RotateGizmo(pivot, -20);
        }

        private void SetNextArrow()
        {
            Gizmos.color = Color.blue;
            var pivot = nextRectangle.transform.position;
            Gizmos.DrawLine(transform.position, pivot);
            RotateGizmo(pivot, 20);
            RotateGizmo(pivot, -20);
        }

        private void RotateGizmo(Vector3 pivot, float angle)
        {
            var rightPoint = Vector3.zero;
            var dir = (transform.position - pivot) * 0.2f;
            dir = Quaternion.AngleAxis(angle, Vector3.back) * dir;
            rightPoint = dir + pivot;
            Gizmos.DrawLine(pivot, rightPoint);
        }

        public void SetOutline()
        {
            if (GetNeighborBottom()?.IsNone() ?? true) Instantiate(border, transform.position, Quaternion.Euler(0, 0, 90), transform);
            if (GetNeighborTop()?.IsNone() ?? true) Instantiate(border, transform.position, Quaternion.Euler(0, 0, -90), transform);
            if (GetNeighborLeft()?.IsNone() ?? true) Instantiate(border, transform.position, Quaternion.Euler(0, 0, 0), transform);
            if (GetNeighborRight()?.IsNone() ?? true) Instantiate(border, transform.position, Quaternion.Euler(0, 0, 180), transform);
        }

        public Rectangle DeepCopy()
        {
            var other = (Rectangle)MemberwiseClone();
            other.Item = Item.DeepCopy();
            return other;
        }


        void RemoveComponent<T>(GameObject obj) where T : Component
        {
            // Check if the object has the component
            T component = obj.GetComponent<T>();
            if (component != null)
            {
                // Remove the component
                Destroy(component);
                // Debug.Log(typeof(T).Name + " component removed.");
            }
            else
            {
                // Debug.Log(typeof(T).Name + " component not found.");
            }
        }

        LevelTargetTypes ISquareItemCommon.GetType()
        {
            return GetSubSquare().type;
        }

        public void DestroyByStriped(bool WithoutShrink = false, bool destroyNeighbours = false)
        {
            //throw new NotImplementedException();
            DestroyBlock(destroyNeighbours);
        }

        public GameObject GetChopperTarget
        {
            get => _chopperTarget;
            set => _chopperTarget = value;
        }

        public GameObject GetGameObject => gameObject;
        public Item GetItem => Item;

        public int TargetByChopperIndex
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }
    }

    public class Texture2DSize
    {
        public Texture2D Texture2D;
        public Vector2Int size = Vector2Int.one;
        public Vector2Int offset = Vector2Int.zero;
        public int order;
        public bool rotate;
        public Vector2 sizeMod = Vector2.one;

        public Texture2DSize(Texture2D texture2D, Vector2Int v, bool rotate = false)
        {
            Texture2D = texture2D;
            size = v;
            this.rotate = rotate;
        }

        public Texture2DSize(Texture2D texture2D, Vector2Int v, int _order, bool rotate = false, Vector2Int _offset = default)
        {
            Texture2D = texture2D;
            size = v;
            order = _order;
            offset = _offset;
            this.rotate = rotate;
        }
    }
}
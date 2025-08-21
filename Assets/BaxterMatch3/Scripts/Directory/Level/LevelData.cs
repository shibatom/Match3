

using System;
using System.Collections.Generic;
using System.Linq;
using Internal.Scripts;
using Internal.Scripts.System;
using Internal.Scripts.Blocks;
using Internal.Scripts.Items;
using Internal.Scripts.Items.Interfaces;
using Internal.Scripts.TargetScripts.TargetEditor;
using Internal.Scripts.TargetScripts.TargetSystem;
using StorySystem;
using UnityEngine;
using UnityEngine.Serialization;

namespace Internal.Scripts.Level
{
    /// <summary>
    /// Level data for level editor
    /// </summary>
    [Serializable]
    public class LevelData
    {
        //Add_feature
        public bool SpawnerExits;

        public string Name;
        public static LevelData THIS;

        /// level number
        public int levelNum;

        private int hashCode;

        /// fields data
        public List<FieldData> fields = new List<FieldData>();

        /// target container keeps the object should be collected, its count, sprite, color
        [SerializeField] public TargetContainer target;

        /// target manager reference
        [SerializeField] public Target targetObject;

        public int targetIndex;

        // public static TargetContainer targetContainer;
        /// moves or time
        public LIMIT limitType;

        // StoryConversation for level
        [SerializeField] public Conversation conversation;

        // CardConfig for level
        [SerializeField] public CardConfig card;

        public int[] ingrCountTarget = new int[2];

        /// moves amount or seconds
        public int limit = 25;

        /// color amount
        public int colorLimit = 5;

        /// score amount for reach 1 star
        public int star1 = 100;

        /// score amount for reach 2 stars
        public int star2 = 300;

        /// score amount for reach 3 stars
        public int star3 = 500;

        /// pre generate Chopper
        public bool enableChopper;

        public int maxRows
        {
            get { return GetField().maxRows; }
            set { GetField().maxRows = value; }
        }

        public int maxCols
        {
            get { return GetField().maxCols; }
            set { GetField().maxCols = value; }
        }

        public int selectedTutorial;

        public int currentSublevelIndex;
        private TargetEditorScriptable targetEditorObject;
        private List<TargetContainer> targetEditorArray;

        /// target container keeps the object should be collected, its count, sprite, color
        public List<SubTargetContainer> subTargetsContainers = new List<SubTargetContainer>();

        public List<TargetCounter> TargetCounters;

        public bool generateIngredientOnlyFromSpawner;

        public FieldData GetField()
        {
            return fields[currentSublevelIndex];
        }

        public FieldData GetField(int index)
        {
            currentSublevelIndex = index;
            return fields[index];
        }

        public LevelData(bool isPlaying, int currentLevel)
        {
            hashCode = GetHashCode();
            levelNum = currentLevel;
            // Debug.Log("Loaded " + hashCode);
            Name = "Level " + levelNum;
            THIS = this;
            LoadTargetObject();
            // if (isPlaying)
            //     targetEditorObject = LevelManager.This.targetEditorScriptable;
            // else
            //     targetEditorObject = AssetDatabase.LoadAssetAtPath("Assets/BaxterMatch3/Directory/Scriptable/TargetEditorScriptable.asset", typeof(TargetEditorScriptable)) as TargetEditorScriptable;
        }

        public void LoadTargetObject()
        {
            targetEditorObject = Resources.Load("Levels/TargetEditorScriptable") as TargetEditorScriptable;
            targetEditorArray = targetEditorObject.targets;
        }

        public Target GetTargetObject()
        {
            return targetObject;
        }

        public Conversation GetConversation()
        {
            return conversation;
        }

        public CardConfig GetCard()
        {
            return card;
        }

        public RectangleBlocks GetBlock(int row, int col)
        {
            return GetField().levelSquares[row * GetField().maxCols + col];
        }

        public RectangleBlocks GetBlock(Vector2Int vec)
        {
            return GetBlock(vec.y, vec.x);
        }

        public FieldData AddNewField()
        {
            var fieldData = new FieldData();
            fields.Add(fieldData);
            return fieldData;
        }

        public void RemoveField()
        {
            FieldData field = fields.Last();
            fields.Remove(field);
        }

        public Sprite[] GetTargetSprites()
        {
            return GetTargetContainersForUI().Where(i => i.extraObject && i.extraObject is Sprite).Select(i => (Sprite)i.extraObject).ToArray();
        }

        //Gets targets except Star, or only alone Star
        public TargetCounter[] GetTargetContainersForUI()
        {
            var list = TargetCounters;
            if (TargetCounters.Count > 1) list = TargetCounters.Where(i => !i.IsTargetStars()).ToList();
            return list.OrderBy(i => i.count).ToArray();
        }

        public TargetCounter[] GetTargetCounters() => TargetCounters.ToArray();

        /// <summary>
        /// deprecated
        /// </summary>
        /// <returns></returns>
        public string[] GetTargetsNames() => targetEditorArray?.Select(i => i.name).ToArray();

        public TargetCounter[] GetTargetsByAction(CollectingTypes action)
        {
            List<TargetCounter> result = new List<TargetCounter>();
            foreach (var target in TargetCounters)
            {
                if (target.collectingAction == action)
                {
                    result.Add(target);
                }
            }

            return result.ToArray();
        }

        /// <summary>
        /// deprecated
        /// </summary>
        /// <returns></returns>
        public string[] GetSubTargetNames() => target.prefabs.Select(i => i.name).ToArray();

        public GameObject[] GetSubTargetPrefabs()
        {
            var layerBlockPrefabs = target.prefabs.Where(i => i.GetComponent<LayeredBlock>() != null).SelectMany(i => i.GetComponent<LayeredBlock>().layers).Select(i => i.gameObject).ToArray();
            var mergedList = layerBlockPrefabs.Concat(target.prefabs.Where(i => i.GetComponent<LayeredBlock>() == null)).ToArray();
            return mergedList;
        }

        public int GetTargetIndex()
        {
            var v = targetEditorArray.FindIndex(i => i.name == target.name);
            if (v < 0)
            {
                SetTarget(0);
                v = 0;
            }

            return v;
        }

        public TargetContainer GetTargetEditor() => targetEditorArray.Find(i => i.name == target.name);
        public TargetContainer GetTargetByNameEditor(string targetName) => targetEditorArray.Find(i => i.name == targetName);
        public bool IsTargetByNameExist(string targetName) => TargetCounters.Any(i => i.targetLevel.name == targetName);
        public bool IsTargetByActionExist(CollectingTypes action) => TargetCounters.Any(i => i.targetLevel.collectAction == action);
        public int GetTargetIndex(string targetName) => targetEditorArray.FindIndex(i => i.name == targetName);

        public TargetContainer GetFirstTarget(bool skipStars)
        {
            Debug.Log("TargetContainer Getting available targets...");
            Debug.Log($"TargetContainer TargetCounters contains: {string.Join(", ", TargetCounters.Select(t => $"{t.targetLevel.name} (Count:{t.count}, Action:{t.collectingAction})"))}");

            var targets = skipStars ? TargetCounters.Where(i => !i.IsTargetStars() && i.collectingAction != CollectingTypes.Spread).ToList() : TargetCounters.ToList();
            Debug.Log($"TargetContainer Found {targets.Count} targets after filtering (skipStars={skipStars})");

            if (!targets.Any())
            {
                Debug.Log("TargetContainer: No valid targets found");
                return null;
            }

            // Filter targets with a count greater than 0
            var targetsWithCount = targets.Where(t => t.count > 0).ToList();
            TargetContainer selectedTarget;

            if (targetsWithCount.Any())
            {
                // Randomly select one target among those with count > 0
                int randomIndex = UnityEngine.Random.Range(0, targetsWithCount.Count);
                selectedTarget = targetsWithCount[randomIndex].targetLevel;
                Debug.Log($"TargetContainer Randomly selected target with count: {selectedTarget.name} (Count:{targetsWithCount[randomIndex].count}, Action:{selectedTarget.collectAction})");
            }
            else
            {
                // If no target has a count > 0, randomly select any target from the list
                int randomIndex = UnityEngine.Random.Range(0, targets.Count);
                selectedTarget = targets[randomIndex].targetLevel;
                Debug.Log($"TargetContainer Randomly selected available target: {selectedTarget.name} (Count:0, Action:{selectedTarget.collectAction})");
            }

            return selectedTarget;
        }


        public void SetTargetFromArray()
        {
            SetTarget(targetEditorArray.FindIndex(x => x.name == target.name));
        }

        public void SetTarget(int index)
        {
            if (targetEditorObject == null || targetEditorArray == null) LoadTargetObject();
            subTargetsContainers.Clear();
            if (index < 0) index = 0;
            target = targetEditorArray[index];
            targetIndex = index;
            try
            {
                targetObject = (Target)Activator.CreateInstance(Type.GetType("Internal.Scripts.TargetScripts." + target.name));
                Debug.Log("create target " + targetObject);
                var subTargetPrefabs = GetSubTargetPrefabs();
                if (subTargetPrefabs.Length > 1)
                {
                    foreach (var _target in subTargetPrefabs)
                    {
                        var component = _target.GetComponent<Item>();
                        Sprite extraObject = null;
                        if (component)
                        {
                            extraObject = component.SprRenderer.FirstOrDefault().sprite;
                        }

                        subTargetsContainers.Add(new SubTargetContainer(_target, 0, extraObject));
                    }
                }
                else if (subTargetPrefabs.Length > 0 && subTargetPrefabs[0].GetComponent<ColorReciever>())
                {
                    foreach (var item in subTargetPrefabs[0].GetComponent<ColorReciever>().GetSprites(levelNum))
                    {
                        subTargetsContainers.Add(new SubTargetContainer(subTargetPrefabs[0], 0, item));
                    }
                }
                else if (subTargetPrefabs.Length > 0)
                {
                    foreach (var _target in subTargetPrefabs)
                    {
                        var component = _target.GetComponent<Item>();
                        Sprite extraObject = null;
                        if (component)
                        {
                            extraObject = component.SprRenderer.FirstOrDefault().sprite;
                        }

                        subTargetsContainers.Add(new SubTargetContainer(_target, 0, extraObject));
                    }
                }
            }
            catch (Exception)
            {
                Debug.LogError("Check the target name or create class " + target.name);
            }
        }

        public void InitTargetObjects(bool forPlay = false)
        {
            if (forPlay)
            {
                var targetLevel = Resources.Load<TargetLevel>("Levels/Targets/TargetLevel" + levelNum);
                var _TargetCounters = targetLevel.targets.Select(i => new TargetCounter(i.targetType.GetTarget().prefabs.FirstOrDefault(), i.CountDrawer.count,
                    i.sprites.Select(o => o.icon).Distinct().ToArray(), -1, i.targetType.GetTarget(), i.NotFinishUntilMoveOut, i)).ToList();
                TargetCounters = _TargetCounters.ToList();
                TargetCounters.RemoveAll(i => i.targetLevel.setCount == SetCount.Manually && i.count == 0);
            }

            targetObject.subTargetContainers = subTargetsContainers.ToArray();
            if (targetObject.subTargetContainers.Length > 0)
                targetObject.InitTarget(this);
            else Debug.LogError("set " + target.name + " more than 0");
        }

        public void SetItemTarget(Item item)
        {
            foreach (var _subTarget in TargetCounters)
            {
                if (_subTarget.targetPrefab && item.CompareTag(_subTarget.targetPrefab.tag) && _subTarget.count > 0 && item.gameObject.GetComponent<TargetComponent>() == null)
                {
                    item.gameObject.AddComponent<TargetComponent>();
                }
            }
        }

        public void SetSquareTarget(GameObject gameObject, LevelTargetTypes _sqType, GameObject prefabLink)
        {
            // if (_sqType.ToString().Contains(target.name))
            {
                var subTargetContainer = TargetCounters.FirstOrDefault(i => i.targetPrefab != null && _sqType.ToString() == i.targetPrefab.name);
                if (subTargetContainer != null)
                {
                    subTargetContainer.changeCount(1);
                    gameObject.AddComponent<TargetComponent>();
                }
            }
        }

        public string GetSaveString()
        {
            var str = "";
            foreach (var item in subTargetsContainers)
            {
                str += item.GetCount() + "/";
            }

            return str;
        }

        public LevelData DeepCopy(int level)
        {
            LoadTargetObject();
            var other = (LevelData)MemberwiseClone();
            other.hashCode = other.GetHashCode();
            other.levelNum = level;
            other.Name = "Level " + other.levelNum;
            other.fields = new List<FieldData>();
            for (var i = 0; i < fields.Count; i++)
            {
                other.fields.Add(fields[i].DeepCopy());
            }

            if (targetEditorArray.Count > 0)
            {
                var matchingTarget = targetEditorArray.FirstOrDefault(x => x.name == target.name);
                if (matchingTarget != null)
                    other.target = matchingTarget;
                else
                    other.target = target.DeepCopy(); // fallback
            }
            else
            {
                other.target = target.DeepCopy();
            }

            if (targetObject != null)
                other.targetObject = targetObject.DeepCopy();
            other.subTargetsContainers = new List<SubTargetContainer>();
            for (var i = 0; i < subTargetsContainers.Count; i++)
            {
                other.subTargetsContainers.Add(subTargetsContainers[i].DeepCopy());
            }

            other.targetObject = (Target)Activator.CreateInstance(Type.GetType("Internal.Scripts.TargetScripts." + target.name));
            return other;
        }

        public LevelData DeepCopyForPlay(int level)
        {
            LevelData data = DeepCopy(level);
            THIS = data;
            return data;
        }

        public bool IsTotalTargetReached()
        {
            return TargetCounters.All(i => i.IsTotalTargetReached());
        }

        public bool IsTargetReachedSublevel()
        {
            return TargetCounters.All(i => i.IsTargetReachedSubLevel());
        }

        public bool WaitForMoveOut() => TargetCounters.Any(i => i.NotFinishUntilMoveOut);

        public void CheckLayers()
        {
            foreach (var fieldData in fields) fieldData.ConvertToLayered();
        }
    }

    /// <summary>
    /// Field data contains field size and square array
    /// </summary>
    [Serializable]
    public class FieldData
    {
        private int hashCode;
        public int subLevel;
        public int maxRows;
        public int maxCols;
        public bool noRegenLevel; //no regenerate level if no matches possible
        public bool scrollLevel; //scroll level
        public RectangleBlocks[] levelSquares = new RectangleBlocks[81];
        internal int row;
        public int bombTimer = 15;
        public int layers;
        public bool switchSublevelNoMatch;

        public FieldData()
        {
            hashCode = GetHashCode();
        }

        public FieldData DeepCopy()
        {
            var other = (FieldData)MemberwiseClone();
            other.levelSquares = new RectangleBlocks[levelSquares.Length];
            for (var i = 0; i < levelSquares.Length; i++)
            {
                other.levelSquares[i] = levelSquares[i].DeepCopy();
            }

            other.hashCode = other.GetHashCode();

            return other;
        }

        public void ConvertToLayered()
        {
            if (layers != 0) return;
            foreach (var squareBlockse in levelSquares)
            {
                if (squareBlockse.blocks.Any() || squareBlockse.block == LevelTargetTypes.NONE) continue;
                squareBlockse.blocks.Add(new SquareTypeLayer { levelTargetType = LevelTargetTypes.EmptySquare });
                AddLayers(squareBlockse, squareBlockse.block, squareBlockse.blockLayer);
                AddLayers(squareBlockse, squareBlockse.obstacle, squareBlockse.obstacleLayer);
            }

            layers = levelSquares.Max(i => i.blocks.Count);
        }

        private static void AddLayers(RectangleBlocks rectangleBlockse, LevelTargetTypes type, int layers)
        {
            if (type == LevelTargetTypes.NONE || type == LevelTargetTypes.EmptySquare) return;
            for (int i = 0; i < layers; i++) rectangleBlockse.blocks.Add(new SquareTypeLayer { levelTargetType = type });
        }
    }

    /// <summary>
    /// Square blocks uses in editor
    /// </summary>
    [Serializable]
    public class RectangleBlocks
    {
        public List<SquareTypeLayer> blocks = new List<SquareTypeLayer>();

        //Added_feature
        public List<SingularSpawn> spawners = new List<SingularSpawn>();
        public LevelTargetTypes block;
        public int blockLayer = 1;
        public LevelTargetTypes obstacle;
        public int obstacleLayer = 1;
        public Vector2Int position;
        public Vector2 direction;
        public bool enterSquare;
        public bool cantGenInto;
        public bool isEnterTeleport;
        public Vector2Int teleportCoordinatesLinked = new Vector2Int(-1, -1);
        public Vector2Int teleportCoordinatesLinkedBack = new Vector2Int(-1, -1);
        public Rect guiRect;
        public EditorItems item;
        public bool[] separatorIndexes = new bool[4];

        public RectangleBlocks DeepCopy()
        {
            var other = (RectangleBlocks)MemberwiseClone();
            return other;
        }

        public void MergeEmptySquares()
        {
            if (blocks.Last().levelTargetType != LevelTargetTypes.EmptySquare) return;
            for (int i = blocks.Count - 1; i >= 1; i--)
            {
                if (blocks[i].levelTargetType != LevelTargetTypes.EmptySquare) break;
                else blocks.RemoveAt(i);
            }
        }

        public void SortMergeBlocks()
        {
            var array = Resources.LoadAll<BindLayer>("Blocks");
            var list = array.OrderBy(i => i.order).ToList();
            var blocks1 = blocks.Where(i => i.levelTargetType != LevelTargetTypes.NONE).Select(i => new
            {
                block = i, layered = list.Find(x => x.name == i.levelTargetType.ToString()).GetComponent<LayeredBlock>()
                                     != null
            });
            var b1 = blocks1.Where(i => i.layered == false).DistinctBy(i => i.block.levelTargetType);
            var b2 = blocks1.Where(i => i.layered);
            blocks = b1.Concat(b2).Select(i => i.block).ToList();
            blocks.Sort((a, b) =>
            {
                if (list.FindIndex(i => i.name == a.levelTargetType.ToString()) < list.FindIndex(i => i.name == b.levelTargetType.ToString()))
                    return -1;
                else if (list.FindIndex(i => i.name == a.levelTargetType.ToString()) == list.FindIndex(i => i.name == b.levelTargetType.ToString()))
                    return 0;
                return 1;
            });
        }

        public bool IsSeparatorRotated(int separatorIndex)
        {
            if (separatorIndex == 0 || separatorIndex == 1) return true;
            return false;
        }

        public Vector2Int GetSeparatorOffset(int separatorIndex)
        {
            var v = separatorIndex switch
            {
                0 => Vector2Int.left,
                1 => Vector2Int.right * 45,
                2 => Vector2Int.up * -5,
                3 => Vector2Int.down * -40,
                _ => Vector2Int.zero
            };
            return v;
        }

        public List<Texture2DSize> GetSeparatorTexture(Texture2D texture)
        {
            List<Texture2DSize> texture2DSizeList = new List<Texture2DSize>();
            for (int i = 0; i < separatorIndexes.Length; i++)
            {
                if (!separatorIndexes[i]) continue;
                var separatorRotated = IsSeparatorRotated(i);
                var texture2DSize = new Texture2DSize(texture, Vector2Int.one, 10, separatorRotated, GetSeparatorOffset(i)) { sizeMod = new Vector2(1, 0.1f) };
                if (separatorRotated) texture2DSize.sizeMod = Quaternion.Euler(0, 0, 90) * texture2DSize.sizeMod;
                texture2DSizeList.Add(texture2DSize);
            }

            return texture2DSizeList;
        }

        public Vector2 GetSeparatorOffsetSimple(int i)
        {
            var v = i switch
            {
                0 => Vector2.left,
                1 => Vector2.right,
                2 => Vector2.up,
                3 => Vector2.down,
                _ => Vector2.zero
            };
            return v;
        }
    }


    [Serializable]
    public struct SquareTypeLayer
    {
        [FormerlySerializedAs("squareType")] public LevelTargetTypes levelTargetType;

        //for big targets, not draw texture in editor
        public bool anotherSquare;
        public Vector2Int originalPos;
        public bool rotate;
        public Vector2Int size;
    }

    [Serializable]
    public class SingularSpawn
    {
        public string SpawnId;
        public Vector2Int position;
        public Vector2 direction;
        public float SpawnPersentage;
        public float SpawnPersentage_2;
        public GredientGenerator IngredentSpawner_01 = new GredientGenerator();
        public GredientGenerator IngredentSpawner_02 = new GredientGenerator();
        public Spawners SpawnersType;
        public Spawners SpawnersType_2;
        public RotationType rotationType;
    }

    [Serializable]
    public class GredientGenerator
    {
        public bool Ingredient_01, Ingredient_02;
        public float Ingredient_01_persentage, Ingredient_02_persentage;
    }

    public enum RotationType
    {
        Top,
        Bottom,
        Left,
        right
    }

    /// <summary>
    /// Item for editor uses in editor
    /// </summary>
    [Serializable]
    public class EditorItems
    {
        public int Color;
        public ItemsTypes ItemType;
        public Texture2D Texture;
        public ColorReciever colors;
        public IItemInterface ItemInterface;
        public GameObject Item;
        public bool EnableChopperTargets;
        public Vector2Int[] TargetChopperPositions;
        public int order;

        public EditorItems DeepCopy()
        {
            var other = (EditorItems)MemberwiseClone();
            return other;
        }

        public void SetColor(int color, int currentLevel)
        {
            Color = color;
            if (colors && colors.GetSprites(currentLevel).Count() > color)
                Texture = colors.GetSprites(currentLevel)[color].texture;
        }
    }
}


using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Internal.Scripts;
using Internal.Scripts.Blocks;
using Internal.Scripts.Effects;
using Internal.Scripts.Items;
using Internal.Scripts.Items.Interfaces;
using Internal.Scripts.System;
using Internal.Scripts.System.Combiner;
using Internal.Scripts.TargetScripts.TargetSystem;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Internal.Scripts.Level
{
    /// <summary>
    /// Field object
    /// </summary>
    public class FieldBoard : MonoBehaviour
    {
        // public SquareBlocks[] fieldData.levelSquares = new SquareBlocks[81];
        public GameObject squarePrefab;
        public Sprite squareSprite1;
        public GameObject outline1;
        public GameObject outline2;
        public GameObject outline3;

        // public int fieldData.maxRows = 9;
        // public int fieldData.maxCols = 9;
        public float squareWidth = 1.2f;
        public float squareHeight = 1.2f;
        public Vector2 firstSquarePosition;
        public Rectangle[] squaresArray;
        public Transform GameField;
        public Hashtable countedSquares = new Hashtable();
        public FieldData fieldData;
        private GameObject pivot;

        public int enterPoints;
        public bool IngredientsByEditor;
        private IColorGettable _colorGettable;

        // Use this for initialization
        private void OnEnable()
        {
            GameField = transform;
        }

        /// <summary>
        /// Initialize field
        /// </summary>
        public void CreateField(IColorGettable gettable)
        {
            _colorGettable = gettable;
            bool chessColor = false;

            InitializeSquares(ref chessColor);
            SetSquareTypes();
            SetupSquarePositions();
            ApplySquareSettings();

            SetOrderInSequence();
            MovingCloudEffect.SetGroupSquares(squaresArray);
            SetPivot();
            SetPosY(0);
            GenerateNewItems(false);

            // CreateTestItems();

            StartCoroutine(DeleteMatches());

            if (MainManager.Instance.enableChopper)
                CreateChopper();
        }

        private void InitializeSquares(ref bool chessColor)
        {
            for (var row = 0; row < fieldData.maxRows; row++)
            {
                if (fieldData.maxCols % 2 == 0)
                    chessColor = !chessColor;

                for (var col = 0; col < fieldData.maxCols; col++)
                {
                    CreateSquare(col, row, chessColor);
                    chessColor = !chessColor;
                }
            }
        }

        private void SetSquareTypes()
        {
            for (var row = 0; row < fieldData.maxRows; row++)
            {
                for (var col = 0; col < fieldData.maxCols; col++)
                {
                    var squareBlock = fieldData.levelSquares[row * fieldData.maxCols + col];
                    if (squareBlock.blocks.LastOrDefault().levelTargetType != LevelTargetTypes.EmptySquare &&
                        squareBlock.blocks.LastOrDefault().levelTargetType != LevelTargetTypes.NONE ||
                        squareBlock.block != LevelTargetTypes.EmptySquare && squareBlock.block != LevelTargetTypes.NONE)
                    {
                        GetSquare(col, row).SetType(squareBlock);
                    }
                }
            }
        }

        private void SetupSquarePositions()
        {
            var squarePositions = new List<PositionSquarePair>();

            foreach (var square in squaresArray)
            {
                var row = square.row;
                var col = square.col;
                if (square.subSquares.Count == 0)
                    continue;

                var squareSubSquare = square.subSquares[0];

                for (var sizeRow = 0; sizeRow < squareSubSquare.sizeInSquares.y; sizeRow++)
                {
                    for (var sizeCol = 0; sizeCol < squareSubSquare.sizeInSquares.x; sizeCol++)
                    {
                        var col1 = col + sizeCol;
                        var row1 = row + sizeRow;
                        if (row1 < fieldData.maxRows && col1 < fieldData.maxCols)
                        {
                            squarePositions.Add(new PositionSquarePair { Row = row1, Col = col1, Rectangle = square });
                        }
                    }
                }
            }

            foreach (var sq in squarePositions)
            {
                squaresArray[sq.Row * fieldData.maxCols + sq.Col].subSquares = sq.Rectangle.subSquares;
                squaresArray[sq.Row * fieldData.maxCols + sq.Col].mainRectangleInGrid = sq.Rectangle;
            }
        }

        private void ApplySquareSettings()
        {
            foreach (var i in squaresArray.ToList())
            {
                i.SetBorderDirection();
                i.SetTeleports();
                if (!i.IsNone())
                    i.SetOutline();
                i.SetSeparators();
            }

            foreach (var i in squaresArray.ToList())
                i.enterRectangle = GetEnterPoint(i);

            enterPoints = squaresArray.Count(i => i.isEnterPoint);

            foreach (var i in squaresArray.ToList())
            {
                i.SetDirection();
                i.SetMask();
            }

            foreach (var i in squaresArray.ToList())
            {
                i.sequenceBeforeThisSquare = i.GetSeqBeforeFromThis();
                if (i.sequence.All(x => !x.IsNone() && !x.undestroyable) && i.sequence.Any() && i.sequence.Any(x => x.isEnterPoint))
                    i.linkedEnterSquare = true;
            }
        }


        /// <summary>
        /// Set order for the squares sequience
        /// </summary>
        private void SetOrderInSequence()
        {
            var list = GetSquareSequence();
            foreach (var seq in list)
            {
                var order = 0;
                foreach (var sq in seq)
                {
                    sq.orderInSequence = order;
                    sq.sequence = seq;
                    order++;
                }
            }
        }


        private void CreateTestItems()
        {
            // if (GetSquare(5, 5) == null) return;
            GetSquare(3, 3).Item.colorableComponent.SetColor(0);
            GetSquare(3, 4).Item.colorableComponent.SetColor(0);
            GetSquare(4, 3).Item.colorableComponent.SetColor(0);
            GetSquare(4, 4).Item.colorableComponent.SetColor(0);
        }

        /// <summary>
        /// Creates Chopper on the field
        /// </summary>
        private void CreateChopper()
        {
            var items = GetItems(true);
            items = items.Where(i => !i.tutorialItem).ToList();
            items[Random.Range(0, items.Count())].SetType(ItemsTypes.Chopper, null);
            var itemsFiltered = items.Where(i => i.currentType != ItemsTypes.Chopper && !i.tutorialItem).ToArray();
            itemsFiltered[Random.Range(0, itemsFiltered.Count())].SetType(ItemsTypes.Chopper, null);
        }

        private void SetPosY(int y)
        {
            transform.position = new Vector2(transform.position.x, transform.position.y + (y - GetPosition().y));
        }

        public void SetPosition(Vector2 pos)
        {
            transform.position = new Vector2(transform.position.x + (pos.x - GetPosition().x), transform.position.y + (pos.y - GetPosition().y));
        }

        private void SetPivot()
        {
            // transform.position = GetCenter();
            pivot = new GameObject();
            pivot.name = "Pivot";
            pivot.transform.SetParent(transform);
            pivot.transform.position = GetCenter();
            // foreach (var square in squaresArray)
            // {
            //     square.transform.SetParent(transform);
            //     if (square.item != null)
            //         square.item.transform.SetParent(transform);
            // }
        }

        private Vector2 GetCenter()
        {
            var minX = squaresArray.Min(x => x.transform.position.x);
            var minY = squaresArray.Min(x => x.transform.position.y);
            var maxX = squaresArray.Max(x => x.transform.position.x);
            var maxY = squaresArray.Max(x => x.transform.position.y);
            var pivotPosMin = new Vector2(minX, minY);
            var pivotPosMax = new Vector2(maxX, maxY);
            var pivotPos = pivotPosMin + (pivotPosMax - pivotPosMin) * 0.5f;
            return pivotPos;
        }

        public Vector2 GetPosition()
        {
            return pivot?.transform.position ?? Vector2.zero;
        }

        public IEnumerator DeleteMatches()
        {
            // yield return new WaitForSeconds(0.5f);
            //        var combs = GetMatches();
            List<CombineClass> combs = new List<CombineClass>();
            List<CombineClass> allFoundCombines = new List<CombineClass>();
            List<CombineClass> bonusCombines = new List<CombineClass>();
            do
            {
                combs = MainManager.Instance.CombineManager.GetCombines(this, out allFoundCombines);
                ChangeFoundCombines(combs);
                ChangeFoundCombines(allFoundCombines);
                combs = MainManager.Instance.CombineManager.GetCombines(this, out allFoundCombines);
                bonusCombines.Clear();
                bonusCombines.Add(ArtificialIntelligence.Instance.GetChopperCombines());
                bonusCombines = bonusCombines.WhereNotNull().ToList();
                ChangeFoundCombines(bonusCombines);
                yield return new WaitForEndOfFrame();

//            yield return new WaitForEndOfFrame();
            } while (combs.Count > 0 || allFoundCombines.SelectMany(i => i.items).Any() || bonusCombines.Any());

            GetItems().ForEach(i => i.Hide(false));
            yield return new WaitingFallingDuration(false);
            MainManager.Instance.gameStatus = GameState.WaitForPopup;
        }

        public void ChangeFoundCombines(List<CombineClass> allFoundCombines)
        {
            ChangeFoundCombines(allFoundCombines.Select(i => i.items).ToList());
        }

        public void ChangeFoundCombines(List<List<Item>> allFoundCombines)
        {
            foreach (var comb in allFoundCombines)
            {
                if (comb == null) continue;
                var colorOffset = 0;
                foreach (var item in comb)
                {
                    if (item.tutorialItem) continue;
                    item.GetComponent<ColorReciever>().RandomizeColor(_colorGettable);
                    colorOffset++;
                }
            }
        }

        public void GenerateNewItems(bool falling = true)
        {
            var prepareLevel = !falling;
            var squares = squaresArray;
            if (fieldData.levelSquares.Any(i => i.item.ItemType == ItemsTypes.Gredient || LevelData.THIS.target.name.Contains("Ingredients") || !LevelData.THIS.SpawnerExits))
                IngredientsByEditor = true;
            foreach (var square in squares)
            {
                // Debug.Log("sallog GenerateNewItems  " + " square.CanGoInto()" + square.CanGoInto() + " square.IsHaveSolidAbove() >> " + square.IsHaveSolidAbove() + " prepareLevel " + prepareLevel + " pos col >> " + square.col + " pos row " + square.row + " square name >> " + square.name);

                if (square.IsNone() || !square.CanGoInto()) continue;
                if (!square.IsHaveSolidAbove() || prepareLevel)
                {
                    if (square.Item == null) //|| !falling && square.item?.currentType != ItemsTypes.SPIRAL)
                    {
                        var squareBlock = fieldData.levelSquares[square.row * fieldData.maxCols + square.col];
                        if (!falling && (squareBlock.item.Texture != null || squareBlock.item.ItemType != ItemsTypes.NONE))
                        {
                            var item = square.GenItem(false, squareBlock.item.ItemType, squareBlock.item.Color, squareBlock.item);
                            if (square.IsNone()) return;
                            item.tutorialItem = true; // not destroy this on regen
                            item.itemForEditor = squareBlock.item;
                            square.Item = item;
                        }

                        if (square.Item == null)
                            GenSimpleItem(falling, square);
                    }
                }
            }
        }

        public void RegenItems(bool falling = true)
        {
            var squares = squaresArray;
            foreach (var square in squares)
            {
                if (square.IsNone() || !square.CanGoInto()) continue;
                if (!square.IsHaveSolidAbove())
                {
                    if (square.Item != null && square.Item.currentType == ItemsTypes.NONE) //|| !falling && square.item?.currentType != ItemsTypes.SPIRAL)
                    {
                        square.Item.colorableComponent.RandomizeColor(_colorGettable);
                    }
                }
            }
        }

        private static void GenSimpleItem(bool falling, Rectangle rectangle)
        {
            rectangle.GenItem(falling);
        }

        public List<Item.WayMakerPoint> GetWaypoints(Rectangle startRectangle, Rectangle destRectangle, List<Rectangle> list = null)
        {
            if (destRectangle.Equals(startRectangle) || startRectangle == null)
                return list.Select(i => new Item.WayMakerPoint(i.transform.position, i)).ToList();
            var nextSquare = startRectangle.GetNextSquare();
            list = list ?? new List<Rectangle>();
            if (!list.Any()) list.Add(startRectangle);
            if (nextSquare != null && (nextSquare.IsFree() || nextSquare.CanGoInto()))
            {
                list.Add(nextSquare);
                GetWaypoints(nextSquare, destRectangle, list);
            }

            return list.Select(i => new Item.WayMakerPoint(i.transform.position, i)).ToList();
        }

        public Rectangle GetEnterPoint(Rectangle rectangle)
        {
            var enterSquare = rectangle.GetPreviousSquare();
            if (enterSquare == null) return rectangle;
            if (!enterSquare.isEnterPoint)
                enterSquare = GetEnterPoint(enterSquare);
            if (enterSquare.isEnterPoint && enterSquare.IsNone())
                enterSquare = GetEnterPoint(enterSquare);
            return enterSquare;
        }


        /// <summary>
        /// Get squares sequence from first to end
        /// </summary>
        /// <returns></returns>
        public List<List<Rectangle>> GetSquareSequence()
        {
            var enterSquares = squaresArray.Where(i => i.isEnterPoint);
            var listofSequences = ListofSequences(enterSquares);
            var l = listofSequences.SelectMany(i => i);
            var squaresNotJoinEnter = squaresArray.Where(i => !l.Contains(i));
            var topSquares = new List<Rectangle>();
            foreach (Rectangle square in squaresNotJoinEnter)
            {
                var sq = square;
                Rectangle prevRectangle;
                do
                {
                    prevRectangle = sq.GetPreviousSquare();
                    if (prevRectangle != null && prevRectangle.IsNone()) prevRectangle = null;
                    if (prevRectangle == null) topSquares.Add(sq);
                    sq = prevRectangle;
                } while (prevRectangle != null);
            }

            listofSequences.AddRange(ListofSequences(topSquares));
            return listofSequences;
        }

        /// <summary>
        /// Get list of all squares sequences
        /// </summary>
        /// <param name="enterSquares"></param>
        /// <returns></returns>
        private List<List<Rectangle>> ListofSequences(IEnumerable<Rectangle> enterSquares)
        {
            var listofSequences = new List<List<Rectangle>>();
            foreach (var enterSquare in enterSquares)
            {
                var sequence = new List<Rectangle>();
                sequence.Add(enterSquare);
                sequence = GetSquareSequenceStep(sequence);
                sequence.Reverse();
                listofSequences.Add(sequence);
            }

            return listofSequences;
        }

        /// <summary>
        /// Get square sequece
        /// </summary>
        /// <param name="sequence"></param>
        /// <returns></returns>
        private List<Rectangle> GetSquareSequenceStep(List<Rectangle> sequence)
        {
            var nextSquare = sequence.LastOrDefault().GetNextSquare();
            if (nextSquare != null && !nextSquare.IsNone())
            {
                sequence.Add(nextSquare);
                sequence = GetSquareSequenceStep(sequence);
            }

            return sequence;
        }

        /// <summary>
        /// Get sequence of the square
        /// </summary>
        /// <param name="rectangle"></param>
        /// <returns></returns>
        public List<Rectangle> GetCurrentSequence(Rectangle rectangle)
        {
            return GetSquareSequence().Where(i => i.Any(x => x == rectangle)).SelectMany(i => i).ToList();
        }

        /// <summary>
        /// Create a square
        /// </summary>
        /// <param name="col">column</param>
        /// <param name="row">row</param>
        /// <param name="chessColor">color switch</param>
        private void CreateSquare(int col, int row, bool chessColor = false)
        {
            GameObject squareObject = null;
            var squareBlock = fieldData.levelSquares[row * fieldData.maxCols + col];

            //Add_feature
            SingularSpawn Spawner = null;
            if (squareBlock.spawners.Count > 0)
                Spawner = squareBlock.spawners[0];


            squareObject = Instantiate(squarePrefab, firstSquarePosition + new Vector2(col * squareWidth, -row * squareHeight), Quaternion.identity);

            squareObject.transform.SetParent(GameField); //set parent later
            squareObject.transform.localPosition = firstSquarePosition + new Vector2(col * squareWidth, -row * squareHeight);
            var square = squareObject.GetComponent<Rectangle>();
            square.ColorGettable = _colorGettable;
            squaresArray[row * fieldData.maxCols + col] = square;
            square.field = this;
            square.row = row;
            square.col = col;
            square.type = LevelTargetTypes.EmptySquare;
            square.direction = squareBlock.direction;
            square.cantGenIn = squareBlock.cantGenInto;
            if (square.teleportOrigin == null)
            {
                square.isEnterPoint = squareBlock.enterSquare;

                //Added_feature
                if (Spawner != null)
                {
                    square.isSpawnerPoint = (Spawner.SpawnersType != Spawners.None) ? true : false;
                    square.SpawnerType = Spawner;
                }
                else
                {
                    square.isSpawnerPoint = false;
                    square.SpawnerType.SpawnersType = Spawners.None;
                    square.SpawnPersentage = 0;
                }
            }

            square.teleportDestinationCoord = squareBlock.teleportCoordinatesLinked;
            // square.AddComponent(Type.GetType(LevelData.target.ToString()));
            // if (squareBlock.blocks.LastOrDefault().squareType == SquareTypes.EmptySquare)
            // {
            //     square.SetType(squareBlock);
            // }
            if (squareBlock.blocks.Count > 0 && squareBlock.blocks.LastOrDefault().levelTargetType == LevelTargetTypes.NONE || squareBlock.block == LevelTargetTypes.NONE)
            {
                squareObject.GetComponent<SpriteRenderer>().enabled = false;
                square.type = LevelTargetTypes.NONE;
            }
            // else
            // {
            //     square.SetType(squareBlock);
            //
            // }
        }

        /// <summary>
        /// Get bottom row
        /// </summary>
        /// <returns>returns list of squares</returns>
        public List<Rectangle> GetBottomRow()
        {
            var itemsList = squaresArray.Where(i => i.bottomRow).ToList(); //GetSquareSequence().Select(i => i.FirstOrDefault()).Where(i => i.type != SquareTypes.NONE).ToList();
            return itemsList;
        }

        /// <summary>
        /// Get field rect
        /// </summary>
        /// <returns>rect</returns>
        public Rect GetFieldRect()
        {
            var square = GetSquare(0, 0);
            var squareRightBottom = GetSquare(fieldData.maxCols - 1, fieldData.maxRows - 1);
            return new Rect(square.transform.position.x, square.transform.position.y, squareRightBottom.transform.position.x - square.transform.position.x,
                square.transform.position.y - squareRightBottom.transform.position.y);
        }

        /// <summary>
        /// Get squares from a rect
        /// </summary>
        /// <param name="rect">rect</param>
        public List<Rectangle> GetFieldSeqment(RectInt rect)
        {
            List<Rectangle> squares = new List<Rectangle>();
            for (int row = rect.yMin; row <= rect.yMax; row++)
            {
                for (int col = rect.xMin; col <= rect.xMax; col++)
                {
                    squares.Add(GetSquare(col, row));
                }
            }

            return squares;
        }

        /// <summary>
        /// Get top row
        /// </summary>
        /// <returns>list of squares</returns>
        public List<Rectangle> GetTopRow()
        {
            return squaresArray.Where(i => !i.IsNone()).GroupBy(i => i.col).Select(i => new { Sq = i.OrderBy(x => x.row).First() }).Select(i => i.Sq).ToList();
        }

        public List<Rectangle> GetSimpleItemsInRow(int count)
        {
            var list = squaresArray
                .Where(i => i.GetSubSquare().IsFree())
                .Where(y => y.GetVerticalNeghbors().Count(z => z.IsFree()) == 2)
                .Select(x => new { Index = x.row, Value = x })
                .GroupBy(i => i.Index)
                .Select(i => i.Select(x => x.Value).Take(count).ToList())
                .ToArray();

            var v1 = list.GetValue(Random.Range(0, list.Length)) as List<Rectangle>;
            return v1;
        }

        public List<Rectangle> GetSquares(bool withUndestroyble = false)
        {
            var list = new List<Rectangle>();
            foreach (var item in squaresArray)
            {
                if (withUndestroyble && item.GetSubSquare().IsObstacle() && item.GetSubSquare().undestroyable)
                    list.Add(item);
                else
                    list.Add(item);
            }

            return list;
        }

        public Rectangle GetSquare(Vector2 vector)
        {
            return GetSquare(Mathf.RoundToInt(vector.x), Mathf.RoundToInt(vector.y));
        }

        public Rectangle GetSquare(int col, int row, bool safe = false)
        {
            if (!safe)
            {
                if (row >= fieldData.maxRows || col >= fieldData.maxCols || row < 0 || col < 0)
                    return null;
                return squaresArray[row * fieldData.maxCols + col];
            }

            row = Mathf.Clamp(row, 0, fieldData.maxRows - 1);
            col = Mathf.Clamp(col, 0, fieldData.maxCols - 1);
            return squaresArray[row * fieldData.maxCols + col];
        }

        /// <summary>
        /// Destroy items around the square.
        /// </summary>
        /// <param name="rectangle"></param>
        /// <param name="item1"></param>
        public void DestroyItemsAround(Rectangle rectangle, Item item1, bool isBombCombined = false, bool isCombinedWithDisco = false)
        {
            var itemsList = MainManager.Instance.GetItemsAroundSquare(rectangle, isBombCombined);
            /*foreach (var VARIABLE in itemsList)
            {
                Debug.LogError(" GetItemsAroundSquare " + VARIABLE.name + "   " + VARIABLE.square.type);
            }*/


            foreach (var item in itemsList.Where(i => i != null && (i.Combinable || i.Explodable)))
            {
                item.DestroyItem(true, true, item1, destroyNeighbours: false, WithoutShrink: true);
            }

            var squaresAround = MainManager.Instance.GetSquaresAroundSquare(rectangle, isBombCombined);
            /*foreach (var VARIABLE in squaresAround)
            {
                Debug.LogError(" DestroyItemsAround " + VARIABLE.type);
            }*/

            var processedBoxParents = new HashSet<Rectangle>(); // Track processed Box parents

            foreach (var detectedSquare in squaresAround)
            {
                /*if (detectedSquare.type == LevelTargetTypes.BreakableBox)
                    Debug.LogError(" Box Detected___________ " + (detectedSquare.boxParent != null) + "  " + (processedBoxParents.Add(detectedSquare.boxParent)));*/
                if ((detectedSquare.type is LevelTargetTypes.BreakableBox or LevelTargetTypes.HoneyBlock) ||
                    (detectedSquare.type is LevelTargetTypes.PlateCabinet or LevelTargetTypes.PotionCabinet
                         or LevelTargetTypes.Eggs or LevelTargetTypes.Pots
                     && detectedSquare.boxParent != null && processedBoxParents.Add(detectedSquare.boxParent)))
                {
                    //Debug.LogError(" detectedSquare " + detectedSquare.type + "  " + detectedSquare.name);
                    detectedSquare.DestroyBlock(destroyNeighbour: false);
                }
            }
        }


        /// <summary>
        /// Check if any items should be destroy
        /// </summary>
        /// <returns></returns>
        public bool DestroyingItemsExist()
        {
            return GetDestroyingItems()?.Length > 0;
        }

        /// <summary>
        /// Get destroying items
        /// </summary>
        /// <returns></returns>
        public ILongDestroyable[] GetDestroyingItems()
        {
            var longDestroyable = FindObjectsOfType<MonoBehaviour>().OfType<ILongDestroyable>().Where(i => !i.IsAnimationFinished() && i.CanBeStarted()).OrderBy(i => i.GetPriority()).ToArray();
            return longDestroyable;
        }

        public List<List<Item>> GetMatches(FindSeparating separating = FindSeparating.None, int matches = 3)
        {
            var newCombines = new List<List<Item>>();
            countedSquares = new Hashtable();
            countedSquares.Clear();
            for (var col = 0; col < fieldData.maxCols; col++)
            {
                for (var row = 0; row < fieldData.maxRows; row++)
                {
                    if (GetSquare(col, row) != null)
                    {
                        if (!countedSquares.ContainsValue(GetSquare(col, row).Item))
                        {
                            var newCombine = GetSquare(col, row).FindMatchesAround(separating, matches, countedSquares);
                            if (newCombine.Count >= matches)
                                newCombines.Add(newCombine);
                        }
                    }
                }
            }

            return newCombines;
        }

        /// <summary>
        /// Get random items for win animation and boosts
        /// </summary>
        /// <param name="count">count of items</param>
        /// <returns>list of items</returns>
        public List<Item> GetRandomItems(int count)
        {
            var list = GetItems(true);

            var list2 = new List<Item>();
            while (list2.Count < Mathf.Clamp(count, 0, GetItems(true).Count()))
            {
                // try
                // {
                if (!list.Any()) return list;
                var newItem = list[Random.Range(0, list.Count)];
                if (list2.IndexOf(newItem) < 0)
                {
                    list2.Add(newItem);
                }
                // }
                // catch (Exception ex)
                // {
                //     gameStatus = GameState.Win;//TODO: check win conditions
                // }
            }

            return list2;
        }

        /// <summary>
        /// Get items by parameters
        /// </summary>
        /// <param name="onlySimple">only simple items</param>
        /// <param name="exceptItems">except items</param>
        /// <param name="nonDestroying">only items currently shouldn't be destroy</param>
        /// <returns></returns>
        public List<Item> GetItems(bool onlySimple = false, Item[] exceptItems = null, bool nonDestroying = true, bool isExclusive = false)
        {
            exceptItems ??= Array.Empty<Item>();

            List<Item> items = new();

            // Get all items from squares
            for (int i = 0; i < squaresArray.Length; i++)
            {
                Rectangle rectangle = squaresArray[i];
                if (rectangle?.Item is not null && !exceptItems.Contains(rectangle.Item))
                    items.Add(rectangle.Item);
            }

            // Filter based on conditions
            List<Item> filteredItems = new();
            for (int i = 0; i < items.Count; i++)
            {
                Item item = items[i];
                bool shouldAdd = true;

                if (isExclusive && (item.currentType == ItemsTypes.NONE ||
                                    item.currentType == ItemsTypes.Eggs ||
                                    item.currentType == ItemsTypes.Pots))
                    shouldAdd = false;

                if (onlySimple && (item.currentType != ItemsTypes.NONE || item.NextType != ItemsTypes.NONE))
                    shouldAdd = false;

                if (nonDestroying && item.destroying)
                    shouldAdd = false;

                if (shouldAdd)
                    filteredItems.Add(item);
            }

            return filteredItems;
        }

        /// <summary>
        /// Get new created items
        /// </summary>
        /// <returns></returns>
        public List<Item> GetJustCreatedItems()
        {
            return FindObjectsOfType<Item>().Where(i => i.JustCreatedItem && i.needFall && !i.falling && !i.GetItemInterfaces()[0].IsStaticOnStart()).ToList();
        }

        /// <summary>
        /// Get items from bottom order
        /// </summary>
        /// <returns></returns>
        public List<Item> GetItemsFromBottomOrder()
        {
            // return GetItems(false, null, false).OrderByDescending(i => i.square.row).ToList();
            var list = GetSquareSequence();
            var items = new List<Item>();
            foreach (var seq in list)
            {
                var order = 0;
                foreach (var sq in seq)
                {
                    if (sq.Item != null)
                    {
                        sq.Item.orderInSequence = order;
                        items.Add(sq.Item);
                    }

                    order++;
                }
            }

            var excludedItems = GetItems().Except(items);
            foreach (var item in excludedItems)
            {
                items.Add(item);
            }

            return items.ToList();
        }

        /// <summary>
        /// Get target objects to check destination count
        /// </summary>
        /// <returns></returns>
        public TargetComponent[] GetTargetObjects()
        {
            var list = FindObjectsOfType(typeof(TargetComponent)) as TargetComponent[];
            list = list.Where(i => i.GetComponent<IField>().GetField() == this).Select(i => i.GetComponent<TargetComponent>()).ToArray();
            return list;
        }

        /// <summary>
        /// Get all bonus items like striped, package
        /// </summary>
        /// <returns></returns>
        public List<Item> GetAllExtaItems()
        {
            var list = new List<Item>();
            foreach (var square in squaresArray)
            {
                if (square.Item != null && (square.Item.currentType == ItemsTypes.Bomb || square.Item.currentType == ItemsTypes.DiscoBall ||
                                            square.Item.currentType == ItemsTypes.RocketVertical || square.Item.currentType == ItemsTypes.RocketHorizontal ||
                                            square.Item.currentType == ItemsTypes.Chopper) /*&& square.Item.Combinable*/)
                {
                    list.Add(square.Item);
                }
            }

            return list;
        }

        /// <summary>
        /// Get squares of particular type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public int CountSquaresByType(string type)
        {
            var squareType = (LevelTargetTypes)Enum.Parse(typeof(LevelTargetTypes), type.Replace("Internal.Scripts.TargetScripts.", ""));

            return /*squaresArray.Count(item => item.type == squareType) + */squaresArray.WhereNotNull().SelectMany(i => i.subSquares).Count(item => item.type == squareType);
        }

        /// <summary>
        /// Get squares without items
        /// </summary>
        /// <returns></returns>
        public List<Rectangle> GetEmptySquares()
        {
            var emptySquares = new List<Rectangle>(); // Use a List instead of an array

            for (int i = 0; i < squaresArray.Length; i++)
            {
                var square = squaresArray[i];
                if (square.IsFree() && square.Item == null && !square.IsHaveSolidAbove() && square.linkedEnterSquare && square.enterRectangle.isEnterPoint)
                {
                    emptySquares.Add(square); // Add squares directly instead of using LINQ
                }
            }

            return emptySquares;
        }


        /// <summary>
        /// Get top square in a column
        /// </summary>
        /// <param name="col">column</param>
        /// <returns></returns>
        public Rectangle GetTopSquareInCol(int col)
        {
            return squaresArray.Where(i => i.IsFree() && i.col == col).OrderBy(i => i.row).FirstOrDefault();
        }

        public Dictionary<int, int> CalculateLayersPerRow()
        {
            Dictionary<int, int> layersPerRow = new Dictionary<int, int>();

            for (int row = 0; row < fieldData.maxRows; row++)
            {
                int layerCount = 0;

                for (int col = 0; col < fieldData.maxCols; col++)
                {
                    Rectangle rectangle = GetSquare(col, row);
                    if (rectangle != null)
                    {
                        layerCount += rectangle.GetLayersCount();
                    }
                }

                layersPerRow[row] = layerCount;
            }

            return layersPerRow;
        }

        public Dictionary<int, int> CalculateLayersPerCol()
        {
            Dictionary<int, int> layersPerCol = new Dictionary<int, int>();

            for (int col = 0; col < fieldData.maxCols; col++)
            {
                int layerCount = 0;

                for (int row = 0; row < fieldData.maxRows; row++)
                {
                    Rectangle rectangle = GetSquare(col, row);
                    if (rectangle != null)
                    {
                        layerCount += rectangle.GetLayersCount();
                    }
                }

                layersPerCol[col] = layerCount;
            }

            return layersPerCol;
        }


        public int GetRowWithMostLayers()
        {
            var layersPerRow = CalculateLayersPerRow();
            return layersPerRow.OrderByDescending(x => x.Value).FirstOrDefault().Key;
        }

        public IEnumerable<Rectangle> IGetRowWithMostLayers()
        {
            int maxLayers = 0;
            IEnumerable<Rectangle> rowWithMostLayers = null;

            for (int row = 0; row < squaresArray.GetLength(0); row++)
            {
                int currentRowLayers = 0;

                for (int col = 0; col < squaresArray.GetLength(1); col++)
                {
                    // Assuming GetLayerCount() returns the number of layers in the Square
                    currentRowLayers += GetSquare(row, col).GetLayersCount();
                }

                if (currentRowLayers > maxLayers)
                {
                    maxLayers = currentRowLayers;
                    rowWithMostLayers = squaresArray.Cast<Rectangle>().Where(s => s.row == row);
                }
            }

            return rowWithMostLayers;
        }

        public int GetColWithMostLayers()
        {
            var layersPerCol = CalculateLayersPerCol();
            return layersPerCol.OrderByDescending(x => x.Value).FirstOrDefault().Key;
        }

        // Your existing methods here...

        public (int row, int col) GetCenterOfAreaWithMostTargets(int areaSize)
        {
            int maxTargets = 0;
            (int row, int col) center = (-1, -1);

            for (int row = 0; row <= fieldData.maxRows - areaSize; row++)
            {
                for (int col = 0; col <= fieldData.maxCols - areaSize; col++)
                {
                    int targetsInArea = CalculateTargetsInArea(row, col, areaSize);
                    if (targetsInArea > maxTargets)
                    {
                        maxTargets = targetsInArea;
                        center = GetCenterCoordinates(row, col, areaSize);
                    }
                }
            }

            return center;
        }

        private (int row, int col) GetCenterCoordinates(int startRow, int startCol, int areaSize)
        {
            int centerRow = startRow + areaSize / 2;
            int centerCol = startCol + areaSize / 2;
            return (centerRow, centerCol);
        }

        private int CalculateTargetsInArea(int startRow, int startCol, int areaSize)
        {
            int targetsCount = 0;

            for (int row = startRow; row < startRow + areaSize; row++)
            {
                for (int col = startCol; col < startCol + areaSize; col++)
                {
                    Rectangle rectangle = GetSquare(col, row);
                    if (rectangle != null)
                    {
                        targetsCount += rectangle.GetLayersCount();
                    }
                }
            }

            return targetsCount;
        }

        /// <summary>
        /// Get items by color
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public Item[] GetItemsByColor(int color)
        {
            Debug.LogError($"FieldBoard: ItemsCount >> {GetItems()?.Count}");
            Debug.LogError($"FieldBoard: ItemsByColor >> {GetItems()?.Where(i => i.color == color).Count()}");
            return GetItems()?.Where(i => i.color == color && i.currentType == ItemsTypes.NONE && !i.JustIntItem).ToArray();
        }


        /// <summary>
        /// Get items without a neighbour
        /// </summary>
        /// <returns></returns>
        public Item[] GetLonelyItemsOrCage()
        {
            return squaresArray.Where(i => i.Item != null).Where(i => !i.GetAllNeghborsCross().Any() || !i.CanGoOut()).Select(i => i.Item).ToArray();
        }

        public FieldBoard DeepCopy()
        {
            var other = (FieldBoard)MemberwiseClone();
            other.squaresArray = new Rectangle[fieldData.maxCols * fieldData.maxRows];
            for (var i = 0; i < squaresArray.Count(); i++)
            {
                var square = squaresArray[i];
                other.squaresArray[i] = square.DeepCopy();
            }

            return other;
        }
    }

    public struct PositionSquarePair
    {
        public int Row { get; set; }
        public int Col { get; set; }
        public Rectangle Rectangle { get; set; }
    }
}


using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Internal.Scripts.Blocks;
using Internal.Scripts.Items;
using Internal.Scripts.Items.Interfaces;
using Internal.Scripts.System;
using Internal.Scripts.System.Combiner;
using Internal.Scripts.System.Pool;


namespace Internal.Scripts
{
    public enum CombineType
    {
        LShape,
        VShape
    }

    /// <summary>
    /// Searches tips and automatic player for debugger
    /// </summary>
    public class ArtificialIntelligence : MonoBehaviour
    {
        /// <summary>
        /// The reference to this object
        /// </summary>
        public static ArtificialIntelligence Instance;

        private DebugSettings _debugSettings;

        /// <summary>
        /// have got a tip
        /// </summary>
        public bool gotTip;

        /// <summary>
        /// The allow show tip
        /// </summary>
        public bool allowShowTip;

        /// <summary>
        /// The tip identifier
        /// </summary>
        int tipID;

        /// <summary>
        /// The count of coroutines
        /// </summary>
        public int corCount;

        /// <summary>
        /// The tip items
        /// </summary>
        public Vector2 DirectionToMove;

        private List<Item> currentPreCombine = new List<Item>();
        // Use this for initialization

        private DG.Tweening.Sequence activeTipSequence;


        private void Awake()
        {
            Instance = this;
            squareBoundaryLine = FindAnyObjectByType<SquareBoundaryLine>();
            //activeTipSequence= DOTween.Sequence();
            _debugSettings = Resources.Load("Scriptable/DebugSettings") as DebugSettings;
            InitSprites();
        }

        private void InitSprites()
        {
            if (ObjectPoolManager.Instance != null) itemSprites = ObjectPoolManager.Instance.GetPooledObject("Item", this, false).GetComponent<ColorReciever>();
        }

        public Vector2 vDirection;
        public CombineType currentCombineType;
        private Item tipItem;
        private bool changeTipAI;
        private ColorReciever itemSprites;
        private int maxRow;
        private int maxCol;

        public Item TipItem
        {
            get { return tipItem; }

            set { tipItem = value; }
        }

        /// <summary>
        /// Gets the square. Return square by row and column
        /// </summary>
        /// <param name="row">The row.</param>
        /// <param name="col">The column.</param>
        /// <param name="currentRectangle"></param>
        /// <returns></returns>
        Rectangle GetSquare(int row, int col, Rectangle currentRectangle)
        {
            var v1 = currentRectangle.GetPosition() - new Vector2(col, row);
            if (v1.magnitude > 1) currentRectangle = MainManager.Instance.field.GetSquare(currentRectangle.GetPosition() + new Vector2(v1.x, v1.y * -1).normalized);
            if (currentRectangle != null && currentRectangle.IsDirectionRestricted(v1.normalized)) return null;
            var newSquare = MainManager.Instance.GetSquare(col, row);
            if (newSquare != null && newSquare.IsDirectionRestricted(v1.normalized)) return null;
            return newSquare;
        }

        /// <summary>
        /// Checks the square. Is the color of item of this square is equal to desired color. If so we add the item to nextMoveItems array.
        /// </summary>
        /// <param name="rectangle">The square.</param>
        /// <param name="COLOR">The color.</param>
        /// <param name="moveThis">is the item should be movable?</param>
        bool CheckSquare(Rectangle rectangle, int COLOR, bool moveThis = false)
        {
            if (rectangle == null)
                return false;
            if (currentPreCombine == null) currentPreCombine = new List<Item>();
            if (rectangle.Item != null)
            {
                if (CheckColorCondition(rectangle, COLOR))
                {
                    if (moveThis && rectangle.GetSubSquare().CanGoOut())
                    {
                        currentPreCombine.Add(rectangle.Item);

                        return true;
                    }
                    else if (!moveThis)
                    {
                        currentPreCombine.Add(rectangle.Item);
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool CheckColorCondition(Rectangle rectangle, int COLOR)
        {
            if (MainManager.Instance.gameStatus != GameState.Tutorial)
            {
                if (COLOR <= MainManager.Instance.levelData.colorLimit)
                    return rectangle.Item.color == COLOR && rectangle.Item.Combinable;
                else
                    return rectangle.item.color > MainManager.Instance.levelData.colorLimit && rectangle.Item.Combinable;
            }
            else
                return rectangle.Item.color == COLOR && rectangle.Item.Combinable && rectangle.Item.tutorialItem;
        }


        public List<Item> GetCombine()
        {
            return currentPreCombine;
        }


        /// <summary>
        /// Extend your existing enum to distinguish between new match patterns:
        /// </summary>
        public enum MatchPattern
        {
            HeliCopter,
            Multicolor,
            HorizontalLeft,
            HorizontalRight,
            VerticalBottom,
            LShapeTopLeft,
            LShapeTopRight,
            LShapeBottomLeft,
            LShapeBottomRight,
            Horizontal4,
            Horizontal5,
            Vertical4,
            Horizontal4FromBelowRight,
            Horizontal4FromAboveRight,

            Vertical4FromRightUp,

            Vertical4FromLeftUp,
            Horizontal4FromAboveLeft,
            Horizontal3FromUpCenter,
            Horizontal3FromDownCenter,
            Vertical3FromUp,
            Vertical3FromDown,
            Vertical3FromLeftCenter,
            Vertical3FromRightCenter,
            Vertical3FromLeftDown,
            Vertical3FromRightDown,
            Vertical3FromLeftUp,
            Vertical3FromRightUp,
            TShapeUp,
            TShapeDown,
            TShapeLeft,
            TShapeRight,
            None,
            Exclusives,
            // If you’d like explicit L-Shape 5 or other shapes, add them here
        }

        /// <summary>
        /// Holds the offsets for matching and the direction to move the 'key' item.
        /// The 'type' can be used for further logic or logging.
        /// </summary>
        public sealed class PatternDefinition
        {
            public MatchPattern Type { get; }
            public IReadOnlyList<Vector2> Offsets { get; }
            public Vector2 MoveDirection { get; }
            public Vector2 MovableItemOffset { get; }
            public int Priority { get; set; }

            public bool IsValid()
            {
                return Offsets != null && Offsets.Count > 0 && MoveDirection != Vector2.zero;
            }

            private PatternDefinition(
                MatchPattern type,
                Vector2[] offsets,
                Vector2 moveDirection,
                Vector2 movableItemOffset,
                int priority)
            {
                if (offsets == null || offsets.Length == 0)
                    throw new ArgumentException("Offsets cannot be null or empty");

                if (!offsets.Contains(movableItemOffset))
                    throw new ArgumentException("Movable item offset must be part of pattern");

                Type = type;
                Offsets = offsets.ToList().AsReadOnly();
                MoveDirection = moveDirection;
                MovableItemOffset = movableItemOffset;
                Priority = priority;
            }

            public static PatternDefinition Create(
                MatchPattern type,
                Vector2[] offsets,
                Vector2 moveDirection,
                Vector2 movableItemOffset,
                int score)
            {
                return new PatternDefinition(type, offsets, moveDirection, movableItemOffset, score);
            }
        }


        private List<List<Rectangle>> foundMatches = new List<List<Rectangle>>();
        private List<PatternDefinition> currentPatterns;
        private List<PatternDefinition> basicCurrentPatterns;


        private float tipCooldown = 5.0f;
        private float lastTipTime = 3f;
        private float tipInterval = 7f;
        private List<Rectangle> lastTipItems = new List<Rectangle>();

        bool isActiveIt;

        public IEnumerator CheckPossibleCombines()
        {
            if (isActiveIt)
                yield break;
            isActiveIt = true;
            // Step 1: Clear board state and ensure readiness
            yield return ClearAndCheckBoardReadiness();

            // Step 2: Ensure patterns are initialized
            EnsurePatternsInitialized();
            //Debug.Log($"[AI] CheckPossibleCombines Starting match search with {currentPatterns.Count} patterns");

            // Step 3: Search for matches
            bool foundAnyMatches = SearchAllMatches();

            // Step 4: Handle match results
            HandleMatchResults(foundAnyMatches);

            //yield return new WaitForEndOfFrame();
        }

        public IEnumerator ClearAndCheckBoardReadiness()
        {
            ClearFoundMatches();
            StopCoroutine(ShowTipsPeriodically());
            yield return InitializeCheck();

            if (!CanCheckForMatches())
            {
                //Debug.Log("[AI] CheckPossibleCombines Cannot check for matches - game state not ready");
                yield break;
            }
        }

        private void EnsurePatternsInitialized()
        {
            if (currentPatterns == null || currentPatterns.Count == 0)
            {
                currentPatterns = InitializePatterns();
                basicCurrentPatterns = InitBasic();
                exclusivePatterns = InitExclusives();
            }
        }

        private void CheckExclusivePatterns(Item color, List<PatternDefinition> exclusivePatterns)
        {
            // throw new NotImplementedException();
            foreach (var item in exclusivePatterns)
            {
                List<Rectangle> squares = new();
                int targetRow = (int)(color.square.row + item.Offsets[0].y);
                int targetCol = (int)(color.square.col + item.Offsets[0].x);
                var targetSquare = GetSquare(targetRow, targetCol, color.square);
                if (!IsValidSquare(targetSquare))
                    continue;
                if (targetSquare.item.currentType != ItemsTypes.NONE && targetSquare.item.currentType != ItemsTypes.Eggs && targetSquare.item.currentType != ItemsTypes.Pots)
                {
                    squares.Add(targetSquare);
                    ProcessMatchedSquaresAndLog(color.square, 10, item, squares);
                    HandleFoundPattern(color.square, color.square.row, color.square.col, 10, item, exclusivePatterns);
                }
                // HandleFoundPattern(color.square,color.square.row,color.square.col,10,item,exclusivePatterns);
            }
            // var square = LevelManager.THIS.GetSquare(col, row);
            // if (!IsValidSquare(square)) return;
            // //patterns.Sort((a,b) => b.Priority.CompareTo(a.Priority));
            // foreach (var pattern in patterns)
            // {
            //     ////print(pattern.Type + "TYPEAmir");
            //     if (CheckPattern(square, color, pattern))
            //     {
            //         HandleFoundPattern(square, row, col, color, pattern, patterns);
            //     }
            // }
        }

        private List<PatternDefinition> exclusivePatterns;

        private bool SearchAllMatches()
        {
            bool foundAnyMatches = false;
            bool isAnythingBesides3found = false;
            var validExclusives = MainManager.Instance.field.GetItems(isExclusive: true);

            foreach (var color in validExclusives)
            {
                if (color.currentType != ItemsTypes.Eggs && color.currentType != ItemsTypes.Pots)
                {
                    CheckExclusivePatterns(color, exclusivePatterns);
                }
                // CheckPatterns(10, exclusivePatterns);
            }

            if (choose.Count != 0)
            {
                foundAnyMatches = true;
                Debug.LogError("[AI] CheckPossibleCombines Found exclusive items on board" + validExclusives.Count);
            }

            var validColors = GetAvailableColors();
            foreach (var color in validColors)
            {
                CheckPatterns(color, currentPatterns);
                if (choose.Count > 0)
                {
                    isAnythingBesides3found = true;
                    foundAnyMatches = true;
                    //Debug.Log($"[AI] CheckPossibleCombines Found {foundMatches.Count} matches for color {color}");
                }
            }

            if (!isAnythingBesides3found)
            {
                foreach (var color in validColors)
                {
                    CheckPatterns(color, basicCurrentPatterns);
                    if (choose.Count > 0)
                    {
                        foundAnyMatches = true;
                        //Debug.Log($"[AI] CheckPossibleCombines Found {foundMatches.Count} matches for color {color}");
                    }
                }
            }

            return foundAnyMatches;
        }

        public Coroutine _showTipPeriodicly;

        private void HandleMatchResults(bool foundAnyMatches)
        {
            if (foundAnyMatches)
            {
                //Debug.Log($"[AI] CheckPossibleCombines Found total of {foundMatches.Count} possible matches");
                _showTipPeriodicly = StartCoroutine(ShowTipsPeriodically());
                gotTip = true;
            }
            else
            {
                //Debug.Log("[AI] CheckPossibleCombines No matches found on board");
                HandleNoMatches();
            }
        }

        /// <summary>
        /// Returns distinct, valid item colors on the board.
        /// </summary>
        private IEnumerable<int> GetAvailableColors()
        {
            return MainManager.Instance.field.GetItems()
                .Where(item => item != null
                               && !item.destroying
                               && !item.falling
                               && item.color != -1)
                .Select(item => item.color)
                .Distinct()
                .OrderBy(c => c);
        }

        /// <summary>
        /// Checks all patterns for a given color in every square on the board.
        /// </summary>
        private void CheckPatterns(int color, List<PatternDefinition> patterns)
        {
            for (var col = 0; col < maxCol; col++)
            {
                for (var row = 0; row < maxRow; row++)
                {
                    ProcessSquareForPatterns(col, row, color, patterns);
                }
            }
        }

        private void ProcessSquareForPatterns(int col, int row, int color, List<PatternDefinition> patterns)
        {
            var square = MainManager.Instance.GetSquare(col, row);
            if (!IsValidSquare(square)) return;
            //patterns.Sort((a,b) => b.Priority.CompareTo(a.Priority));
            foreach (var pattern in patterns)
            {
                ////print(pattern.Type + "TYPEAmir");
                if (CheckPattern(square, color, pattern))
                {
                    HandleFoundPattern(square, row, col, color, pattern, patterns);
                }
            }
        }

        public class ChoosedPattern
        {
            public PatternDefinition choose;
            public List<Rectangle> squares;
            public int score;

            public ChoosedPattern(PatternDefinition choose, List<Rectangle> squares, int score)
            {
                this.choose = choose;
                this.squares = squares;
                this.score = score;
            }
        }

        private List<ChoosedPattern> choose = new();
        private List<PatternDefinition> choosenPattern = new();

        private void HandleFoundPattern(Rectangle pivotRectangle, int row, int col, int color, PatternDefinition pattern, List<PatternDefinition> patterns)
        {
            //LogFoundPattern(pivotSquare, row, col, color, pattern, patterns);


            var squaresInMatch = currentPreCombine.Select(item => item.square).ToList();
            //currentPreCombine.Add(pivotSquare.item);
            //////print($"Amirhossein{pattern.MoveDirection}{pattern.Type}");
            //DirectionToMove = pattern.MoveDirection;
            //if (!IsDuplicatePattern(squaresInMatch))
            //{
            //squaresInMatch.ForEach((x) => print($"AmirHosseinSquares{x.item.instanceID}"));
            var tmp = new ChoosedPattern(pattern, squaresInMatch, pattern.Priority);
            var lasitem = tmp.squares.Last();
            var strayPiece = GetSquareByOffset(lasitem, tmp.choose.MoveDirection);
            tmp.squares.Remove(lasitem);
            tmp.squares.Add(strayPiece);
            tmp.squares.Add(lasitem);
            choose.Add(tmp);
            choosenPattern.Add(pattern);
            foundMatches.Add(squaresInMatch);
            //Debug.Log($"Added new unique match pattern {pattern.Type} to found matches. Total found matches: {foundMatches.Count}");
            //}
            //else
            //{
            //    //Debug.Log($"Pattern {pattern.Type} already found - skipping duplicate");
            //}

            currentPreCombine.Clear();
        }


        // Placeholder for duplicate-check. Implement your own duplicate-detection logic.
        private bool IsDuplicatePattern(List<Rectangle> squaresInMatch)
        {
            // Example: Always return false to add all matches.
            // Replace with: foundMatches.Any(m => AreTipsSame(m, squaresInMatch));
            return false;
        }

        /// <summary>
        /// Checks whether a square can be used for matching logic.
        /// Modify to reflect your exact game constraints.
        /// </
        // sssssssssssssssssssssssssssswsummary>
        private bool IsValidSquare(Rectangle rectangle)
        {
            if (rectangle == null) return false;

            // Example boundary checks
            if (rectangle.row < 0 || rectangle.row >= maxRow ||
                rectangle.col < 0 || rectangle.col >= maxCol)
                return false;

            // Example checks for whether a square can contain a matching item
            if (!rectangle.CanGoInto() || rectangle.IsNone())
                return false;

            if (rectangle.Item == null || rectangle.Item.destroying ||
                rectangle.Item.falling || rectangle.Item.color == -1)
                return false;

            return true;
        }

        /// <summary>
        /// Core logic to verify if the given square + offsets form a valid pattern.
        /// This also sets up the 'TipItem' (the item to move) and logs a debug message.
        /// </summary>
        private bool CheckPattern(Rectangle pivotRectangle, int color, PatternDefinition pattern)
        {
            if (!IsValidSquare(pivotRectangle))
                return false;

            //Debug.Log($"[AI.CheckPattern] Checking pattern {pattern.Type} at square ({pivotSquare.col}, {pivotSquare.row}) for color {color}");

            if (!TryGetMatchedSquares(pivotRectangle, color, pattern, out List<Rectangle> matchedSquares))
                return false;

            ProcessMatchedSquaresAndLog(pivotRectangle, color, pattern, matchedSquares);
            return true;
        }

        /// <summary>
        /// Iterates through each offset in the pattern and validates that the target square satisfies conditions.
        /// </summary>
        private bool TryGetMatchedSquares(Rectangle pivotRectangle, int color, PatternDefinition pattern, out List<Rectangle> matchedSquares)
        {
            matchedSquares = new List<Rectangle> { pivotRectangle };

            foreach (var offset in pattern.Offsets)
            {
                int targetRow = (int)(pivotRectangle.row + offset.y);
                int targetCol = (int)(pivotRectangle.col + offset.x);
                var targetSquare = GetSquare(targetRow, targetCol, pivotRectangle);

                if (CheckSquare(targetSquare, color, false))
                {
                    matchedSquares.Add(targetSquare);
                }
                else
                    return false;
            }

            // Ensure the count matches the number of offsets plus the pivot.
            if (matchedSquares.Count != pattern.Offsets.Count + 1 || matchedSquares.Any(sq => sq == null))
                return false;

            return true;
        }

        /// <summary>
        /// Processes the matched squares by logging details, adding the movable part, and updating state.
        /// </summary>
        private void ProcessMatchedSquaresAndLog(Rectangle pivotRectangle, int color, PatternDefinition pattern, List<Rectangle> matchedSquares)
        {
            var movableSquare = GetAndAddMovableSquare(pivotRectangle, pattern, matchedSquares);
            //LogPatternMatch(pivotSquare, color, pattern, matchedSquares, movableSquare);
            UpdateMatchState(pivotRectangle, pattern, matchedSquares, movableSquare);
        }

        private Rectangle GetAndAddMovableSquare(Rectangle pivotRectangle, PatternDefinition pattern, List<Rectangle> matchedSquares)
        {
            var movableSquare = GetSquareByOffset(pivotRectangle, pattern.MovableItemOffset);
            //matchedSquares.Add(movableSquare);
            return movableSquare;
        }

        private void LogPatternMatch(Rectangle pivotRectangle, int color, PatternDefinition pattern, List<Rectangle> matchedSquares, Rectangle movableRectangle)
        {
#if UNITY_EDITOR
            // var squaresInfo = string.Join(", ", matchedSquares.Select(sq => $"({sq.col},{sq.row})"));
            // if (movableSquare != null)
            // {
            //     // //Debug.Log(
            //     //     $"[{pattern.Type}] Found pattern for color={color}. Move item at ({movableSquare.col},{movableSquare.row}) " +
            //     //     $"(ID: {movableSquare.Item.GetInstanceID()}) to form pattern affecting squares [{squaresInfo}]. " +
            //     //     $"Pattern movable offset: [{pattern.MovableItemOffset}] and direction: [{pattern.MoveDirection}]"
            //     // );
            // }
            // else
            // {
            //     //Debug.Log(
            //         $"[{pattern.Type}] Found pattern for color={color}. No movable square assigned. " +
            //         $"Pattern squares: [{squaresInfo}]."
            //     );
            // }
#endif
        }

        private void UpdateMatchState(Rectangle pivotRectangle, PatternDefinition pattern, List<Rectangle> matchedSquares, Rectangle movableRectangle)
        {
            currentPreCombine = matchedSquares
                .Where(sq => sq != pivotRectangle)
                .Select(sq => sq.Item)
                .ToList();

            vDirection = pattern.MoveDirection;
            TipItem = movableRectangle?.Item;
            currentCombineType = (CombineType)pattern.Type;
        }

        public Rectangle GetSquareByOffset(Rectangle rectangle, Vector2 offset)
        {
            return GetSquare((int)(rectangle.row + offset.y), (int)(rectangle.col + offset.x), rectangle);
        }


        /// <summary>
        /// Periodically shows a hint (tip) if we have any found matches.
        /// Adjust the logic to your preference and loops through all found matches.
        /// </summary>
        /// p
        ///
        public void StopCoroutineShowTipsPeriodically()
        {
            // choose.Clear();
            ClearFoundMatches();
            if (_showTipPeriodicly != null)
                StopCoroutine(_showTipPeriodicly);
        }

        private IEnumerator ShowTipsPeriodically()
        {
            int i = 0;
            choose.Sort((a, b) => b.score.CompareTo(a.score));

            while (choose.Count > 0)
            {
                // Log all found matches
                //for (int i = 0; i < choose.Count; i++)
                //{
                //var match = foundMatches[i];
                ////Debug.Log($"[AI] ShowTipsPeriodically Match {i + 1}/{foundMatches.Count}: {match.Count} items at positions: " +
                //    string.Join(", ", match.Select(s => $"({s.col},{s.row})")));
                //int matchIndex = (int)((Time.time / tipInterval) % foundMatches.Count);
                var selectedMatch = choose[i].squares;
                DirectionToMove = choose[i].choose.MoveDirection;
                //print($"AmirHosseinDir{DirectionToMove}");
                //print($"AmirHosseinScore{choose[i].score}");
                //print($"AmirHosseinID{lastItem.item.instanceID}");

                //print($"AmirHosseinIDstrayPiece{strayPiece.item.instanceID}");
                //print($"AmirHosseinType{choose[i].choose.Type}");
                if (DirectionToMove == Vector2.up || DirectionToMove == Vector2.down)
                {
                    DirectionToMove = DirectionToMove * -1;
                }


                //Debug.Log($"[AI] ShowTipsPeriodically Showing tip for match {i + 1}: " +
                //  string.Join(", ", selectedMatch.Select(s => $"({s.col},{s.row})")));
                //selectedMatch.Add(strayPiece);
                showTip(selectedMatch.Select(square => square.Item).ToList(), DirectionToMove, choose[i].choose.Type);

                lastTipTime = Time.time;
                gotTip = true;
                yield return new WaitForSeconds(tipInterval);
                i++;
                if (i == choose.Count)
                    i = 0;
                //}
                //AmirHossein Niaz Be tamiz Kari darad: Attention
                // Rotate through matches instead of just showing first one
                //int matchIndex = (int)((Time.time / tipInterval) % foundMatches.Count);
                //var selectedMatch = foundMatches[matchIndex];
                //DirectionToMove = choosenPattern[matchIndex].MoveDirection;
                ////print($"AmirHosseinDir{DirectionToMove}");
                //var lastItem = selectedMatch.Last();
                ////print($"AmirHosseinID{lastItem.item.instanceID}");

                //var strayPiece = GetSquareByOffset(lastItem, DirectionToMove);
                ////print($"AmirHosseinIDstrayPiece{strayPiece.item.instanceID}");
                ////print($"AmirHosseinType{choosenPattern[matchIndex].Type}");

                //selectedMatch.Remove(lastItem);
                //selectedMatch.Remove(lastItem);
                //selectedMatch.Add(strayPiece);
                //selectedMatch.Add(lastItem);

                ////Debug.Log($"[AI] ShowTipsPeriodically Showing tip for match {matchIndex + 1}: " +
                //    string.Join(", ", selectedMatch.Select(s => $"({s.col},{s.row})")));
                ////selectedMatch.Add(strayPiece);
                //showTip(selectedMatch.Select(square => square.Item).ToList());

                //lastTipTime = Time.time;
                //gotTip = true;


                //yield return new WaitForSeconds(0.5f);
            }
        }


        /// <summary>
        /// Overload for items, if needed.
        /// </summary>
        private bool AreTipsSame(List<Item> tip1, List<Item> tip2)
        {
            if (tip1.Count != tip2.Count) return false;
            for (int i = 0; i < tip1.Count; i++)
            {
                if (tip1[i] != tip2[i]) return false;
            }

            return true;
        }

        /// <summary>
        /// Called when no matches have been found.
        /// Typically handles board regeneration or other fallback logic.
        /// </summary>
        private void HandleNoMatches()
        {
            //
            // if (gotTip || LevelManager.THIS.gameStatus != GameState.Playing) return;
            // currentPreCombine.Clear();
            ClearFoundMatches();
            TipItem = null;
            MainManager.Instance.ReGenLevel
                ();
            // e.g.: LevelManager.THIS.RegenerateField(); // your logic
        }

        /// <summary>
        /// Clears the list of found matches.
        /// Useful if you want to forcibly reset match detection.
        /// </summary>
        public void ClearFoundMatches()
        {
            foundMatches.Clear();
            choosenPattern.Clear();
            choose.Clear();
        }

        private List<PatternDefinition> InitExclusives()
        {
            var exclusivePatterns = new[]
            {
                PatternDefinition.Create(MatchPattern.Exclusives, new[] { new Vector2(1, 0) }, Vector2.left, new Vector2(1, 0), 1000),
                PatternDefinition.Create(MatchPattern.Exclusives, new[] { new Vector2(-1, 0) }, Vector2.right, new Vector2(-1, 0), 1005),
                PatternDefinition.Create(MatchPattern.Exclusives, new[] { new Vector2(0, 1) }, Vector2.down, new Vector2(0, 1), 1010),
                PatternDefinition.Create(MatchPattern.Exclusives, new[] { new Vector2(0, -1) }, Vector2.up, new Vector2(0, -1), 1020),
            };
            return exclusivePatterns.ToList();
        }

        private List<PatternDefinition> InitBasic()
        {
            var basicPatterns = new[]
            {
                // Horizontal 3-match: Move item from right to left
                PatternDefinition.Create(
                    MatchPattern.HorizontalLeft,
                    new[] { new Vector2(-1, 0), new Vector2(-2, 0), new Vector2(1, 0) },
                    Vector2.left,
                    new Vector2(1, 0),
                    10 // This item needs to move left to create the match
                ),
                PatternDefinition.Create(
                    MatchPattern.HorizontalLeft,
                    new[] { new Vector2(-1, 0), new Vector2(-2, 0), new Vector2(0, 1) },
                    Vector2.down,
                    new Vector2(0, 1),
                    12 // This item needs to move left to create the match
                ),
                PatternDefinition.Create(
                    MatchPattern.HorizontalLeft,
                    new[] { new Vector2(-1, 0), new Vector2(-2, 0), new Vector2(0, -1) },
                    Vector2.up,
                    new Vector2(0, -1),
                    13 // This item needs to move left to create the match
                ),

                // Horizontal 3-match: Move item from left to right
                PatternDefinition.Create(
                    MatchPattern.HorizontalRight,
                    new[] { new Vector2(1, 0), new Vector2(2, 0), new Vector2(-1, 0) },
                    Vector2.right,
                    new Vector2(-1, 0),
                    15 // This item needs to move right to create the match
                ),
                PatternDefinition.Create(
                    MatchPattern.HorizontalRight,
                    new[] { new Vector2(1, 0), new Vector2(2, 0), new Vector2(0, 1) },
                    Vector2.down,
                    new Vector2(0, 1),
                    17 // This item needs to move right to create the match
                ),
                PatternDefinition.Create(
                    MatchPattern.HorizontalRight,
                    new[] { new Vector2(1, 0), new Vector2(2, 0), new Vector2(0, -1) },
                    Vector2.up,
                    new Vector2(0, -1),
                    18 // This item needs to move right to create the match
                ),

                // Horizontal 3-match: Move item from above to below
                PatternDefinition.Create(
                    MatchPattern.Horizontal3FromUpCenter,
                    new[] { new Vector2(1, 0), new Vector2(-1, 0), new Vector2(0, 1) },
                    Vector2.down,
                    new Vector2(0, 1),
                    20 // This item needs to move down to create the match
                ),
                PatternDefinition.Create(
                    MatchPattern.Horizontal3FromUpCenter,
                    new[] { new Vector2(1, 0), new Vector2(-1, 0), new Vector2(0, -1) },
                    Vector2.up,
                    new Vector2(0, -1),
                    23 // This item needs to move down to create the match
                ),

                // Horizontal 3-match: Move item from below to above


                // Vertical 3-match: Move item from below to above
                PatternDefinition.Create(
                    MatchPattern.Vertical3FromUp,
                    new[] { new Vector2(0, -1), new Vector2(0, -2), new Vector2(0, 1) },
                    Vector3.down,
                    new Vector2(0, 1),
                    30 // This item needs to move up to create the match
                ),
                PatternDefinition.Create(
                    MatchPattern.Vertical3FromUp,
                    new[] { new Vector2(0, -1), new Vector2(0, -2), new Vector2(1, 0) },
                    Vector3.left,
                    new Vector2(1, 0),
                    32 // This item needs to move up to create the match
                ),
                PatternDefinition.Create(
                    MatchPattern.Vertical3FromUp,
                    new[] { new Vector2(0, -1), new Vector2(0, -2), new Vector2(-1, 0) },
                    Vector3.right,
                    new Vector2(-1, 0),
                    33 // This item needs to move up to create the match
                ),

                // Vertical 3-match: Move item from above to below
                PatternDefinition.Create(
                    MatchPattern.Vertical3FromDown,
                    new[] { new Vector2(0, 1), new Vector2(0, 2), new Vector2(0, -1) },
                    Vector2.up,
                    new Vector2(0, -1), 40 // This item needs to move down to create the match
                ),
                PatternDefinition.Create(
                    MatchPattern.Vertical3FromDown,
                    new[] { new Vector2(0, 1), new Vector2(0, 2), new Vector2(1, 0) },
                    Vector2.left,
                    new Vector2(1, 0), 42 // This item needs to move down to create the match
                ),
                PatternDefinition.Create(
                    MatchPattern.Vertical3FromDown,
                    new[] { new Vector2(0, 1), new Vector2(0, 2), new Vector2(-1, 0) },
                    Vector2.right,
                    new Vector2(-1, 0), 44 // This item needs to move down to create the match
                ),

                // Vertical 3-match: Move item from right to left


                // Vertical 3-match: Move item from left to right
                PatternDefinition.Create(
                    MatchPattern.Vertical3FromRightCenter,
                    new[] { new Vector2(0, 1), new Vector2(0, -1), new Vector2(1, 0) },
                    Vector2.left,
                    new Vector2(1, 0), 50 // This item needs to move right to create the match
                ),

                // Vertical 3-match: Move item from left to right (down)
                PatternDefinition.Create(
                    MatchPattern.Vertical3FromLeftDown,
                    new[] { new Vector2(0, 1), new Vector2(0, -1), new Vector2(-1, 0) },
                    Vector2.right,
                    new Vector2(-1, 0), 55 // This item needs to move left to create the match
                ),
            };
            return basicPatterns.ToList();
        }

        private List<PatternDefinition> InitializePatterns()
        {
            //             var exclusivePatterns = new[]
            // {
            //     PatternDefinition.Create(MatchPattern.Exclusives, new[] { new Vector2(1, 0) }, Vector2.left, new Vector2(1, 0), 1000),
            //     PatternDefinition.Create(MatchPattern.Exclusives, new[] { new Vector2(-1, 0) }, Vector2.right, new Vector2(-1, 0), 1005),
            //     PatternDefinition.Create(MatchPattern.Exclusives, new[] { new Vector2(0, 1) }, Vector2.down, new Vector2(0, 1), 1010),
            //     PatternDefinition.Create(MatchPattern.Exclusives, new[] { new Vector2(0, -1) }, Vector2.up, new Vector2(0, -1), 1020),


            // };

            // Adjusted extended matches (4 and 5 in shapes)
            var fourInRowPatterns = new[]
            {
                // Horizontal 4 without requiring more than 3 in a row (3 + 1 offset)
                PatternDefinition.Create(
                    MatchPattern.Horizontal4,
                    new[] { new Vector2(-2, 0), new Vector2(-1, 0), new Vector2(1, 0), new Vector2(0, -1) },
                    Vector2.up,
                    new Vector2(0, -1), 80 // This item needs to move to create the match
                ),
                PatternDefinition.Create(
                    MatchPattern.Horizontal4,
                    new[] { new Vector2(-2, 0), new Vector2(-1, 0), new Vector2(1, 0), new Vector2(0, 1) },
                    Vector2.down,
                    new Vector2(0, 1), 85 // This item needs to move to create the match
                ),
                PatternDefinition.Create(
                    MatchPattern.Horizontal4,
                    new[] { new Vector2(-1, 0), new Vector2(1, 0), new Vector2(2, 0), new Vector2(0, -1) },
                    Vector2.up,
                    new Vector2(0, -1), 90 // This item needs to move to create the match
                ),
                PatternDefinition.Create(
                    MatchPattern.Horizontal4,
                    new[] { new Vector2(-1, 0), new Vector2(1, 0), new Vector2(2, 0), new Vector2(0, 1) },
                    Vector2.down,
                    new Vector2(0, 1), 95 // This item needs to move to create the match
                ),
// Vertical 4 without requiring more than 3 in a column (3 + 1 offset)
                PatternDefinition.Create(
                    MatchPattern.Vertical4,
                    new[] { new Vector2(0, -2), new Vector2(0, -1), new Vector2(0, 1), new Vector2(-1, 0) },
                    Vector2.right,
                    new Vector2(-1, 0), 100 // This item needs to move to create the match
                ),
                PatternDefinition.Create(
                    MatchPattern.Vertical4,
                    new[] { new Vector2(0, -2), new Vector2(0, -1), new Vector2(0, 1), new Vector2(1, 0) },
                    Vector2.left,
                    new Vector2(1, 0), 105 // This item needs to move to create the match
                ),
                PatternDefinition.Create(
                    MatchPattern.Vertical4,
                    new[] { new Vector2(0, -1), new Vector2(0, 1), new Vector2(0, 2), new Vector2(-1, 0) },
                    Vector2.right,
                    new Vector2(-1, 0), 110 // This item needs to move to create the match
                ),
                PatternDefinition.Create(
                    MatchPattern.Vertical4,
                    new[] { new Vector2(0, -1), new Vector2(0, 1), new Vector2(0, 2), new Vector2(1, 0) },
                    Vector2.left,
                    new Vector2(1, 0), 115 // This item needs to move to create the match
                ),
            };


            var HeliCopterPattern = new[]
            {
                // Move UP to complete 2x2
                PatternDefinition.Create(
                    MatchPattern.HeliCopter,
                    new[] { new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -1) },
                    Vector2.up,
                    new Vector2(0, -1), 83 // Moves up to form the 2x2 block
                ),
                PatternDefinition.Create(
                    MatchPattern.HeliCopter,
                    new[] { new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(-1, 0) },
                    Vector2.right,
                    new Vector2(-1, 0), 93 // Moves up to form the 2x2 block
                ),

                // Move DOWN to complete 2x2
                PatternDefinition.Create(
                    MatchPattern.HeliCopter,
                    new[] { new Vector2(1, 0), new Vector2(0, -1), new Vector2(1, -1), new Vector2(0, 1) },
                    Vector2.down,
                    new Vector2(0, 1), 98 // Moves down to form the 2x2 block
                ),
                PatternDefinition.Create(
                    MatchPattern.HeliCopter,
                    new[] { new Vector2(1, 0), new Vector2(0, -1), new Vector2(1, -1), new Vector2(-1, 0) },
                    Vector2.right,
                    new Vector2(-1, 0), 102 // Moves down to form the 2x2 block
                ),

                // Move LEFT to complete 2x2
                PatternDefinition.Create(
                    MatchPattern.HeliCopter,
                    new[] { new Vector2(0, 1), new Vector2(-1, 0), new Vector2(-1, 1), new Vector2(1, 0) },
                    Vector2.left,
                    new Vector2(1, 0), 105 // Moves left to form the 2x2 block
                ),
                PatternDefinition.Create(
                    MatchPattern.HeliCopter,
                    new[] { new Vector2(0, 1), new Vector2(-1, 0), new Vector2(-1, 1), new Vector2(0, -1) },
                    Vector2.up,
                    new Vector2(0, -1), 89 // Moves left to form the 2x2 block
                ),

                PatternDefinition.Create(
                    MatchPattern.HeliCopter,
                    new[] { new Vector2(0, -1), new Vector2(-1, 0), new Vector2(-1, -1), new Vector2(1, 0) },
                    Vector2.left,
                    new Vector2(1, 0), 91 // Moves left to form the 2x2 block
                ),
                PatternDefinition.Create(
                    MatchPattern.HeliCopter,
                    new[] { new Vector2(0, -1), new Vector2(-1, 0), new Vector2(-1, -1), new Vector2(0, +1) },
                    Vector2.down,
                    new Vector2(0, +1), 94 // Moves left to form the 2x2 block
                ),

                // Move RIGHT to complete 2x2
                //PatternDefinition.Create(
                //    MatchPattern.HeliCopter,
                //    new [] { new Vector2(0, 1), new Vector2(1, 0), new Vector2(1, 1), new Vector2(-1, 0) },
                //    Vector2.right,
                //    new Vector2(-1, 0),145 // Moves right to form the 2x2 block
                //)
            };

            var MulticolorPattern = new[]
            {
                // Multicolor horizontal
                PatternDefinition.Create(
                    MatchPattern.Multicolor,
                    new[] { new Vector2(-2, 0), new Vector2(-1, 0), new Vector2(1, 0), new Vector2(2, 0), new Vector2(0, 1) },
                    Vector2.down,
                    new Vector2(0, 1), 600
                ),
                PatternDefinition.Create(
                    MatchPattern.Multicolor,
                    new[] { new Vector2(-2, 0), new Vector2(-1, 0), new Vector2(1, 0), new Vector2(2, 0), new Vector2(0, -1) },
                    Vector2.up,
                    new Vector2(0, -1), 650
                ),

                // Multicolor vertical
                PatternDefinition.Create(
                    MatchPattern.Multicolor,
                    new[] { new Vector2(0, -2), new Vector2(0, -1), new Vector2(0, 1), new Vector2(0, 2), new Vector2(1, 0) },
                    Vector2.left,
                    new Vector2(1, 0), 700
                ),
                PatternDefinition.Create(
                    MatchPattern.Multicolor,
                    new[] { new Vector2(0, -2), new Vector2(0, -1), new Vector2(0, 1), new Vector2(0, 2), new Vector2(-1, 0) },
                    Vector2.right,
                    new Vector2(-1, 0), 720
                ),
            };

            var BombPattern = new[]
            {
                // L-Shape (Top-Left Corner)
                PatternDefinition.Create(
                    MatchPattern.LShapeTopLeft,
                    new[] { new Vector2(-1, 0), new Vector2(-2, 0), new Vector2(0, 1), new Vector2(0, 2), new Vector2(0, -1) },
                    Vector2.up,
                    new Vector2(0, -1), 200 // Moves up to form the L
                ),
                PatternDefinition.Create(
                    MatchPattern.LShapeTopLeft,
                    new[] { new Vector2(-1, 0), new Vector2(-2, 0), new Vector2(0, 1), new Vector2(0, 2), new Vector2(1, 0) },
                    Vector2.left,
                    new Vector2(1, 0), 202 // Moves up to form the L
                ),
                //PatternDefinition.Create(
                //    MatchPattern.LShapeTopLeft,
                //    new[] { new Vector2(-1, 0), new Vector2(-2, 0), new Vector2(0, 1), new Vector2(0, 2), new Vector2(-1, 0) },
                //    Vector2.right,
                //    new Vector2(-1, 0),203 // Moves up to form the L
                //),

                // L-Shape (Top-Right Corner)
                PatternDefinition.Create(
                    MatchPattern.LShapeTopRight,
                    new[] { new Vector2(1, 0), new Vector2(2, 0), new Vector2(0, 1), new Vector2(0, 2), new Vector2(0, -1) },
                    Vector2.up,
                    new Vector2(0, -1), 220 // Moves up to form the L
                ),
                PatternDefinition.Create(
                    MatchPattern.LShapeTopRight,
                    new[] { new Vector2(1, 0), new Vector2(2, 0), new Vector2(0, 1), new Vector2(0, 2), new Vector2(-1, 0) },
                    Vector2.right,
                    new Vector2(-1, 0), 222 // Moves up to form the L
                ),

                // L-Shape (Bottom-Left Corner)
                PatternDefinition.Create(
                    MatchPattern.LShapeBottomLeft,
                    new[] { new Vector2(-1, 0), new Vector2(-2, 0), new Vector2(0, -1), new Vector2(0, -2), new Vector2(0, 1) },
                    Vector2.down,
                    new Vector2(0, 1), 230 // Moves down to form the L
                ),
                PatternDefinition.Create(
                    MatchPattern.LShapeBottomLeft,
                    new[] { new Vector2(-1, 0), new Vector2(-2, 0), new Vector2(0, -1), new Vector2(0, -2), new Vector2(1, 0) },
                    Vector2.left,
                    new Vector2(1, 0), 232 // Moves down to form the L
                ),
                // L-Shape (Bottom-Right Corner)
                PatternDefinition.Create(
                    MatchPattern.LShapeBottomRight,
                    new[] { new Vector2(1, 0), new Vector2(2, 0), new Vector2(0, -1), new Vector2(0, -2), new Vector2(0, 1) },
                    Vector2.down,
                    new Vector2(0, 1), 240 // Moves down to form the L
                ),
                PatternDefinition.Create(
                    MatchPattern.LShapeBottomRight,
                    new[] { new Vector2(1, 0), new Vector2(2, 0), new Vector2(0, -1), new Vector2(0, -2), new Vector2(-1, 0) },
                    Vector2.right,
                    new Vector2(-1, 0), 242 // Moves down to form the L
                ),
                // T-Shape (Vertical, moves UP)
                PatternDefinition.Create(
                    MatchPattern.TShapeUp,
                    new[] { new Vector2(1, 0), new Vector2(-1, 0), new Vector2(0, 1), new Vector2(0, 2), new Vector2(0, -1) },
                    Vector2.up,
                    new Vector2(0, -1), 250 // Moves up to form the T shape
                ),

                // T-Shape (Vertical, moves DOWN)
                PatternDefinition.Create(
                    MatchPattern.TShapeDown,
                    new[] { new Vector2(1, 0), new Vector2(-1, 0), new Vector2(0, -1), new Vector2(0, -2), new Vector2(0, 1) },
                    Vector2.down,
                    new Vector2(0, 1), 265 // Moves down to form the T shape
                ),

                // T-Shape (Horizontal, moves LEFT)
                PatternDefinition.Create(
                    MatchPattern.TShapeLeft,
                    new[] { new Vector2(0, 1), new Vector2(0, -1), new Vector2(-1, 0), new Vector2(-2, 0), new Vector2(1, 0) },
                    Vector2.left,
                    new Vector2(1, 0), 280 // Moves left to form the T shape
                ),

                // T-Shape (Horizontal, moves RIGHT)
                PatternDefinition.Create(
                    MatchPattern.TShapeRight,
                    new[] { new Vector2(0, 1), new Vector2(0, -1), new Vector2(1, 0), new Vector2(2, 0), new Vector2(-1, 0) },
                    Vector2.right,
                    new Vector2(-1, 0), 290 // Moves right to form the T shape
                )
            };

            // Combine and sort by priority (larger patterns first)
            return MulticolorPattern
                .Concat(BombPattern)
                .Concat(fourInRowPatterns)
                .Concat(HeliCopterPattern)
                // .Concat(exclusivePatterns)
                //.Concat(basicPatterns)
                .Where(p => p.IsValid())
                .ToList();
            ;
        }


        /// <summary>
        /// Optionally set current patterns externally if you only want to detect certain types.
        /// </summary>
        public void SetCurrentPatterns(List<PatternDefinition> patterns)
        {
            currentPatterns = patterns;
        }

        public void SetCurrentPatterns(List<MatchPattern> patternTypes)
        {
            var allPatterns = InitializePatterns(); // All possible definitions
            //currentPatterns = allPatterns.Where(p => patternTypes.Contains(p.Type)).ToList();
            currentPatterns = allPatterns;
        }

        /// <summary>
        /// Confirms the game state is ready for match checking.
        /// E.g., game is playing, not blocked, etc.
        /// </summary>
        private bool CanCheckForMatches()
        {
            return !MainManager.Instance.DragBlocked &&
                   (MainManager.Instance.gameStatus == GameState.Playing ||
                    MainManager.Instance.gameStatus == GameState.Tutorial);
        }

        /// <summary>
        /// Simple coroutine that waits for 3 seconds, then ensures the board is stable.
        /// </summary>
        private IEnumerator InitializeCheck()
        {
            if (!itemSprites) InitSprites();
            yield return new WaitForSeconds(3);
            allowShowTip = true;

            // Typically you store these in your level data
            maxRow = MainManager.Instance.levelData.maxRows;
            maxCol = MainManager.Instance.levelData.maxCols;
            gotTip = false;

            // Wait until the game is ready
            while (!IsGameReady())
            {
                yield return new WaitForEndOfFrame();
            }
        }

        private bool IsGameReady()
        {
            // Basic checks: existence, correct state, no items are falling
            if (MainManager.Instance == null) return false;
            if (MainManager.Instance.gameStatus != GameState.Playing &&
                MainManager.Instance.gameStatus != GameState.Tutorial) return false;
            if (MainManager.Instance.DragBlocked ||
                MainManager.Instance.field.GetItems().Any(i => i.falling || i.destroying))
                return false;

            return true;
        }


        // ------------------------------------------------------------------------
        // Dummy placeholders for missing methods in your snippet:
        // You likely already have these, so adapt them as appropriate.
        // ------------------------------------------------------------------------


        //show tip function calls coroutine for
        void showTip(List<Item> nextMoveItems, Vector2 direction, MatchPattern patterns = MatchPattern.None)
        {
            //Debug.Log($"Next move items count: {nextMoveItems.Count}");
            //Debug.Log($"show vDirection {DirectionToMove}");
            //Debug.Log("showTip");

            StopCoroutine(showTipCor(nextMoveItems, direction, patterns));
            StartCoroutine(showTipCor(nextMoveItems, direction, patterns));

            // Use OuterGraph instead
        }

        private SquareBoundaryLine squareBoundaryLine;
        //show tip coroutine split into smaller methods


        IEnumerator showTipCor(List<Item> nextMoveItems, Vector2 direction, MatchPattern patterns = MatchPattern.None)
        {
            InitializeTipState();

            if (ShouldExitPreWait())
            {
                DecrementCoroutineCount();
                yield break;
            }

            tipID = MainManager.Instance.moveID;
            yield return new WaitForSeconds(1);

            if (!ValidateAfterWait())
            {
                DecrementCoroutineCount();
                yield break;
            }

            if (!ValidateItems(nextMoveItems))
            {
                DecrementCoroutineCount();
                yield break;
            }

            bool needSquareAnimation = nextMoveItems.Count != 4;
            var squares = ProcessTipAnimations(nextMoveItems, needSquareAnimation, direction);
            if (needSquareAnimation)
                squareBoundaryLine.SetSquares(squares); // Draw using OuterGraph


            DecrementCoroutineCount();
        }


        // Initializes state for the tip coroutine.
        void InitializeTipState()
        {
            changeTipAI = false;
            gotTip = true;
            corCount++;
        }

        // Checks conditions to exit before waiting.
        bool ShouldExitPreWait()
        {
            return corCount > 1 || (MainManager.Instance.DragBlocked && !allowShowTip);
        }

        // Validates game state after the initial wait.
        bool ValidateAfterWait()
        {
            return !(MainManager.Instance.DragBlocked && !allowShowTip && tipID != MainManager.Instance.moveID);
        }

        // Ensures none of the items is null.
        bool ValidateItems(List<Item> items)
        {
            return !items.Any(item => item == null);
        }

        // Triggers animation for each valid item and collects square data.
        private Vector3 originalPos;
        private Transform itemToMove;
        private Animator tipedItem;

        private string directionUsing;
        // Updated ProcessTipAnimations() to use OuterGraph.SquareData

        List<SquareBoundaryLine.SquareData> ProcessTipAnimations(List<Item> nextMoveItems, bool DoesNeedSquareAnimation, Vector2 direction)
        {
            tipAnimations = nextMoveItems;

            var squares = new List<SquareBoundaryLine.SquareData>();
            if (nextMoveItems.Count == 2)
            {
                for (int i = 0; i < tipAnimations.Count; i++)
                {
                    tipAnimations[i].anim.ResetTrigger("stop");

                    if (tipAnimations[i].anim != null && tipAnimations[i].square != null)
                    {
                        if (i == tipAnimations.Count - 1)
                        {
                            tipedItem = tipAnimations[i].anim;
                            if (direction == Vector2.up)
                            {
                                directionUsing = "UpTip";
                                tipedItem.SetTrigger(directionUsing);
                            }
                            else if (direction == Vector2.down)
                            {
                                directionUsing = "DownTip";
                                tipedItem.SetTrigger(directionUsing);
                            }
                            else if (direction == Vector2.left)
                            {
                                directionUsing = "LeftTip";
                                tipedItem.SetTrigger(directionUsing);
                            }
                            else if (direction == Vector2.right)
                            {
                                directionUsing = "RightTip";
                                tipedItem.SetTrigger(directionUsing);
                            }
                        }

                        squares.Add(new SquareBoundaryLine.SquareData(
                            new Vector3(tipAnimations[i].square.transform.position.x, tipAnimations[i].square.transform.position.y, 0),
                            .82f));
                    }
                }
            }
            else
            {
                for (int i = 0; i < tipAnimations.Count; i++)
                {
                    tipAnimations[i].anim.ResetTrigger("stop");

                    if (i == tipAnimations.Count - 1)
                    {
                        //var squareTransform = nextMoveItems[i].transform;
                        ////print($"SalamatBashid{nextMoveItems[i].transform.name}");
                        ////print($"SalamatBashid2{squareTransform.name}");
                        //Vector3 originalPosition = squareTransform.position;
                        //itemToMove = squareTransform;
                        //originalPos = squareTransform.position;
                        //float moveDistance = 0.15f;
                        //float moveDuration = 0.3f;
                        //Vector3 moveOffset = new Vector3(DirectionToMove.x, DirectionToMove.y, 0) * moveDistance;
                        //Vector3 targetPosition = originalPosition + moveOffset;
                        ////print("SalamatBashid" + targetPosition);
                        ////print("SalamatBashidOriginal" + originalPosition);

                        //activeTipSequence = DOTween.Sequence();
                        //activeTipSequence.Append(squareTransform.DOMove(targetPosition, moveDuration)
                        //                                       .SetEase(DG.Tweening.Ease.InOutSine));
                        //activeTipSequence.Append(squareTransform.DOMove(originalPosition, moveDuration)
                        //                                       .SetEase(DG.Tweening.Ease.InOutSine));
                        //activeTipSequence.SetLoops(3, LoopType.Restart);
                        //activeTipSequence.Play();
                        // tipAnimations[i].anim.SetBool("package_idle", false);
                        // tipAnimations[i].anim.SetTrigger("tip");
                        // tipAnimations[i].anim.ResetTrigger("stop");
                        // tipAnimations[i].anim.SetTrigger("tip");
                        tipedItem = tipAnimations[i].anim;
                        if (direction == Vector2.up)
                        {
                            directionUsing = "UpTip";
                            tipedItem.SetTrigger(directionUsing);
                        }
                        else if (direction == Vector2.down)
                        {
                            directionUsing = "DownTip";
                            tipedItem.SetTrigger(directionUsing);
                        }
                        else if (direction == Vector2.left)
                        {
                            directionUsing = "LeftTip";
                            tipedItem.SetTrigger(directionUsing);
                        }
                        else if (direction == Vector2.right)
                        {
                            directionUsing = "RightTip";
                            tipedItem.SetTrigger(directionUsing);
                        }
                    }
                    else
                    {
                        if (tipAnimations[i].anim != null && tipAnimations[i].square != null)
                        {
                            if (i != tipAnimations.Count - 2)
                            {
                                // tipAnimations[i].anim.SetBool("package_idle", false);
                                //tipAnimations[i].anim.SetTrigger("tip");
                            }

                            if (DoesNeedSquareAnimation)
                                squares.Add(new SquareBoundaryLine.SquareData(
                                    new Vector3(tipAnimations[i].square.transform.position.x, tipAnimations[i].square.transform.position.y, 0),
                                    .82f));
                        }
                    }
                }
            }

            // StartCoroutine(EndingAnimation());
            return squares;
        }

        // private IEnumerator EndingAnimation()
        // {
        //     // throw new NotImplementedException();
        //     yield return new WaitForSeconds(1.1f);
        //     if(tipedItem!=null)
        //     {
        //         tipedItem?.SetTrigger("stop");
        //     }
        // }

        List<Item> tipAnimations = new List<Item>();

        public void StopTipAnimation()
        {
            if (tipAnimations.Count > 0)
                foreach (var item in tipAnimations)
                    if (item != null && item.anim != null)
                    {
                        item.anim.SetTrigger("stop");
                    }

            if (tipedItem != null)
            {
                tipedItem?.SetTrigger("stop");
            }

            StartCoroutine(TryAgainForFix());

            //if (activeTipSequence != null)
            //{
            //    itemToMove.position = originalPos;
            //    activeTipSequence.Kill();
            //}
        }

        private IEnumerator TryAgainForFix()
        {
            yield return new WaitForSeconds(1);
            if (tipAnimations.Count > 0)
                foreach (var item in tipAnimations)
                    if (item != null && item.anim != null)
                    {
                        item.anim.SetTrigger("stop");
                    }

            if (tipedItem != null)
            {
                tipedItem?.SetTrigger("stop");
            }

            tipAnimations.Clear();
            isActiveIt = false;
        }

        // Draws the outline, waits, and then clears it.
        // Updated OutlineSequence to use OuterGraph
        // Updated OutlineSequence() to use OuterGraph

        public CombineClass GetChopperCombines()
        {
            for (var COLOR = 0; COLOR < 6; COLOR++)
            {
                for (var col = 0; col < MainManager.Instance.levelData.maxCols; col++)
                {
                    for (var row = 0; row < MainManager.Instance.levelData.maxRows; row++)
                    {
                        var square = MainManager.Instance.GetSquare(col, row);
                        //Get Chopper tip
                        int[] array;
                        array = new[]
                        {
                            1, 1, 0,
                            1, 1, 0,
                            0, 0, 0
                        };
                        if (CheckMatrix(square, array, COLOR, 0))
                        {
                            var l = new Item[currentPreCombine.Count];
                            for (var index = 0; index < currentPreCombine.Count; index++)
                            {
                                var item = currentPreCombine[index];
                                l[index] = item;
                            }

                            return new CombineClass { items = l.ToList() };
                        }

                        currentPreCombine.Clear();
                    }
                }
            }

            return null;
        }

        private bool CheckMatrix(Rectangle rectangle, int[] array, int COLOR, int findIndex)
        {
            if (rectangle == null) return false;

            vDirection = GetDirection(4) - GetDirection(findIndex);
            vDirection = GetDirection(4) - GetDirection(findIndex);
            ////Debug.Log($"CheckMatrix: vDirection calculated as {vDirection}");

            // Ensure the given pattern does not go out of the game board boundaries
            vDirection = GetDirection(4) - GetDirection(findIndex);
            ////Debug.Log($"CheckMatrix: vDirection calculated as {vDirection}");

            // Ensure the given pattern does not go out of the game board boundaries
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i] == 0) continue;
                var arraySq = rectangle.GetPosition() + GetDirection(i);
                if (vDirection.x > 0 && arraySq.x < 0) return false;
                if (vDirection.x < 0 && arraySq.x > maxCol - 1) return false;
                if (vDirection.y > 0 && arraySq.y < 0) return false;
                if (vDirection.y < 0 && arraySq.y > maxRow - 1) return false;
            }

            int c = 0;
            var moveSq = GetRelativeSquare(rectangle, GetDirection(findIndex));
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i] == 2 && CheckSquare(moveSq, COLOR, true)) c++;
                else if (array[i] == 1 && CheckSquare(GetRelativeSquare(rectangle, GetDirection(i)), COLOR)) c++;
            }

            if (c == 4 && moveSq.CanGoInto())
            {
                showTip(currentPreCombine, Vector2.zero);
                TipItem = currentPreCombine[0];
                foreach (var item in currentPreCombine)
                {
                    //item.shouldMove = false; // Reset the shouldMove to false
                }

                return true;
            }

            return false;
        }

        private Vector2 GetDirection(int num)
        {
            if (num == 0) return new Vector2(-1, -1);
            if (num == 1) return new Vector2(0, -1);
            if (num == 2) return new Vector2(1, -1);
            if (num == 3) return new Vector2(-1, 0);
            if (num == 4) return new Vector2(0, 0);
            if (num == 5) return new Vector2(1, 0);
            if (num == 6) return new Vector2(-1, 1);
            if (num == 7) return new Vector2(0, 1);
            if (num == 8) return new Vector2(1, 1);
            return Vector2.zero;
        }

        private Rectangle GetRelativeSquare(Rectangle sq, Vector2 vector2)
        {
            Rectangle relativeRectangle = null;
            if (!sq.directionRestriction.Any(i => i == vector2))
                relativeRectangle = sq.field.GetSquare(sq.GetPosition() + vector2);
            return relativeRectangle;
        }

        // Decrements the coroutine count.
        void DecrementCoroutineCount()
        {
            corCount--;
        }
    }
}
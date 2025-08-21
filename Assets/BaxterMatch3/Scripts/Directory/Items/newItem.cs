// using System;
// using System.Collections;
// using System.Collections.Generic;
// using System.Linq;
// using UnityEngine;

// public class FallingItem : MonoBehaviour
// {
//     // =============== Configurable Fields ===============
//     public bool isLoggingEnabled = false; // Enable/disable logging
    
//     // Reference fields (must be assigned accordingly in your real code)
//     public Animator anim;
//     public Field field;
//     public Square square;
//     public Square previousSquare;
//     public ItemsTypes currentType;
//     public ItemsTypes NextType;
//     public bool falling;
//     public bool needFall;
//     public bool JustCreatedItem;
//     public int fallingID;
//     public Color color; // Item's color, used for matches
//     public AnimationCurve fallingCurve;

//     // Placeholder: These should be defined in your code
//     // or passed in as dependencies.
//     // Example: LevelManager.THIS, SoundBase, etc.
//     // Here we assume these exist as in your original code.
    
//     // =============== Private/Internal Fields ===============
//     private bool skipFalling = false; // From Script 2 (handle carefully)
//     private Action onFallingCompleteCallback;

//     #region Public Entry Points
    
//     /// <summary>
//     /// Starts the falling process towards a list of waypoints.
//     /// </summary>
//     /// <param name="waypoints">The path along which the item falls.</param>
//     /// <param name="animate">Whether to animate the movement.</param>
//     /// <param name="callback">Action to call on fall completion.</param>
//     public void StartFallingTo(List<Waypoint> waypoints, bool animate = true, Action callback = null)
//     {
//         onFallingCompleteCallback = callback;
//         StartCoroutine(FallingCor(waypoints, animate, callback));
//     }

//     #endregion

//     #region Core Falling Coroutine

//     /// <summary>
//     /// The main falling coroutine.
//     /// It initializes the fall, processes each waypoint, and handles post-fall logic.
//     /// </summary>
//     private IEnumerator newFallingCor(List<Waypoint> waypoints, bool animate, Action callback = null)
//     {
//         Log($"[FallingScript][FallingCor] Starting coroutine. ItemID={GetInstanceID()} WaypointsCount={waypoints.Count} animate={animate}");

//         // Initialize falling
//         if (InitializeFalling()) yield break;
//         if (skipFalling)
//         {
//             Log($"[FallingScript][FallingCor] skipFalling=true. ItemID={GetInstanceID()} Exiting early.");
//             FinalizeImmediately();
//             yield break;
//         }

//         // Wait for falling to resume if stopped externally
//         yield return new WaitWhile(() => LevelManager.THIS.StopFall);

//         float startTimeGlobal = Time.time;
        
//         // Process each waypoint
//         for (int i = 0; i < waypoints.Count; i++)
//         {
//             yield return ProcessWaypoint(waypoints, i, animate, startTimeGlobal);
//         }

//         // Handle post-fall actions once all waypoints are processed
//         yield return HandlePostFall(animate, callback);

//         Log($"[FallingScript][FallingCor] Completed falling. ItemID={GetInstanceID()} Position={transform.position}");
//     }

//     #endregion

//     #region Initialization and Finalization

//     /// <summary>
//     /// Initializes the falling process. Returns true if already falling and should exit.
//     /// </summary>
//     private bool InitializeFalling()
//     {
//         if (fallingID > 0)
//         {
//             Log($"[FallingScript][InitializeFalling] Already falling. ItemID={GetInstanceID()}");
//             return true; // Exit if already falling
//         }

//         fallingID++;
//         LevelManager.THIS.FindMatches();
//         falling = true;
//         needFall = false;

//         Log($"[FallingScript][InitializeFalling] Initialized falling. ItemID={GetInstanceID()} fallingID={fallingID}");

//         return false;
//     }

//     /// <summary>
//     /// Immediately finalize falling (if skipping or aborted).
//     /// </summary>
//     private void FinalizeImmediately()
//     {
//         falling = false;
//         needFall = false;
//         fallingID = 0;
//     }

//     #endregion

//     #region Waypoint Processing

//     /// <summary>
//     /// Processes movement to a single waypoint.
//     /// Includes movement, speed calculation, teleportation effects, and collision checks.
//     /// </summary>
//     private IEnumerator ProcessWaypoint(List<Waypoint> waypoints, int index, bool animate, float startTimeGlobal)
//     {
//         // Retrieve the waypoint
//         Waypoint waypoint = waypoints[index];
//         Vector3 startPos = transform.position;
//         Vector3 destPos = waypoint.destPosition + Vector3.back * 0.2f;
//         float distance = Vector2.Distance(startPos, destPos);

//         // If minimal distance, no need to move
//         if (distance < 0.2f)
//         {
//             Log($"[FallingScript][ProcessWaypoint] Distance too small, skipping. ItemID={GetInstanceID()} Index={index}");
//             yield break;
//         }

//         // Apply teleportation effects if applicable
//         ApplyTeleportationEffects(waypoint);

//         // Begin moving towards the destination
//         yield return MoveTowardsDestination(waypoints, index, startTimeGlobal, startPos, destPos, waypoint);
//     }

//     /// <summary>
//     /// Applies teleportation effects if the waypoint is part of a teleportation system.
//     /// </summary>
//     private void ApplyTeleportationEffects(Waypoint waypoint)
//     {
//         if (waypoint.square?.teleportDestination != null || waypoint.square?.teleportOrigin != null)
//         {
//             TeleportationEffect(waypoint.square);
//             Log($"[FallingScript][ApplyTeleportationEffects] TeleportationEffect applied. ItemID={GetInstanceID()} Square={waypoint.square}");
//         }
//     }

//     #endregion

//     #region Movement Logic

//     /// <summary>
//     /// Moves the item from its current position towards the target destination.
//     /// Handles pausing, collision checks, instant move conditions, and mid-fall square changes.
//     /// </summary>
//     private IEnumerator MoveTowardsDestination(List<Waypoint> waypoints, int i, float startTimeGlobal, Vector3 startPos, Vector3 destPos, Waypoint waypoint)
//     {
//         float startTime = Time.time;
//         float pauseTime = Time.time;
//         float totalPauseTime = 0.0f;
//         float fracJourney = 0f;
//         bool fallStopped = false;

//         while (fracJourney < 0.9f)
//         {
//             // If fall is stopped, record pause time and wait
//             if (LevelManager.THIS.StopFall)
//             {
//                 fallStopped = true;
//                 pauseTime = Time.time;
//                 Log($"[FallingScript][MoveTowardsDestination] Fall stopped. ItemID={GetInstanceID()}");
//             }

//             yield return new WaitWhile(() => LevelManager.THIS.StopFall);

//             if (fallStopped && !LevelManager.THIS.StopFall)
//             {
//                 // Fall resumed
//                 fallStopped = false;
//                 totalPauseTime += Time.time - pauseTime;
//                 startTime += totalPauseTime;
//                 startTimeGlobal += totalPauseTime;
//                 Log($"[FallingScript][MoveTowardsDestination] Fall resumed. ItemID={GetInstanceID()} totalPauseTime={totalPauseTime}");
//             }

//             // Calculate speed using improved logic
//             float speed = ApplySpeedCalculation(i, startTimeGlobal);

//             // Check collisions
//             RaycastHit2D hit2D = DetectCollisionTowards(destPos, startPos);

//             // Decide how to move based on collision
//             if (CanMoveWithoutStopping(hit2D, startPos, destPos))
//             {
//                 // Move towards the destination
//                 fracJourney = UpdatePositionOverTime(startPos, destPos, speed, startTime);
//             }
//             else if (ShouldSkipItem(hit2D))
//             {
//                 // Reset times if skipping item to attempt a move again
//                 SkipItem(ref startPos, ref startTimeGlobal, ref startTime);
//                 fracJourney = 0f; // Restart journey fraction
//             }

//             // Check instant move condition (if item is too far from the square)
//             if (waypoint.instant && Vector2.Distance(square.transform.position, transform.position) > 2)
//             {
//                 PerformInstantMove(waypoint);
//                 yield break; // Stop current movement logic
//             }

//             // Attempt a mid-fall square change if fraction >= 0.5
//             if (fracJourney >= 0.5f)
//             {
//                 AttemptMidFallSquareChange(waypoints, ref startPos, destPos, waypoint);
//             }

//             // If still not reached destination, wait a frame
//             if (fracJourney < 1f)
//                 yield return new WaitForEndOfFrame();
//         }
//     }

//     /// <summary>
//     /// Applies the improved speed calculation logic.
//     /// Integrates new features: speed adjusts based on index and game state.
//     /// </summary>
//     private float ApplySpeedCalculation(int index, float startTimeGlobal)
//     {
//         int sideFall = (LevelManager.THIS.gameStatus == GameState.PreWinAnimations) ? 3 : 2;
//         float indexFactor = 1 + (index * 0.5f); // Slight incremental factor per waypoint
//         float speed = fallingCurve.Evaluate(Time.time - startTimeGlobal) * sideFall * indexFactor;
//         return speed;
//     }

//     /// <summary>
//     /// Detects collision with other items along the path.
//     /// </summary>
//     private RaycastHit2D DetectCollisionTowards(Vector3 destPos, Vector3 startPos)
//     {
//         Vector3 direction = (destPos - startPos).normalized;
//         RaycastHit2D[] hits = new RaycastHit2D[2];
//         Physics2D.RaycastNonAlloc(transform.position + direction * -0.5f, direction, hits, 0.8f, 1 << LayerMask.NameToLayer("Item"));
//         return hits.FirstOrDefault(x => x.transform != transform);
//     }

//     /// <summary>
//     /// Determines if movement can continue based on collision detection.
//     /// Original logic retained, but structured more clearly.
//     /// </summary>
//     private bool CanMoveWithoutStopping(RaycastHit2D hit2D, Vector3 startPos, Vector3 destPos)
//     {
//         // If no collision or collided item isn't falling, we can move
//         if (!hit2D || !hit2D.transform.GetComponent<Item>().falling) return true;

//         // If direction doesn't match expected directions, don't move
//         Vector2 direction = ((Vector2)destPos - (Vector2)startPos).normalized;
//         if (direction != ((Vector2)square.transform.position - (Vector2)startPos).normalized)
//             return false;
//         if (direction != square.direction)
//             return false;

//         // If we should skip item due to logic, movement halts temporarily
//         return !ShouldSkipItem(hit2D);
//     }

//     /// <summary>
//     /// Determines if we should skip the item currently blocking the fall.
//     /// This logic can be adjusted if the skipping caused issues.
//     /// </summary>
//     private bool ShouldSkipItem(RaycastHit2D hit2D)
//     {
//         // In original code, this was a separate condition check.
//         // Adjust this as needed. If skip logic caused bugs, we can limit it:
//         // Return false to disable skipping or refine logic if needed.
//         return false; 
//     }

//     /// <summary>
//     /// Resets the start positions and time tracking if we decide to skip an item.
//     /// </summary>
//     private void SkipItem(ref Vector3 startPos, ref float startTimeGlobal, ref float startTime)
//     {
//         startPos = transform.position;
//         startTimeGlobal = Time.time;
//         startTime = Time.time;
//         Log($"[FallingScript][SkipItem] Skipping item. ItemID={GetInstanceID()}");
//     }

//     /// <summary>
//     /// Updates the item position over time based on speed and returns fraction of journey completed.
//     /// </summary>
//     private float UpdatePositionOverTime(Vector3 startPos, Vector3 destPos, float speed, float startTime)
//     {
//         float distCovered = (Time.time - startTime) * speed;
//         float distance = Vector2.Distance(startPos, destPos);
//         if (distance == 0) return 1f;

//         float fracJourney = Mathf.Clamp01(distCovered / distance);
//         float smoothedFrac = Mathf.SmoothStep(0, 1, fracJourney);
//         transform.position = Vector2.Lerp(startPos, destPos, smoothedFrac);

//         return fracJourney;
//     }

//     /// <summary>
//     /// Performs an instant move if the item is too far from its target position (teleport scenario).
//     /// </summary>
//     private void PerformInstantMove(Waypoint waypoint)
//     {
//         Vector3 pos = square.GetReverseDirection();
//         Vector3 instantDest = waypoint.destPosition + Vector3.back * 0.2f + pos * field.squareHeight;
//         transform.position = instantDest;
//         JustCreatedItem = true;
//         falling = false;
//         needFall = true;
//         fallingID = 0;

//         Log($"[FallingScript][PerformInstantMove] Instant move triggered. ItemID={GetInstanceID()} Pos={transform.position}");

//         List<Waypoint> newWaypoints = new List<Waypoint> { new Waypoint(square.transform.position, square) };
//         StartFallingTo(newWaypoints);
//     }

//     /// <summary>
//     /// Attempts to change the square mid-fall if we are halfway through the journey.
//     /// If next square is free, move the item to that square and add a new waypoint.
//     /// </summary>
//     private void AttemptMidFallSquareChange(List<Waypoint> waypoints, ref Vector3 startPos, Vector3 destPos, Waypoint waypoint)
//     {
//         Square squareNew = square.GetNextSquare(true);
//         if (squareNew != null && squareNew.Item == null && squareNew.IsFree())
//         {
//             JustCreatedItem = false;
//             square.Item = null;
//             squareNew.Item = this;
//             Log($"[FallingScript][AttemptMidFallSquareChange] Changed square. ItemID={GetInstanceID()} OldSquare={square} NewSquare={squareNew}");

//             square = squareNew;
//             waypoints.Add(new Waypoint(squareNew.transform.position + Vector3.back * 0.2f, squareNew));
//             // Recalculate distance if needed
//             destPos = waypoint.destPosition + Vector3.back * 0.2f;
//             float distance = Vector2.Distance(startPos, destPos);
//             Log($"[FallingScript][AttemptMidFallSquareChange] Added new waypoint. distance={distance}");
//         }
//     }

//     #endregion

//     #region Post-Fall Handling

//     /// <summary>
//     /// Handles all logic after the item finishes falling:
//     /// - Playing sounds
//     /// - Checking matches and destroying items
//     /// - Applying post-fall effects (e.g. squash)
//     /// - Resetting animations
//     /// </summary>
//     private IEnumerator HandlePostFall(bool animate, Action callback)
//     {
//         JustCreatedItem = false;
//         if (previousSquare?.Item == this) previousSquare.Item = null;

//         Vector3 destPos = square.transform.position + Vector3.back * 0.2f;
//         float distanceFromEnd = Vector2.Distance(transform.position, destPos);

//         // Play drop sound if we moved a significant distance
//         if (distanceFromEnd > 0.5f && animate)
//         {
//             SoundBase.Instance?.PlayOneShot(SoundBase.Instance.drop[UnityEngine.Random.Range(0, SoundBase.Instance.drop.Length)]);
//         }

//         fallingID = 0;
//         StopFallFinished();

//         // Wait until falling fully stops
//         yield return new WaitWhile(() => falling);
//         yield return new WaitForSeconds(LevelManager.THIS.waitAfterFall);

//         // Align to final position
//         transform.position = destPos;
//         CheckSquareBelow();

//         // Invoke callback if provided
//         callback?.Invoke();

//         // If no further falling needed, apply post-fall effects
//         if (!needFall)
//         {
//             ResetAnimTransform();

//             // Small delay to ensure everything settled
//             yield return new WaitForSeconds(0.2f);

//             // Check for matches and destroy items
//             yield return HandleMatchesAndDestroy();

//             // If it's a normal item, apply squash effect (new feature)
//             if (currentType == ItemsTypes.NONE)
//             {
//                 ApplyDeformationEffect(); // Post-fall squash effect
//                 Log($"[FallingScript][HandlePostFall] Deformation effect applied. ItemID={GetInstanceID()}");
//             }

//             OnStopFall();
//         }
//     }

//     /// <summary>
//     /// Finds and destroys matched items. Integrate improved destruction logic here.
//     /// </summary>
//     private IEnumerator HandleMatchesAndDestroy()
//     {
//         // Check if any neighbors are still falling with the same color.
//         // If none, proceed to find and handle matches.
//         bool anyFallingNeighborSameColor = square.GetAllNeghborsCross().Any(i => i.Item && i.Item.falling && i.Item.color == color);
//         if (anyFallingNeighborSameColor)
//         {
//             yield break; // Wait for them to settle
//         }

//         // Retrieve matches
//         var combines = GetMatchesAround();
//         Log($"[FallingScript][HandleMatchesAndDestroy] Found {combines.Count} combines. ItemID={GetInstanceID()}");

//         // If Script 2 introduced a more advanced destruction logic, apply it here.
//         // For demonstration, if more than 3 items in total, use a DestroyGroup, else destroy individually.
//         var allMatchedItems = combines.SelectMany(c => c.items).Distinct().ToList();

//         foreach (var combine in combines)
//         {
//             if (combine.nextType != ItemsTypes.NONE)
//             {
//                 var firstItem = combine.items.FirstOrDefault();
//                 if (firstItem != null && firstItem.NextType == ItemsTypes.NONE)
//                 {
//                     firstItem.NextType = combine.nextType;
//                 }
//             }
//         }

//         if (allMatchedItems.Count > 3)
//         {
//             // Use a DestroyGroup approach if available
//             DestroyGroup destroyGroup = new DestroyGroup(allMatchedItems);
//             yield return destroyGroup.DestroyGroupCor(showScore: false, particles: true, explEffect: false);
//             Log($"[FallingScript][HandleMatchesAndDestroy] DestroyGroup used. ItemID={GetInstanceID()} ItemsCount={allMatchedItems.Count}");
//         }
//         else
//         {
//             // Destroy items individually
//             allMatchedItems.ForEach(x => x?.DestroyItem(false));
//             Log($"[FallingScript][HandleMatchesAndDestroy] Individual destruction. ItemID={GetInstanceID()} ItemsCount={allMatchedItems.Count}");
//         }

//         // If square is TargetType2 or special type, inform level manager
//         if (square != null && square.type == SquareTypes.TargetType2)
//         {
//             LevelManager.THIS.levelData.GetTargetObject().CheckSquares(allMatchedItems.Select(i => i.square).ToArray());
//         }
//     }

//     #endregion

//     #region Utility and Event Methods

//     /// <summary>
//     /// Teleportation effect (original logic retained).
//     /// </summary>
//     private void TeleportationEffect(Square sq)
//     {
//         // Original teleportation logic here
//     }

//     /// <summary>
//     /// Checks the square below (original logic).
//     /// </summary>
//     private void CheckSquareBelow()
//     {
//         // Original logic
//     }

//     /// <summary>
//     /// Called when falling is finished (original logic).
//     /// </summary>
//     private void StopFallFinished()
//     {
//         // Original logic
//     }

//     /// <summary>
//     /// Resets the animation transform (original logic).
//     /// </summary>
//     private void ResetAnimTransform()
//     {
//         anim.SetTrigger("stop");
//         // Original logic for resetting any transform modifications
//     }

//     /// <summary>
//     /// Called when fall stops (original logic).
//     /// </summary>
//     private void OnStopFall()
//     {
//         // Original logic
//     }

//     /// <summary>
//     /// Retrieves matches around the current item (original logic).
//     /// </summary>
//     private List<Combine> GetMatchesAround()
//     {
//         // Original logic to find matches
//         return new List<Combine>();
//     }

//     /// <summary>
//     /// Applies a deformation effect after the item lands (new feature integrated from Script 2).
//     /// </summary>
//     private void ApplyDeformationEffect()
//     {
//         // Implement the squash/stretch or any visual effect here
//         // Example: anim.SetTrigger("squash");
//     }

//     #endregion

//     #region Logging

//     /// <summary>
//     /// Logs a message if logging is enabled.
//     /// Prefixes all logs with [FallingScript] and includes ItemID.
//     /// </summary>
//     private void Log(string message)
//     {
//         if (!isLoggingEnabled) return;
//         Debug.Log(message);
//     }

//     #endregion
// }

// // Placeholder classes to make the script self-contained.
// // In your project, these would be defined elsewhere.
// public enum ItemsTypes { NONE, TYPE1, TYPE2 }
// public enum GameState { PreWinAnimations, Playing }
// public class LevelManager
// {
//     public static LevelManager THIS = new LevelManager();
//     public bool StopFall;
//     public float waitAfterFall = 0.2f;
//     public GameState gameStatus = GameState.Playing;
//     public AnimationCurve fallingCurve = new AnimationCurve();
//     public DebugSettingsClass DebugSettings = new DebugSettingsClass();
//     public LevelData levelData = new LevelData();

//     public void FindMatches() { }

//     public class DebugSettingsClass
//     {
//         public bool FallingLog = false;
//     }
// }
// public class LevelData
// {
//     public TargetObject GetTargetObject() { return new TargetObject(); }
// }
// public class TargetObject
// {
//     public void CheckSquares(Square[] squares) { }
// }
// public class SoundBase
// {
//     public static SoundBase Instance = new SoundBase();
//     public AudioClip[] drop = new AudioClip[1];
//     public void PlayOneShot(AudioClip clip) { }
// }
// public class Field
// {
//     public float squareHeight = 1.0f;
// }
// public class Square
// {
//     public Square teleportDestination;
//     public Square teleportOrigin;
//     public SquareTypes type;
//     public Transform transform;
//     public Item Item;
//     public Vector2 direction = Vector2.down;

//     public Square GetNextSquare(bool param = false) { return null; }
//     public bool IsFree() { return true; }
//     public bool GetAllNeghborsCross() { return false; }
//     public Vector3 GetReverseDirection() { return Vector3.up; }
// }
// public enum SquareTypes { TargetType2, Normal }
// public class Waypoint
// {
//     public Vector3 destPosition;
//     public Square square;
//     public bool instant;

//     public Waypoint(Vector3 destPos, Square sq)
//     {
//         destPosition = destPos;
//         square = sq;
//     }
// }
// public class Item : MonoBehaviour
// {
//     public bool falling;
//     public ItemsTypes NextType;
//     public Color color;
//     public void DestroyItem(bool destroyNeighbours = false) { }
// }
// public class DestroyGroup
// {
//     private List<Item> items;
//     public DestroyGroup(List<Item> its) { items = its; }

//     public IEnumerator DestroyGroupCor(bool showScore, bool particles, bool explEffect)
//     {
//         // Mock destruction routine
//         foreach (var item in items) item?.DestroyItem(false);
//         yield break;
//     }
// }
// public class Combine
// {
//     public ItemsTypes nextType;
//     public List<Item> items = new List<Item>();
// }
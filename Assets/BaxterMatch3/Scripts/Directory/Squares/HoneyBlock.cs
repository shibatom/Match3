using UnityEngine;
using UnityEngine.Tilemaps;

namespace Internal.Scripts.Blocks
{
    public class HoneyBlock : Rectangle
    {
        #region Fields and Properties

        [Header("Honey Block Settings")] public Tilemap blockTilemap;
        public TilemapRenderer tilemapRenderer;

        [Header("Solo")] [SerializeField] private bool solo01;

        [Header("Tile Types")] [SerializeField]
        private TileBase inside_fill0; // For interior tiles when surrounded

        [SerializeField] private TileBase corner01; // For corners with no adjacent neighbors
        [SerializeField] private TileBase corner_side01; // For tiles connecting to neighbors

        // Unused tiles (kept for future expansion)
        [SerializeField] private TileBase corner02;
        [SerializeField] private TileBase corner03;
        [SerializeField] private TileBase corner04;
        [SerializeField] private TileBase corner05;
        [SerializeField] private TileBase corner_side02;
        [SerializeField] private TileBase inside_corner01;
        [SerializeField] private TileBase inside_corner02;
        [SerializeField] private TileBase inside_corner_short;
        [SerializeField] private TileBase side01;
        [SerializeField] private TileBase side02;
        [SerializeField] private TileBase side03;
        [SerializeField] private TileBase side04;

        // Neighbor states (orthogonal and diagonal)
        private bool neighborLeft;
        private bool neighborRight;
        private bool neighborTop;
        private bool neighborBottom;
        private bool neighborTopRight;
        private bool neighborTopLeft;
        private bool neighborBottomLeft;
        private bool neighborBottomRight;

        // Quadrant positions and default rotations
        private readonly Vector3Int[] quadrantPositions = new[]
        {
            new Vector3Int(1, 1, 0), // Top-Right (TR)
            new Vector3Int(0, 1, 0), // Top-Left (TL)
            new Vector3Int(0, 0, 0), // Bottom-Left (BL)
            new Vector3Int(1, 0, 0)  // Bottom-Right (BR)
        };

        // Adjusted default rotations based on base tile facing left (0°)
        private readonly float[] defaultRotations = { 270, 0, 90, 180 }; // TR, TL, BL, BR

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            gameObject.name = "HoneyBlock." + GetInstanceID();
            InitializeTile();
            //tilemapRenderer = blockTilemap.GetComponent<TilemapRenderer>();
        }

        public void InitializeTile()
        {
            //Debug.LogError("InitializeTile getting called");
            UpdateNeighborStates();
            UpdateBlockTiles();
        }

        #endregion

        #region Neighbor Management

        /// <summary>
        /// Updates the neighbor state flags for all eight directions.
        /// </summary>
        private void UpdateNeighborStates()
        {
            var neighbors = GetNeighbors();
            neighborLeft = IsHoneyBlock(neighbors.left);
            neighborRight = IsHoneyBlock(neighbors.right);
            neighborTop = IsHoneyBlock(neighbors.top);
            neighborBottom = IsHoneyBlock(neighbors.bottom);
            neighborTopRight = IsHoneyBlock(neighbors.topRight);
            neighborTopLeft = IsHoneyBlock(neighbors.topLeft);
            neighborBottomLeft = IsHoneyBlock(neighbors.bottomLeft);
            neighborBottomRight = IsHoneyBlock(neighbors.bottomRight);
        }

        /// <summary>
        /// Retrieves all eight neighboring squares (orthogonal and diagonal).
        /// </summary>
        private (Rectangle left, Rectangle right, Rectangle top, Rectangle bottom,
            Rectangle topRight, Rectangle topLeft, Rectangle bottomLeft, Rectangle bottomRight) GetNeighbors()
        {
            var parent = transform.parent.GetComponent<Rectangle>();
            var top = parent.GetNeighborTop();
            var bottom = parent.GetNeighborBottom();

            return (
                parent.GetNeighborLeft(),
                parent.GetNeighborRight(),
                top,
                bottom,
                top?.GetNeighborRight(),
                top?.GetNeighborLeft(),
                bottom?.GetNeighborLeft(),
                bottom?.GetNeighborRight()
            );
        }

        /// <summary>
        /// Checks if a neighbor is a HoneyBlock.
        /// </summary>
        private bool IsHoneyBlock(Rectangle neighbor) =>
            neighbor != null && neighbor.type == LevelTargetTypes.HoneyBlock;

        #endregion

        #region Tile Management

        /// <summary>
        /// Updates the 2x2 grid of tiles based on neighboring HoneyBlocks.
        /// </summary>
        public void UpdateBlockTiles()
        {
            TileBase[] selectedTiles = SelectTilesBasedOnNeighbors();
            ApplyTilesToQuadrants(selectedTiles);
        }

        /// <summary>
        /// Selects tiles for the 2x2 grid based on all eight neighbors.
        /// </summary>
        private TileBase[] SelectTilesBasedOnNeighbors()
        {
            TileBase tr = DetermineTileForQuadrant(quadrantPositions[0], neighborTop, neighborRight, neighborTopRight);
            TileBase tl = DetermineTileForQuadrant(quadrantPositions[1], neighborTop, neighborLeft, neighborTopLeft);
            TileBase bl = DetermineTileForQuadrant(quadrantPositions[2], neighborBottom, neighborLeft, neighborBottomLeft);
            TileBase br = DetermineTileForQuadrant(quadrantPositions[3], neighborBottom, neighborRight, neighborBottomRight);
            return CreateTilePattern(tr, tl, bl, br);
        }

        /// <summary>
        /// Determines the tile type for a quadrant based on its adjacent neighbors.
        /// </summary>
        private TileBase DetermineTileForQuadrant(Vector3Int position, bool ortho1, bool ortho2, bool diagonal)
        {
            bool hasOrthoNeighbor1 = ortho1; // e.g., Top for TR, TL
            bool hasOrthoNeighbor2 = ortho2; // e.g., Right for TR, BR
            bool hasDiagonalNeighbor = diagonal;

            Debug.LogWarning($"Honey: Checking tile at {position} - Ortho1: {hasOrthoNeighbor1}, Ortho2: {hasOrthoNeighbor2}, Diagonal: {hasDiagonalNeighbor}");

            TileBase result;
            // Use inside_fill0 if both orthogonal neighbors are present (ignore diagonal)
            if (hasOrthoNeighbor1 && hasOrthoNeighbor2 && hasDiagonalNeighbor)
            {
                Debug.LogWarning($"Honey: Using inside_fill0 at {position} due to both orthogonal neighbors");
                result = inside_fill0;
            }
            // If no orthogonal neighbors, use corner01
            else if (!hasOrthoNeighbor1 && !hasOrthoNeighbor2 && !hasDiagonalNeighbor)
            {
                Debug.LogWarning($"Honey: Using corner01 at {position} due to no neighbors");
                result = corner01;
            }
            // If any neighbor (orthogonal or diagonal), use corner_side01
            else if (hasOrthoNeighbor1 && hasOrthoNeighbor2 && !hasDiagonalNeighbor)
            {
                tilemapRenderer.sortingOrder = 6;
                Debug.LogWarning($"Honey: Using  inside_corner01 at {position} due to at least one neighbor");
                result = inside_corner01;
            }

            // If any neighbor (orthogonal or diagonal), use corner_side01
            else if ((hasOrthoNeighbor1 || hasOrthoNeighbor2))
            {
                Debug.LogWarning($"Honey: Using corner_side01 at {position} due to at least one neighbor");
                result = corner_side01;
            }


            else
            {
                Debug.LogWarning($"Honey: Fallback to corner01 at {position}");
                result = corner01;
            }

            Debug.LogWarning($"Honey: Returning TileBase {result?.name ?? "null"} for position {position}");
            return result;
        }

        /// <summary>
        /// Applies tiles to the 2x2 grid with appropriate rotations.
        /// </summary>
        private void ApplyTilesToQuadrants(TileBase[] tiles)
        {
            for (int i = 0; i < 4; i++)
            {
                SetTileWithRotation(quadrantPositions[i], tiles[i], defaultRotations[i]);
            }
        }

        /// <summary>
        /// Sets a tile at a position and applies its calculated rotation.
        /// </summary>
        private void SetTileWithRotation(Vector3Int position, TileBase tile, float initialRotation)
        {
            if (tile == null) tile = corner01; // Fallback to avoid null tiles
            blockTilemap.SetTile(position, tile);
            float finalRotation = CalculateFinalRotation(tile, position, initialRotation);
            ApplyRotation(position, finalRotation);
        }

        /// <summary>
        /// Calculates the final rotation based on tile type and neighbors.
        /// Base tile faces left at 0°; adjusts to point toward neighbors.
        /// </summary>
        private float CalculateFinalRotation(TileBase tile, Vector3Int position, float initialRotation)
        {
            Debug.LogWarning($"Honey: Calculating rotation for tile: {tile.name} at position: {position}");

            if (tile == corner01)
            {
                Debug.LogWarning($"Honey: Corner01 tile using default rotation: {initialRotation}");
                return initialRotation; // Corner tiles use default rotation
            }

            if (tile == corner_side01)
            {
                Debug.LogWarning($"Honey: Processing corner_side01 tile at {position}");

                if (position == quadrantPositions[0]) // TR (1,1)
                {
                    Debug.LogWarning($"Honey: TR quadrant - Neighbors: Right={neighborRight}, Top={neighborTop}, TopRight={neighborTopRight}");
                    if (neighborRight) return 0f; // Face right
                    if (neighborTop) return 270f; // Face up
                    if (neighborTopRight) return 0f; // Face right (orthogonal preference)
                    return 90f; // Default to up if no neighbor
                }
                else if (position == quadrantPositions[1]) // TL (0,1)
                {
                    Debug.LogWarning($"Honey: TL quadrant - Neighbors: Left={neighborLeft}, Top={neighborTop}, TopLeft={neighborTopLeft}");
                    if (neighborLeft) return 0f; // Face left
                    if (neighborTop) return 90f; // Face up
                    if (neighborTopLeft) return 180f; // Face left (orthogonal preference)
                    return 90f; // Default to up if no neighbor
                }
                else if (position == quadrantPositions[2]) // BL (0,0)
                {
                    Debug.LogWarning($"Honey: BL quadrant - Neighbors: Left={neighborLeft}, Bottom={neighborBottom}, BottomLeft={neighborBottomLeft}");
                    if (neighborLeft) return 180f; // Face left
                    if (neighborBottom) return 90f; // Face down
                    if (neighborBottomLeft) return 180f; // Face left (orthogonal preference)
                    return 180f; // Default to left if no neighbor
                }
                else if (position == quadrantPositions[3]) // BR (1,0)
                {
                    Debug.LogWarning($"Honey: BR quadrant - Neighbors: Right={neighborRight}, Bottom={neighborBottom}, BottomRight={neighborBottomRight}");
                    if (neighborRight) return 180f; // Face right
                    if (neighborBottom) return 270f; // Face down
                    if (neighborBottomRight) return 180f; // Face right (orthogonal preference)
                    return 270f; // Default to down if no neighbor
                }
            }

            Debug.LogWarning($"Honey: Using default rotation {initialRotation} for tile {tile.name}");
            return initialRotation; // Default for inside_fill0 or unrecognized tiles
        }

        /// <summary>
        /// Applies a rotation to a tile at the specified position.
        /// </summary>
        private void ApplyRotation(Vector3Int position, float rotation)
        {
            var transformMatrix = Matrix4x4.Rotate(Quaternion.Euler(0f, 0f, rotation));
            blockTilemap.SetTransformMatrix(position, transformMatrix);
        }

        /// <summary>
        /// Creates a tile pattern array in the order: TR, TL, BL, BR.
        /// </summary>
        private TileBase[] CreateTilePattern(TileBase tr, TileBase tl, TileBase bl, TileBase br) =>
            new[] { tr, tl, bl, br };

        #endregion
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DrawOuterGraph
{
    public class OuterGraph : MonoBehaviour
    {
        private List<List<Vector3>> _squares;
        private HashSet<KeyValuePair<Vector3, Vector3>> _edges;
        private List<Vector3> _coordsList;

        [SerializeField]
        private float edgeDistance = 1f;
        private const float Tolerance = 0.001f;

        // LineRenderer field � assign in the inspector or it will be added at runtime.
        [SerializeField]
        private LineRenderer lineRenderer;

        private void Start()
        {
            Debug.unityLogger.logEnabled = true;
            _squares = new List<List<Vector3>>();
            SetupLineRenderer();
            SetSquaresExample();
        }

        /// <summary>
        /// If a LineRenderer isn�t already assigned, add one and set some default properties.
        /// </summary>
        private void SetupLineRenderer()
        {
            if (lineRenderer == null)
            {
                lineRenderer = gameObject.AddComponent<LineRenderer>();
                // Using a basic sprite shader; you can change the shader or material as needed.
                lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
                lineRenderer.widthMultiplier = 0.1f;
                lineRenderer.positionCount = 0;
                lineRenderer.loop = false; // Set to true if you want the line to close the shape.
                lineRenderer.startColor = Color.cyan;
                lineRenderer.endColor = Color.cyan;
            }
        }

        public void SetSquaresExample()
        {
            List<SquareData> squares = new List<SquareData>()
            {
                new SquareData(new Vector3(0.5f, 0.5f, 0f), 1f),
                new SquareData(new Vector3(1.5f, 0.5f, 0f), 1f),
                new SquareData(new Vector3(0.5f, 1.5f, 0f), 1f),
                new SquareData(new Vector3(1.5f, 1.5f, 0f), 1f)
            };
            SetSquares(squares);
        }

        public void SetSquares(List<SquareData> squares)
        {
            if (squares == null || squares.Count == 0)
            {
                Debug.LogWarning("SetSquares(): Empty or null list!");
                return;
            }

            _squares = squares.Select(s => GenerateSquareCorners(s.Center, s.Size)).ToList();
            print($"_squares.Count: {_squares.Count}");
            ComputeEdges();
            BuildList();
            // Update the LineRenderer positions based on the computed ordered polygon.
            UpdateLineRenderer();
        }

        public struct SquareData
        {
            public Vector3 Center;
            public float Size;

            public SquareData(Vector3 center, float size)
            {
                Center = center;
                Size = size;
            }
        }

        private List<Vector3> GenerateSquareCorners(Vector3 center, float size)
        {
            float halfSize = size / 2f;
            return new List<Vector3>
            {
                RoundVector(center + new Vector3(-halfSize, -halfSize, 0)),
                RoundVector(center + new Vector3(halfSize, -halfSize, 0)),
                RoundVector(center + new Vector3(halfSize, halfSize, 0)),
                RoundVector(center + new Vector3(-halfSize, halfSize, 0))
            };
        }

        private Vector3 RoundVector(Vector3 v)
        {
            return new Vector3(
                (float)Math.Round(v.x, 3),
                (float)Math.Round(v.y, 3),
                (float)Math.Round(v.z, 3)
            );
        }

        private void ComputeEdges()
        {
            _edges = new HashSet<KeyValuePair<Vector3, Vector3>>(new EdgeComparer());
            var allEdges = new List<KeyValuePair<Vector3, Vector3>>();

            foreach (var square in _squares)
            {
                for (int i = 0; i < square.Count; i++)
                {
                    int next = (i + 1) % square.Count;
                    Vector3 src = square[i];
                    Vector3 dest = square[next];

                    if (Vector3.Distance(src, dest) > edgeDistance + Tolerance)
                        continue;

                    var edge = MakeEdge(src, dest);
                    allEdges.Add(edge);
                }
            }

            var duplicates = allEdges.GroupBy(e => e, new EdgeComparer())
                                       .Where(g => g.Count() > 1)
                                       .Select(g => g.Key)
                                       .ToList();

            foreach (var edge in allEdges)
            {
                if (!duplicates.Contains(edge, new EdgeComparer()))
                {
                    _edges.Add(edge);
                }
            }
        }

        private KeyValuePair<Vector3, Vector3> MakeEdge(Vector3 a, Vector3 b)
        {
            return IsVectorLessThan(a, b)
                ? new KeyValuePair<Vector3, Vector3>(a, b)
                : new KeyValuePair<Vector3, Vector3>(b, a);
        }

        private bool IsVectorLessThan(Vector3 a, Vector3 b)
        {
            if (a.x != b.x) return a.x < b.x;
            if (a.y != b.y) return a.y < b.y;
            return a.z < b.z;
        }

        private void BuildList()
        {
            _coordsList = new List<Vector3>();
            if (_edges.Count == 0)
                return;

            var edgeList = new List<KeyValuePair<Vector3, Vector3>>(_edges);
            _coordsList.Add(edgeList[0].Key);
            _coordsList.Add(edgeList[0].Value);
            edgeList.RemoveAt(0);

            while (edgeList.Count > 0)
            {
                Vector3 last = _coordsList.Last();
                int foundIndex = edgeList.FindIndex(e => ApproxEquals(e.Key, last) || ApproxEquals(e.Value, last));
                if (foundIndex == -1)
                    break;

                var edge = edgeList[foundIndex];
                Vector3 next = ApproxEquals(edge.Key, last) ? edge.Value : edge.Key;
                _coordsList.Add(next);
                edgeList.RemoveAt(foundIndex);
            }
        }

        private bool ApproxEquals(Vector3 a, Vector3 b)
        {
            return Vector3.Distance(a, b) < Tolerance;
        }

        /// <summary>
        /// Updates the LineRenderer's positions to match the ordered polygon.
        /// </summary>
        private void UpdateLineRenderer()
        {
            if (lineRenderer != null && _coordsList != null && _coordsList.Count > 0)
            {
                lineRenderer.positionCount = _coordsList.Count;
                lineRenderer.SetPositions(_coordsList.ToArray());
            }
        }

        private void Update()
        {
            // These Debug.DrawLine calls help visualize the edges and polygon in the Scene view.
            // They are optional if you're using the LineRenderer for visualization.

            // Draw outer edges in green
            if (_edges != null)
            {
                foreach (var edge in _edges)
                {
                    Debug.DrawLine(edge.Key, edge.Value, Color.green, Time.deltaTime);
                }
            }

            // Draw ordered polygon in cyan
            if (_coordsList != null && _coordsList.Count > 1)
            {
                for (int i = 1; i < _coordsList.Count; i++)
                {
                    Debug.DrawLine(_coordsList[i - 1], _coordsList[i], Color.cyan, Time.deltaTime);
                }
            }
        }
    }

    public class EdgeComparer : IEqualityComparer<KeyValuePair<Vector3, Vector3>>
    {
        private const float Tolerance = 0.001f;

        public bool Equals(KeyValuePair<Vector3, Vector3> x, KeyValuePair<Vector3, Vector3> y)
        {
            return (ApproxEquals(x.Key, y.Key) && ApproxEquals(x.Value, y.Value)) ||
                   (ApproxEquals(x.Key, y.Value) && ApproxEquals(x.Value, y.Key));
        }

        public int GetHashCode(KeyValuePair<Vector3, Vector3> edge)
        {
            var ordered = OrderEdge(edge.Key, edge.Value);
            return ordered.Key.GetHashCode() ^ ordered.Value.GetHashCode();
        }

        private bool ApproxEquals(Vector3 a, Vector3 b)
        {
            return Vector3.Distance(a, b) < Tolerance;
        }

        private KeyValuePair<Vector3, Vector3> OrderEdge(Vector3 a, Vector3 b)
        {
            return a.x < b.x || (a.x == b.x && (a.y < b.y || (a.y == b.y && a.z < b.z)))
                ? new KeyValuePair<Vector3, Vector3>(a, b)
                : new KeyValuePair<Vector3, Vector3>(b, a);
        }
    }
}

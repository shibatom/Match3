using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using System;

namespace Internal.Scripts.Items
{
    public class SquareBoundaryLine : MonoBehaviour
    {
        public LineRenderer lineRenderer;
        public float fadeDuration = 0.3f; // Duration of the fade effect

        private Material lineMaterial;

        // Initialize the LineRenderer when needed
        void Awake()
        {
            // Debug.Log("SquareBoundaryLine: Awake() called.");
            // // Create the LineRenderer component
            // lineRenderer = gameObject.AddComponent<LineRenderer>();

            // // Set basic properties
            // lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            // lineRenderer.widthMultiplier = 0.1f;

            // // Customize appearance
            // lineRenderer.startColor = Color.red;
            // lineRenderer.endColor = Color.red;

            // // Set the initial number of points to 0
            // lineRenderer.positionCount = 0;

            Debug.Log("SquareBoundaryLine: LineRenderer initialized.");
            lineMaterial = lineRenderer.material;
        }
        //    void Start()
        // {
        //     // Example squares to highlight
        //     Square[] squares = new Square[]
        //     {
        //         new Square(new Vector3(0, 0, 0), 1f),   // Square 1
        //         new Square(new Vector3(1, 0, 0), 1f),   // Square 2
        //         new Square(new Vector3(1, 1, 0), 1f),   // Square 3
        //         new Square(new Vector3(0, 1, 0), 1f)    // Square 4
        //     };

        //     // Draw the outline around the squares
        //     DrawOutline(squares);
        // }

        // Public method to set points of the line from another script
        private Coroutine fadeincoroutine;

        public void ClearLineInstantly()
        {
            StopAnyActiveFade();
            lineRenderer.positionCount = 0;
            if (lineMaterial != null) lineMaterial.SetFloat("_Alpha", 0f);
        }

        private void StopAnyActiveFade()
        {
            if (fadeincoroutine != null)
            {
                StopCoroutine(fadeincoroutine);
                fadeincoroutine = null;
            }

            StartCoroutine(FadeOutAndClear(0));

            StartCoroutine(ForSureDeActive());
        }

        private IEnumerator ForSureDeActive()
        {
            yield return new WaitForSeconds(1);
            if (fadeincoroutine != null)
            {
                StopCoroutine(fadeincoroutine);
                fadeincoroutine = null;
            }

            StartCoroutine(FadeOutAndClear(0));
        }

        public void SetPoints(Vector3[] points)
        {
            fadeincoroutine = StartCoroutine(FadeIn());
            // Debug.Log($"SquareBoundaryLine: SetPoints() called with {points.Length} points.");
            // Set the number of points
            lineRenderer.positionCount = points.Length;

            // Assign the points to the LineRenderer
            for (int i = 0; i < points.Length; i++)
            {
                lineRenderer.SetPosition(i, points[i]);
            }
            // ClearLine(1.7f);
        }

        // Optional: Clear the line
        public void ClearLine(float fadeDuration = 0.5f)
        {
            // Debug.Log("SquareBoundaryLine: ClearLine() called.");
            if (fadeincoroutine != null)
                StopCoroutine(fadeincoroutine);
            StartCoroutine(FadeOutAndClear(fadeDuration));
        }


        public IEnumerator FadeOutAndClear(float fadeDuration = 0.5f, bool clearAfterFade = true)
        {
            //yield return new WaitForSeconds(waitforTime);
            float startAlpha = lineMaterial.GetFloat("_Alpha");
            float elapsedTime = 0f;

            while (elapsedTime < fadeDuration)
            {
                elapsedTime += Time.deltaTime;
                float newAlpha = Mathf.Lerp(startAlpha, -1f, elapsedTime / fadeDuration);
                lineMaterial.SetFloat("_Alpha", newAlpha);
                yield return null;
            }

            if (clearAfterFade && lineRenderer.positionCount != 0)
                lineRenderer.positionCount = 0; // Clear the line after fading out
            lineMaterial.SetFloat("_Alpha", 0);
        }

        private IEnumerator FadeIn()
        {
            float elapsedTime = 0f;
            float startAlpha = -1f; // Start from -1
            float targetAlpha = 0f;

            while (elapsedTime <= fadeDuration) // Use <= to ensure full transition
            {
                elapsedTime += Time.deltaTime; // Update time first
                float t = Mathf.Clamp01(elapsedTime / fadeDuration); // Ensure t is between 0 and 1
                float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                lineMaterial.SetFloat("_Alpha", newAlpha);

                yield return null;
            }

            lineMaterial.SetFloat("_Alpha", targetAlpha); // Ensure final value is exactly 0
            yield return new WaitForSeconds(1.7f);
            ClearLine();
        }


        private LineRendererSetup lineRendererSetup;

        // Square structure, defined by its center and size
        public struct Square
        {
            public Vector3 center;
            public float size;

            public Square(Vector2 center, float size)
            {
                this.center = center;
                this.size = size;
                Debug.Log($"Square: Created with center {center} and size {size}");
            }
        }

        // Method to draw a closed outline around squares
        private List<List<Vector3>> _squares;
        private HashSet<KeyValuePair<Vector3, Vector3>> _edges;
        private List<Vector3> _coordsList;

        [SerializeField] private float edgeDistance = 1f;
        private const float Tolerance = 0.001f;


        private void Start()
        {
            _squares = new List<List<Vector3>>();
        }

        /// <summary>
        /// If a LineRenderer isn�t already assigned, add one and set some default properties.
        /// </summary>
        public void SetSquares(List<SquareData> squares)
        {
            if (squares == null || squares.Count == 0)
            {
                Debug.LogWarning("SetSquares(): Empty or null list!");
                return;
            }

            _squares = squares.Select(s => GenerateSquareCorners(s.Center, s.Size)).ToList();
            // print($"_squares.Count: {_squares.Count}");
            ComputeEdges();
            BuildList();
            // Update the LineRenderer positions based on the computed ordered polygon.
            //UpdateLineRenderer();
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

            SetPoints(_coordsList.ToArray());
        }

        private bool ApproxEquals(Vector3 a, Vector3 b)
        {
            return Vector3.Distance(a, b) < Tolerance;
        }

        /// <summary>
        /// Updates the LineRenderer's positions to match the ordered polygon.
        /// </summary>
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
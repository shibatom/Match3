
// using System;
// using System.Collections.Generic;
// using System.Linq;
// using UnityEngine;

// namespace DrawOuterGraph
// {
//     public class OuterGraph: MonoBehaviour
//     {
//         private List<List<Vector3>> _squares;

//         private HashSet<KeyValuePair<Vector3, Vector3>> _edges;

//         private List<Vector3> _coordsList;

//         [SerializeField]
//         private float edgeDistance = 1;

//         private void Start()
//         {
//             CreateCoords();
//             ComputeEdges();
//             BuildList();
//         }

//         private void ComputeEdges()
//         {
//             // The edges collection is a set of pair of vertex
//             _edges = new HashSet<KeyValuePair<Vector3, Vector3>>();
//             foreach (var square in _squares)
//             {
//                 // Iterate over the coordinates to compute the edges
//                 // Using for loop to skip already processed edges
//                 var squareCount = square.Count;
//                 for (var i = 0; i < squareCount; i++)
//                 {
//                     // The source vertex
//                     var src = square[i];

//                     for (var j = 0; j < squareCount; j++)
//                     {
//                         if (i == j) continue;
//                         // The vertex with whom we want to determine if they form and edge
//                         var dest = square[j];

//                         // Check the distance between them to filter out the diagonal edges
//                         if (!(Math.Abs(Vector3.Distance(src, dest) - edgeDistance) < 0.001)) continue;

//                         var edge = new KeyValuePair<Vector3, Vector3>(src, dest);

//                         // _edges is a set, making it viable to use Contains
//                         // even when the collections contains a lot of elements
//                         if (_edges.Contains(edge))
//                         {
//                             // If the edge already exists in the set,
//                             // it means its not part of the border
//                             _edges.Remove(edge);
//                         }
//                         else
//                         {
//                             _edges.Add(edge);
//                         }
//                     }
//                 }
//             }
//         }

//         public void BuildList()
//         {
//             _coordsList = new List<Vector3>();

//             // Make a copy of the edges so we can remove items from it
//             // without destroying the original collection
//             var copy = new HashSet<KeyValuePair<Vector3, Vector3>>(_edges);

//             // Add the first pair before starting the loop
//             var previousEdge = _edges.First();

//             _coordsList.Add(previousEdge.Key);
//             _coordsList.Add(previousEdge.Value);

//             KeyValuePair<Vector3, Vector3> currentEdge;

//             // While there is an edge that follows the previous one
//             while (!(currentEdge = copy.FirstOrDefault(pair => pair.Key == previousEdge.Value))
//                    .Equals(default(KeyValuePair<Vector3, Vector3>)))
//             {
//                 // Our graph is not oriented but we want to ignores edges
//                 // that go back from where we went
//                 if (currentEdge.GetHashCode() == previousEdge.GetHashCode())
//                 {
//                     copy.Remove(currentEdge);
//                     continue;
//                 }

//                 // Add the vertex to the list and continue
//                 _coordsList.Add(currentEdge.Value);
//                 previousEdge = currentEdge;

//                 // Remove traversed nodes
//                 copy.Remove(currentEdge);
//             }
//         }

//         public void CreateCoords()
//         {
//             _squares = new List<List<Vector3>>()
//             {
//                 new List<Vector3>()
//                 {
//                     new Vector3(0,0,1),
//                     new Vector3(1,0,0),
//                     new Vector3(0,0,0),
//                     new Vector3(1,0,1),
//                 },
//                 new List<Vector3>()
//                 {
//                     new Vector3(0,0,1),
//                     new Vector3(1,0,1),
//                     new Vector3(1,0,2),
//                     new Vector3(0,0,2),
//                 },
//                 new List<Vector3>()
//                 {
//                     new Vector3(1,0,1),
//                     new Vector3(2,0,1),
//                     new Vector3(2,0,2),
//                     new Vector3(1,0,2),
//                 },
//                 new List<Vector3>()
//                 {
//                     new Vector3(2,0,1),
//                     new Vector3(2,0,2),
//                     new Vector3(3,0,1),
//                     new Vector3(3,0,2),
//                 },
//                 new List<Vector3>()
//                 {
//                     new Vector3(2,0,1),
//                     new Vector3(3,0,1),
//                     new Vector3(2,0,0),
//                     new Vector3(3,0,0),
//                 },
//                 new List<Vector3>()
//                 {
//                     new Vector3(4,0,1),
//                     new Vector3(3,0,1),
//                     new Vector3(4,0,0),
//                     new Vector3(3,0,0),
//                 },
//                 new List<Vector3>()
//                 {
//                     new Vector3(1,0,2),
//                     new Vector3(1,0,3),
//                     new Vector3(2,0,2),
//                     new Vector3(2,0,3),
//                 },
//                 new List<Vector3>()
//                 {
//                     new Vector3(3,0,2),
//                     new Vector3(3,0,3),
//                     new Vector3(2,0,2),
//                     new Vector3(2,0,3),
//                 },
//                 new List<Vector3>()
//                 {
//                     new Vector3(3,0,4),
//                     new Vector3(3,0,3),
//                     new Vector3(2,0,4),
//                     new Vector3(2,0,3),
//                 },
//                 new List<Vector3>()
//                 {
//                     new Vector3(1,0,4),
//                     new Vector3(1,0,3),
//                     new Vector3(2,0,4),
//                     new Vector3(2,0,3),
//                 },
//                 new List<Vector3>()
//                 {
//                     new Vector3(3,0,4),
//                     new Vector3(3,0,5),
//                     new Vector3(2,0,4),
//                     new Vector3(2,0,5),
//                 },
//                 new List<Vector3>()
//                 {
//                     new Vector3(3,0,2),
//                     new Vector3(3,0,3),
//                     new Vector3(4,0,2),
//                     new Vector3(4,0,3),
//                 },
//             };
//         }

//         private void OnDrawGizmos()
//         {
//             // Draw using the edges
//             if (_edges is not null)
//             {
//                 foreach (var (src, dest) in _edges)
//                 {
//                     Gizmos.DrawLine(src, dest);
//                 }
//             }

//             // Draw using the ordered list
//             if (_coordsList is null || _coordsList.Count == 0) return;
//             var previous = _coordsList[0];
//             foreach (var current in _coordsList)
//             {
//                 Gizmos.DrawLine(previous, current);
//                 previous = current;
//             }
//         }
//     }
// }

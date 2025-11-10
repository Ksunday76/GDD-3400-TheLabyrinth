using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GDD3400.Labyrinth
{
    [SelectionBase]
    public class TileHandler : MonoBehaviour
    {
        [SerializeField] private Vector2Int _TileBounds = new Vector2Int(4, 4);
        [SerializeField] private Transform _NodeContainer;
        public Transform NodeContainer => _NodeContainer;
        [SerializeField] private List<PathNode> _nodes = new List<PathNode>();

        public void CreateNodeMap(Dictionary<Vector2Int, PathNode> nodeMap)
        {
            int x = 0;
            int y = 0;
            // Look at each node and store it in the node map
            foreach (var node in _nodes)
            {
                // Skip if the node is null
                if (node == null) continue;

                // Get the rounded integer position of the node
                x = Mathf.RoundToInt(node.transform.position.x);
                y = Mathf.RoundToInt(node.transform.position.z);

                // Skip if the node is already in the node map
                // This is important as exit nodes overlap with each other
                // We only want to add the exit nodes once
                if (nodeMap.ContainsKey(new Vector2Int(x, y))) continue;

                // Add the node to the node map
                nodeMap.Add(new Vector2Int(x, y), node);
            }
        }

        #region Gizmos & Inspector Setup

        public void GenerateNodes()
        {
            // Calculate the number of nodes in each direction
            int numNodesX = _TileBounds.x / 2;
            int numNodesY = _TileBounds.y / 2;

            // Calculate the offset to center the nodes around 0,0,0
            float offsetX = -_TileBounds.x / 2.0f;
            float offsetY = -_TileBounds.y / 2.0f;

            // Loop through the grid positions
            for (int x = 0; x <= numNodesX; x++)
            {
                for (int y = 0; y <= numNodesY; y++)
                {
                    // Skip the corner nodes
                    if ((x == 0 || x == numNodesX) && (y == 0 || y == numNodesY))
                    {
                        continue;
                    }

                    // Calculate the position for the node, centered around 0,0,0
                    Vector3 nodePosition = new Vector3((x * 2) + offsetX, 0, (y * 2) + offsetY) + transform.position;

                    // Instantiate a new PathNode at the calculated position
                    GameObject nodeObject = new GameObject("Path Node");
                    nodeObject.transform.position = nodePosition;
                    nodeObject.transform.parent = _NodeContainer;

                    // Add the PathNode component
                    PathNode pathNode = nodeObject.AddComponent<PathNode>();

                    // Setup the exits
                    // If on the edge of the tile, setup an exit
                    if (x == 0)
                    {
                        // Left Exit
                        pathNode.SetupExit(new Vector2Int(1, 0));
                    }
                    if (x == numNodesX)
                    {
                        // Right Exit
                        pathNode.SetupExit(new Vector2Int(-1, 0));
                    }
                    if (y == 0)
                    {
                        // Top Exit
                        pathNode.SetupExit(new Vector2Int(0, 1));
                    }
                    if (y == numNodesY)
                    {
                        // Bottom Exit
                        pathNode.SetupExit(new Vector2Int(0, -1));
                    }

                    // Add the node to the list
                    _nodes.Add(pathNode);
                }
            }
        }

        public void GatherNodes()
        {
            _nodes.Clear(); // Clear the existing list to avoid duplicates

            foreach (Transform child in _NodeContainer)
            {
                PathNode node = child.GetComponent<PathNode>();
                if (node != null)
                {
                    _nodes.Add(node);
                }
            }
        }

        public void ClearNodes()
        {
            _nodes.Clear();
        }

        #endregion
    }

    #if UNITY_EDITOR
    [CustomEditor(typeof(TileHandler))]
    public class TileHandlerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (Application.isPlaying)
            {
                return;
            }

            GUILayout.Space(20);

            TileHandler tileHandler = (TileHandler)target;

            if (GUILayout.Button("Generate Nodes", GUILayout.Height(30))) tileHandler.GenerateNodes();
            GUILayout.Space(10);
            if (GUILayout.Button("Gather Nodes", GUILayout.Height(30))) tileHandler.GatherNodes();
            GUILayout.Space(10);

            if (GUILayout.Button("Clear Nodes", GUILayout.Height(30))) 
            {
                tileHandler.ClearNodes();

                if (tileHandler.NodeContainer.childCount == 0) return;

                for (int i = tileHandler.NodeContainer.childCount - 1; i >= 0; i--)
                {
                    DestroyImmediate(tileHandler.NodeContainer.GetChild(i).gameObject);
                }
            }
        }
    }
    #endif
}

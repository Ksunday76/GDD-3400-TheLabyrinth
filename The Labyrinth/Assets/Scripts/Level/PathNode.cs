using System.Collections.Generic;
using UnityEngine;

namespace GDD3400.Labyrinth
{
    public class PathNode : MonoBehaviour
    {
        [SerializeField] private bool _isExit = false;
        [SerializeField] private Vector2Int _exitDirection = Vector2Int.zero;

        private Dictionary<PathNode, float> _connections = new Dictionary<PathNode, float>();
        public Dictionary<PathNode, float> Connections => _connections;

        private PathNode _exitLink = null;
        public PathNode ExitLink => _exitLink;
        
        private bool DEBUG_SHOW_NODES = true;
        private bool DEBUG_SHOW_NODE_CONNECTIONS = false;

        public void GenerateConnections(Dictionary<Vector2Int, PathNode> nodeMap, float diagonalCost)
        {
            // Initialize the connections
            _connections = new Dictionary<PathNode, float>();

            int rootX = Mathf.RoundToInt(transform.position.x);
            int rootY = Mathf.RoundToInt(transform.position.z);

            float cost = 0;

            // Use two nested for loops to look at each adjacent node and add it to the connections list
            for (int offsetX = -2; offsetX <= 2; offsetX+=2)
            {
                for (int offsetY = -2; offsetY <= 2; offsetY+=2)
                {
                    // Skip if the node is the same as the current node
                    if (offsetX == 0 && offsetY == 0) continue;

                    // Get the adjacent node
                    Vector2Int adjacentPosition = new Vector2Int(rootX + offsetX, rootY + offsetY);
                    if (nodeMap.ContainsKey(adjacentPosition))
                    {
                        // Calculate the cost of the connection
                        cost = offsetX == 0 || offsetY == 0 ? 1 : diagonalCost;

                        // Add the connection to the connections list
                        _connections.Add(nodeMap[adjacentPosition], cost);
                    }
                }
            }
        }

        #region Gizmos & Inspector Setup

        public void SetupExit(Vector2Int exitDirection)
        {
            _isExit = true;
            _exitDirection = exitDirection;

            this.name = "Exit Node";
        }

        public void OnDrawGizmos()
        {
            if (!DEBUG_SHOW_NODES) return;

            Gizmos.color = _isExit ? Color.cyan : Color.cyan;
            if (_isExit)
            {
                Vector3 cubeSize = new Vector3(1, 2, 1);
                Vector3 rotatedExitDirection = Quaternion.Euler(0, transform.eulerAngles.y, 0) * new Vector3(_exitDirection.x, 0, _exitDirection.y);
                cubeSize.x = Mathf.Abs(rotatedExitDirection.z * 1.8f);
                cubeSize.z = Mathf.Abs(rotatedExitDirection.x * 1.8f);
                Gizmos.DrawWireCube(transform.position + Vector3.up, cubeSize);
                Gizmos.DrawLine(transform.position, transform.position + rotatedExitDirection);
            }
            else
            {
                Gizmos.DrawWireSphere(transform.position, .25f);
            }

            if (!DEBUG_SHOW_NODE_CONNECTIONS) return;

            if (_connections.Count > 0)
            {
                
                foreach (var connection in _connections)
                {
                    Gizmos.color = Color.Lerp(Color.yellow, Color.red, Mathf.InverseLerp(1, 1.5f, connection.Value));
                    Gizmos.DrawLine(transform.position, connection.Key.transform.position);
                }
            }
        }

        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UI;

namespace GDD3400.Labyrinth
{
    public class LevelManager : MonoBehaviour
    {
        [SerializeField] private float _DiagonalTileCost = 1.41f;
        [SerializeField] List<Collectable> _collectables = new List<Collectable>();
        [SerializeField] LevelExitHandler _levelExit;
        [SerializeField] Text _levelCompleteText;
        
        private List<TileHandler> _tiles = new List<TileHandler>();

        private Dictionary<Vector2Int, PathNode> _nodeMap = new Dictionary<Vector2Int, PathNode>();
                
        private void Awake()
        {
            InitializeTiles();

            _levelExit.Initialize(this);
            foreach (var collectable in _collectables)
            {
                collectable.Initialize(this);
            }
        }

        private void InitializeTiles()
        {
            // Collect all the tiles that are children of the level manager
            // Inactive tiles should not be included in the list
            _tiles = GetComponentsInChildren<TileHandler>(false).ToList();

            // First, initialize all the tiles and generate a map of all the nodes, storing them in the _nodeMap dictionary
            foreach (var tile in _tiles)
            {
                // Create the node map for the tile
                // The node map is a dictionary of vector2int and pathnode
                tile.CreateNodeMap(_nodeMap);
            }

            Debug.Log("Level Contains: " + _nodeMap.Count + " nodes");

            // Once we know where all the nodes are, we can generate the connections between the nodes
            foreach (var node in _nodeMap)
            {
                node.Value.GenerateConnections(_nodeMap, _DiagonalTileCost);
            }
        }

        public PathNode GetNode(Vector2Int position)
        {
            if (_nodeMap.ContainsKey(position))
            {
                return _nodeMap[position];
            }

            return null;
        }

        public PathNode GetNode(Vector3 position)
        {
            // Round the position to the nearest 2,2 tile
            // This is because the nodes are spaced 2 units apart
            int x = Mathf.RoundToInt(position.x / 2.0f) * 2;
            int z = Mathf.RoundToInt(position.z / 2.0f) * 2;

            // Try to get the node at the nearest 2,2 tile
            PathNode node = GetNode(new Vector2Int(x, z));
            if (node != null) return node;

            // If we don't have a node, search the surrounding tiles, and return the closest node we find
            // This is to account for destinations that migth be slightly too far away from the nearest 2,2 tile

            List<PathNode> nodes = new List<PathNode>();
            for (int offsetX = -2; offsetX <= 2; offsetX+=2)
            {
                for (int offsetZ = -2; offsetZ <= 2; offsetZ+=2)
                {
                    node = GetNode(new Vector2Int(x + offsetX, z + offsetZ));
                    if (node != null) nodes.Add(node);
                }
            }

            // Return the closest node we find
            if (nodes.Count > 0) return nodes.OrderBy(n => Vector3.Distance(n.transform.position, position)).FirstOrDefault();

            // If we don't have a node, return null
            return null;
        }

        public void CollectCollectable(Collectable collectable)
        {
            _collectables.Remove(collectable);
            Destroy(collectable.gameObject);

            if (_collectables.Count == 0)
            {
                _levelCompleteText.text = "Get to the exit!";
            }
            else
            {
                _levelCompleteText.text = "Collectables \nRemaining: " + _collectables.Count;
            }
        }

        public void LevelComplete()
        {
            if (_collectables.Count == 0)
            {
                _levelCompleteText.text = "Level complete!";
            } 
            else
            {
                Debug.Log("Level is not complete, missing " + _collectables.Count + " collectables");
            }
        }
    }
}

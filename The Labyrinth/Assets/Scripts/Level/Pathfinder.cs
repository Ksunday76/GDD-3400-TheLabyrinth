using System.Collections.Generic;
using UnityEngine;

namespace GDD3400.Labyrinth
{
    public class Pathfinder
    {
        public static List<PathNode> FindPath(PathNode startNode, PathNode endNode)
        {
            if (startNode == null || endNode == null) return new List<PathNode>();
            if (startNode == endNode) return new List<PathNode> { startNode };

            // Open set (nodes to evaluate)
            List<PathNode> openSet = new List<PathNode> { startNode };
            // Closed set (nodes already evaluated)
            HashSet<PathNode> closedSet = new HashSet<PathNode>();

            // Parent map to reconstruct path later
            Dictionary<PathNode, PathNode> cameFrom = new Dictionary<PathNode, PathNode>();

            // gScore: cost from start to node
            Dictionary<PathNode, float> gScore = new Dictionary<PathNode, float>();
            gScore[startNode] = 0f;

            // fScore: gScore + heuristic estimate
            Dictionary<PathNode, float> fScore = new Dictionary<PathNode, float>();
            fScore[startNode] = Heuristic(startNode, endNode);

            while (openSet.Count > 0)
            {
                PathNode current = GetLowestCost(openSet, fScore);

                if (current == endNode)
                    return ReconstructPath(cameFrom, current);

                openSet.Remove(current);
                closedSet.Add(current);

                if (current.Connections == null) continue;

                foreach (var kvp in current.Connections)
                {
                    PathNode neighbor = kvp.Key;
                    float edgeCost = kvp.Value;

                    if (neighbor == null) continue;
                    if (closedSet.Contains(neighbor)) continue;

                    float currentG = gScore.ContainsKey(current) ? gScore[current] : Mathf.Infinity;
                    float tentativeG = currentG + edgeCost;

                    bool betterPath = false;
                    if (!gScore.ContainsKey(neighbor))
                    {
                        betterPath = true;
                    }
                    else if (tentativeG < gScore[neighbor])
                    {
                        betterPath = true;
                    }

                    if (betterPath)
                    {
                        cameFrom[neighbor] = current;
                        gScore[neighbor] = tentativeG;
                        fScore[neighbor] = tentativeG + Heuristic(neighbor, endNode);

                        if (!openSet.Contains(neighbor))
                            openSet.Add(neighbor);
                    }
                }
            }

            // No path found
            return new List<PathNode>();
        }

        private static float Heuristic(PathNode a, PathNode b)
        {
            return Vector3.Distance(a.transform.position, b.transform.position);
        }

        private static PathNode GetLowestCost(List<PathNode> openSet, Dictionary<PathNode, float> fScore)
        {
            PathNode lowest = openSet[0];
            float lowestCost = fScore.ContainsKey(lowest) ? fScore[lowest] : Mathf.Infinity;

            for (int i = 1; i < openSet.Count; i++)
            {
                PathNode node = openSet[i];
                float cost = fScore.ContainsKey(node) ? fScore[node] : Mathf.Infinity;
                if (cost < lowestCost)
                {
                    lowest = node;
                    lowestCost = cost;
                }
            }

            return lowest;
        }

        private static List<PathNode> ReconstructPath(Dictionary<PathNode, PathNode> cameFrom, PathNode current)
        {
            List<PathNode> totalPath = new List<PathNode> { current };
            while (cameFrom.ContainsKey(current))
            {
                current = cameFrom[current];
                totalPath.Insert(0, current);
            }
            return totalPath;
        }
    }
}

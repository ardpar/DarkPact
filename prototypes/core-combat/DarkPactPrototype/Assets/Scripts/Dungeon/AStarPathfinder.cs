using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace DarkPact.Core
{
    public class AStarPathfinder : MonoBehaviour
    {
        [SerializeField] Tilemap _wallsTilemap;
        [SerializeField] int _maxPathLength = 50;

        static AStarPathfinder _instance;
        public static AStarPathfinder Instance => _instance;

        void Awake()
        {
            _instance = this;
            ServiceLocator.Register(this);
        }

        public List<Vector2> FindPath(Vector2 start, Vector2 end)
        {
            var startCell = (Vector2Int)(Vector3Int)_wallsTilemap.WorldToCell(start);
            var endCell = (Vector2Int)(Vector3Int)_wallsTilemap.WorldToCell(end);

            if (startCell == endCell) return new List<Vector2> { end };
            if (IsBlocked(endCell)) return null;

            var openSet = new SortedSet<Node>(new NodeComparer());
            var closedSet = new HashSet<Vector2Int>();
            var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
            var gScore = new Dictionary<Vector2Int, float>();

            gScore[startCell] = 0;
            openSet.Add(new Node(startCell, Heuristic(startCell, endCell)));

            while (openSet.Count > 0)
            {
                var current = First(openSet);
                openSet.Remove(current);

                if (current.Pos == endCell)
                    return ReconstructPath(cameFrom, endCell);

                closedSet.Add(current.Pos);

                foreach (var neighbor in GetNeighbors(current.Pos))
                {
                    if (closedSet.Contains(neighbor)) continue;
                    if (IsBlocked(neighbor)) continue;

                    float tentativeG = gScore.GetValueOrDefault(current.Pos, float.MaxValue) + Cost(current.Pos, neighbor);

                    if (tentativeG < gScore.GetValueOrDefault(neighbor, float.MaxValue))
                    {
                        cameFrom[neighbor] = current.Pos;
                        gScore[neighbor] = tentativeG;
                        float f = tentativeG + Heuristic(neighbor, endCell);
                        openSet.Add(new Node(neighbor, f));
                    }
                }

                if (closedSet.Count > _maxPathLength * 4) return null; // safety limit
            }

            return null; // no path
        }

        List<Vector2> ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current)
        {
            var path = new List<Vector2>();
            int safety = _maxPathLength;
            while (cameFrom.ContainsKey(current) && safety-- > 0)
            {
                path.Add(CellToWorld(current));
                current = cameFrom[current];
            }
            path.Reverse();
            return path;
        }

        Vector2 CellToWorld(Vector2Int cell)
        {
            return _wallsTilemap.CellToWorld(new Vector3Int(cell.x, cell.y, 0)) + _wallsTilemap.cellSize * 0.5f;
        }

        bool IsBlocked(Vector2Int cell)
        {
            return _wallsTilemap.HasTile(new Vector3Int(cell.x, cell.y, 0));
        }

        static readonly Vector2Int[] Dirs8 = {
            new(0,1), new(1,0), new(0,-1), new(-1,0),
            new(1,1), new(1,-1), new(-1,1), new(-1,-1)
        };

        IEnumerable<Vector2Int> GetNeighbors(Vector2Int pos)
        {
            for (int i = 0; i < 8; i++)
            {
                var n = pos + Dirs8[i];
                // Diagonal: prevent corner cutting
                if (i >= 4)
                {
                    var dx = new Vector2Int(Dirs8[i].x, 0);
                    var dy = new Vector2Int(0, Dirs8[i].y);
                    if (IsBlocked(pos + dx) || IsBlocked(pos + dy)) continue;
                }
                yield return n;
            }
        }

        float Heuristic(Vector2Int a, Vector2Int b)
        {
            // Octile distance
            int dx = Mathf.Abs(a.x - b.x);
            int dy = Mathf.Abs(a.y - b.y);
            return Mathf.Max(dx, dy) + 0.414f * Mathf.Min(dx, dy);
        }

        float Cost(Vector2Int from, Vector2Int to)
        {
            return (from.x != to.x && from.y != to.y) ? 1.414f : 1f;
        }

        struct Node
        {
            public Vector2Int Pos;
            public float F;
            public Node(Vector2Int pos, float f) { Pos = pos; F = f; }
        }

        class NodeComparer : IComparer<Node>
        {
            public int Compare(Node a, Node b)
            {
                int c = a.F.CompareTo(b.F);
                if (c != 0) return c;
                c = a.Pos.x.CompareTo(b.Pos.x);
                if (c != 0) return c;
                return a.Pos.y.CompareTo(b.Pos.y);
            }
        }

        static Node First(SortedSet<Node> set)
        {
            using var e = set.GetEnumerator();
            e.MoveNext();
            return e.Current;
        }
    }
}

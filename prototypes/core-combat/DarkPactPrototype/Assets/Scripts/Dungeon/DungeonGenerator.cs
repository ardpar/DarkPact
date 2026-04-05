using System.Collections.Generic;
using UnityEngine;

namespace DarkPact.Core
{
    public enum RoomType { Start, Combat, Treasure, Boss }

    public enum Direction { North, South, East, West }

    [System.Serializable]
    public class RoomData
    {
        public Vector2Int GridPos;
        public RoomType Type;
        public int PathIndex; // -1 for branch rooms
        public float Difficulty;
        public HashSet<Direction> Doors = new();
        public bool IsCleared;

        public Vector2 WorldCenter(float roomSpacing) =>
            new Vector2(GridPos.x * roomSpacing, GridPos.y * roomSpacing);
    }

    [System.Serializable]
    public class DungeonLayout
    {
        public int Seed;
        public List<RoomData> Rooms = new();
        public List<(int fromIndex, int toIndex, Direction dir)> Connections = new();
        public int StartRoomIndex;
        public int BossRoomIndex;

        public RoomData GetRoomAt(Vector2Int pos)
        {
            foreach (var r in Rooms)
                if (r.GridPos == pos) return r;
            return null;
        }
    }

    public class DungeonGenerator : MonoBehaviour
    {
        [Header("Layout Source")]
        [SerializeField] DungeonLayoutSO _fixedLayout;

        [Header("Procedural Generation")]
        [SerializeField] int _mainPathLength = 6;
        [SerializeField] float _branchChance = 0.25f;
        [SerializeField] int _maxBranchLength = 2;
        [SerializeField] int _maxTotalRooms = 12;
        [SerializeField] float _treasureRoomChance = 0.6f;

        [Header("Difficulty")]
        [SerializeField] float _baseDifficulty = 1f;
        [SerializeField] float _difficultyRange = 1f;

        public DungeonLayout CurrentLayout { get; private set; }
        public DungeonLayoutSO FixedLayout => _fixedLayout;

        static readonly Direction[] AllDirs = { Direction.North, Direction.East, Direction.South, Direction.West };

        void Awake()
        {
            ServiceLocator.Register(this);
        }

        public DungeonLayout Generate(int seed)
        {
            // Use fixed layout if assigned
            if (_fixedLayout != null)
            {
                CurrentLayout = _fixedLayout.ToDungeonLayout();
                return CurrentLayout;
            }

            var rng = new System.Random(seed);
            var layout = new DungeonLayout { Seed = seed };
            var occupied = new HashSet<Vector2Int>();

            // 1. Generate main path
            var mainPath = GenerateMainPath(rng, occupied);
            layout.Rooms.AddRange(mainPath);

            // Tag start and boss
            mainPath[0].Type = RoomType.Start;
            mainPath[^1].Type = RoomType.Boss;
            layout.StartRoomIndex = 0;
            layout.BossRoomIndex = mainPath.Count - 1;

            // Assign difficulty to main path
            for (int i = 0; i < mainPath.Count; i++)
            {
                float t = mainPath.Count > 1 ? (float)i / (mainPath.Count - 1) : 0;
                mainPath[i].Difficulty = _baseDifficulty + t * _difficultyRange;
                mainPath[i].PathIndex = i;
            }

            // Connect main path rooms
            for (int i = 0; i < mainPath.Count - 1; i++)
            {
                var dir = GetDirection(mainPath[i].GridPos, mainPath[i + 1].GridPos);
                mainPath[i].Doors.Add(dir);
                mainPath[i + 1].Doors.Add(Opposite(dir));
                layout.Connections.Add((i, i + 1, dir));
            }

            // 2. Generate branches
            for (int i = 1; i < mainPath.Count - 1; i++) // skip start and boss
            {
                if (layout.Rooms.Count >= _maxTotalRooms) break;
                if (rng.NextDouble() > _branchChance) continue;

                int branchLen = rng.Next(1, _maxBranchLength + 1);
                GenerateBranch(rng, layout, i, branchLen, occupied);
            }

            CurrentLayout = layout;
            return layout;
        }

        List<RoomData> GenerateMainPath(System.Random rng, HashSet<Vector2Int> occupied)
        {
            var rooms = new List<RoomData>();
            var pos = Vector2Int.zero;

            for (int i = 0; i < _mainPathLength; i++)
            {
                var room = new RoomData
                {
                    GridPos = pos,
                    Type = RoomType.Combat
                };
                rooms.Add(room);
                occupied.Add(pos);

                if (i < _mainPathLength - 1)
                {
                    // Pick random unoccupied neighbor
                    var nextPos = PickNeighbor(rng, pos, occupied);
                    if (nextPos == pos)
                    {
                        // Stuck — backtrack and retry
                        // Fallback: force a direction
                        foreach (var d in ShuffledDirs(rng))
                        {
                            var candidate = pos + DirToVec(d);
                            if (!occupied.Contains(candidate))
                            {
                                nextPos = candidate;
                                break;
                            }
                        }
                    }
                    pos = nextPos;
                }
            }
            return rooms;
        }

        void GenerateBranch(System.Random rng, DungeonLayout layout, int parentIndex, int length, HashSet<Vector2Int> occupied)
        {
            var parentRoom = layout.Rooms[parentIndex];
            var pos = parentRoom.GridPos;

            int prevIndex = parentIndex;
            for (int i = 0; i < length; i++)
            {
                if (layout.Rooms.Count >= _maxTotalRooms) break;

                var nextPos = PickNeighbor(rng, pos, occupied);
                if (nextPos == pos) break; // stuck

                bool isEnd = (i == length - 1);
                var room = new RoomData
                {
                    GridPos = nextPos,
                    Type = isEnd
                        ? (rng.NextDouble() < _treasureRoomChance ? RoomType.Treasure : RoomType.Combat)
                        : RoomType.Combat,
                    PathIndex = -1,
                    Difficulty = parentRoom.Difficulty * 0.8f // branches slightly easier
                };

                int newIndex = layout.Rooms.Count;
                layout.Rooms.Add(room);
                occupied.Add(nextPos);

                // Connect
                var dir = GetDirection(layout.Rooms[prevIndex].GridPos, nextPos);
                layout.Rooms[prevIndex].Doors.Add(dir);
                room.Doors.Add(Opposite(dir));
                layout.Connections.Add((prevIndex, newIndex, dir));

                prevIndex = newIndex;
                pos = nextPos;
            }
        }

        Vector2Int PickNeighbor(System.Random rng, Vector2Int pos, HashSet<Vector2Int> occupied)
        {
            var dirs = ShuffledDirs(rng);
            foreach (var d in dirs)
            {
                var candidate = pos + DirToVec(d);
                if (!occupied.Contains(candidate))
                    return candidate;
            }
            return pos; // stuck
        }

        Direction[] ShuffledDirs(System.Random rng)
        {
            var arr = (Direction[])AllDirs.Clone();
            for (int i = arr.Length - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (arr[i], arr[j]) = (arr[j], arr[i]);
            }
            return arr;
        }

        public static Vector2Int DirToVec(Direction d) => d switch
        {
            Direction.North => Vector2Int.up,
            Direction.South => Vector2Int.down,
            Direction.East => Vector2Int.right,
            Direction.West => Vector2Int.left,
            _ => Vector2Int.zero
        };

        public static Direction GetDirection(Vector2Int from, Vector2Int to)
        {
            var diff = to - from;
            if (diff.y > 0) return Direction.North;
            if (diff.y < 0) return Direction.South;
            if (diff.x > 0) return Direction.East;
            return Direction.West;
        }

        public static Direction Opposite(Direction d) => d switch
        {
            Direction.North => Direction.South,
            Direction.South => Direction.North,
            Direction.East => Direction.West,
            Direction.West => Direction.East,
            _ => d
        };
    }
}

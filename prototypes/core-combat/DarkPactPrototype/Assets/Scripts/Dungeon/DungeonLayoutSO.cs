using System.Collections.Generic;
using UnityEngine;

namespace DarkPact.Core
{
    [CreateAssetMenu(fileName = "NewDungeonLayout", menuName = "DarkPact/Dungeon Layout")]
    public class DungeonLayoutSO : ScriptableObject
    {
        [System.Serializable]
        public class RoomNode
        {
            public string Name;
            public RoomType Type = RoomType.Combat;
            public Vector2 EditorPosition; // node editor'da görsel pozisyon
            public float Difficulty = 1f;
            public GameObject RoomPrefab; // oda prefab'ı (tilemap, spawnpoints, dekor)
            [TextArea] public string Notes;
        }

        [System.Serializable]
        public class RoomConnection
        {
            public int FromIndex;
            public int ToIndex;
        }

        public string LayoutName;
        public int Act = 1;
        public List<RoomNode> Rooms = new();
        public List<RoomConnection> Connections = new();
        public int StartRoomIndex;
        public int BossRoomIndex = -1;

        public DungeonLayout ToDungeonLayout()
        {
            var layout = new DungeonLayout { Seed = LayoutName?.GetHashCode() ?? 0 };

            // Build grid positions from editor positions
            // Quantize to grid
            var gridPositions = new Dictionary<int, Vector2Int>();
            for (int i = 0; i < Rooms.Count; i++)
            {
                var pos = new Vector2Int(
                    Mathf.RoundToInt(Rooms[i].EditorPosition.x / 3f),
                    Mathf.RoundToInt(Rooms[i].EditorPosition.y / 3f)
                );
                gridPositions[i] = pos;

                layout.Rooms.Add(new RoomData
                {
                    GridPos = pos,
                    Type = Rooms[i].Type,
                    PathIndex = i,
                    Difficulty = Rooms[i].Difficulty,
                    IsCleared = false
                });
            }

            layout.StartRoomIndex = StartRoomIndex;
            layout.BossRoomIndex = BossRoomIndex >= 0 ? BossRoomIndex : Rooms.Count - 1;

            // Build connections and door directions
            foreach (var conn in Connections)
            {
                if (conn.FromIndex < 0 || conn.FromIndex >= Rooms.Count) continue;
                if (conn.ToIndex < 0 || conn.ToIndex >= Rooms.Count) continue;

                var dir = DungeonGenerator.GetDirection(gridPositions[conn.FromIndex], gridPositions[conn.ToIndex]);
                layout.Rooms[conn.FromIndex].Doors.Add(dir);
                layout.Rooms[conn.ToIndex].Doors.Add(DungeonGenerator.Opposite(dir));
                layout.Connections.Add((conn.FromIndex, conn.ToIndex, dir));
            }

            return layout;
        }

        #if UNITY_EDITOR
        // Validation
        public string Validate()
        {
            if (Rooms.Count == 0) return "No rooms defined";
            if (StartRoomIndex < 0 || StartRoomIndex >= Rooms.Count) return "Invalid start room index";

            int bossIdx = BossRoomIndex >= 0 ? BossRoomIndex : Rooms.Count - 1;
            if (bossIdx >= Rooms.Count) return "Invalid boss room index";

            // Check connectivity via BFS
            var visited = new HashSet<int>();
            var queue = new Queue<int>();
            queue.Enqueue(StartRoomIndex);
            visited.Add(StartRoomIndex);

            var adj = new Dictionary<int, List<int>>();
            for (int i = 0; i < Rooms.Count; i++) adj[i] = new List<int>();
            foreach (var c in Connections)
            {
                if (c.FromIndex >= 0 && c.FromIndex < Rooms.Count && c.ToIndex >= 0 && c.ToIndex < Rooms.Count)
                {
                    adj[c.FromIndex].Add(c.ToIndex);
                    adj[c.ToIndex].Add(c.FromIndex);
                }
            }

            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                foreach (int neighbor in adj[current])
                {
                    if (!visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }
            }

            if (visited.Count != Rooms.Count)
                return $"Not all rooms reachable! {visited.Count}/{Rooms.Count} connected";

            if (!visited.Contains(bossIdx))
                return "Boss room not reachable from start!";

            return null; // valid
        }
        #endif
    }
}

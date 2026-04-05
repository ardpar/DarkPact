using System.Collections.Generic;
using UnityEngine;

namespace DarkPact.Core
{
    public class DungeonManager : MonoBehaviour
    {
        [Header("Fallback (no prefab rooms)")]
        [SerializeField] GameObject _fallbackRoomPrefab;
        [SerializeField] GameObject _bossPrefab;

        DungeonLayout _layout;
        DungeonLayoutSO _layoutSO;
        int _currentRoomIndex = -1;
        GameObject _activeRoomInstance;
        RoomPrefabData _activeRoomData;
        readonly List<GameObject> _activeDoors = new();

        public RoomData CurrentRoom => _layout != null && _currentRoomIndex >= 0
            ? _layout.Rooms[_currentRoomIndex] : null;
        public DungeonLayout Layout => _layout;
        public int CurrentRoomIndex => _currentRoomIndex;

        void Awake()
        {
            ServiceLocator.Register(this);
        }

        public void BuildDungeon(DungeonLayout layout, DungeonLayoutSO layoutSO = null)
        {
            _layout = layout;
            _layoutSO = layoutSO;
            EnterRoom(layout.StartRoomIndex);
        }

        // === ROOM LIFECYCLE ===

        void EnterRoom(int roomIndex)
        {
            if (roomIndex < 0 || roomIndex >= _layout.Rooms.Count) return;

            // Cleanup previous room
            UnloadCurrentRoom();

            _currentRoomIndex = roomIndex;
            var room = _layout.Rooms[roomIndex];

            // Load room prefab
            GameObject prefab = GetRoomPrefab(roomIndex);
            if (prefab != null)
            {
                _activeRoomInstance = Instantiate(prefab, Vector3.zero, Quaternion.identity);
                _activeRoomData = _activeRoomInstance.GetComponent<RoomPrefabData>();
            }

            // Place player
            PlacePlayer(room);

            // Setup camera bounds
            SetupCameraBounds();

            // Spawn door triggers
            SpawnDoorTriggers(room, roomIndex);

            // Notify RunManager
            if (ServiceLocator.TryGet<RunManager>(out var run))
                run.OnRoomEntered(roomIndex, room.Type);

            // Spawn room content
            if (!room.IsCleared)
                SpawnRoomContent(room);

            Debug.Log($"[Dungeon] Entered: {GetRoomName(roomIndex)} ({room.Type}, diff {room.Difficulty:F1})");
        }

        void UnloadCurrentRoom()
        {
            // Destroy room instance
            if (_activeRoomInstance != null)
            {
                Destroy(_activeRoomInstance);
                _activeRoomInstance = null;
                _activeRoomData = null;
            }

            // Destroy door triggers
            foreach (var d in _activeDoors)
                if (d != null) Destroy(d);
            _activeDoors.Clear();

            // Clear spawned enemies
            if (ServiceLocator.TryGet<RoomManager>(out var rm))
                rm.ClearAllEnemies();
        }

        // === TRANSITION ===

        public void TransitionToRoom(int targetRoomIndex, Direction fromDirection)
        {
            EnterRoom(targetRoomIndex);

            // Override player position to opposite door
            if (_activeRoomData != null)
            {
                var oppositeDir = DungeonGenerator.Opposite(fromDirection);
                var doorTransform = _activeRoomData.GetDoor(oppositeDir);
                if (doorTransform != null && ServiceLocator.TryGet<PlayerController>(out var player))
                {
                    // Offset inward from door
                    Vector2 inward = DirToVector(oppositeDir) * -2f;
                    player.transform.position = (Vector2)doorTransform.position + inward;
                }
            }
        }

        // === PREFAB LOOKUP ===

        GameObject GetRoomPrefab(int roomIndex)
        {
            // Check SO for room-specific prefab
            if (_layoutSO != null && roomIndex < _layoutSO.Rooms.Count)
            {
                var nodePrefab = _layoutSO.Rooms[roomIndex].RoomPrefab;
                if (nodePrefab != null) return nodePrefab;
            }

            return _fallbackRoomPrefab;
        }

        string GetRoomName(int roomIndex)
        {
            if (_layoutSO != null && roomIndex < _layoutSO.Rooms.Count)
                return _layoutSO.Rooms[roomIndex].Name;
            return $"Room {roomIndex}";
        }

        // === PLAYER PLACEMENT ===

        void PlacePlayer(RoomData room)
        {
            if (!ServiceLocator.TryGet<PlayerController>(out var player)) return;

            if (_activeRoomData != null && _activeRoomData.PlayerSpawnPoint != null)
            {
                player.transform.position = _activeRoomData.PlayerSpawnPoint.position;
            }
            else
            {
                player.transform.position = Vector3.zero;
            }
        }

        // === CAMERA ===

        void SetupCameraBounds()
        {
            if (!ServiceLocator.TryGet<CameraController>(out var cam)) return;

            if (_activeRoomData != null)
            {
                var bounds = _activeRoomData.GetBounds();
                cam.SetRoomBounds(bounds.min, bounds.max);
            }
            else
            {
                cam.ClearBounds();
            }
        }

        // === DOOR TRIGGERS ===

        void SpawnDoorTriggers(RoomData room, int roomIndex)
        {
            foreach (var door in room.Doors)
            {
                // Find connected room
                int connectedIndex = FindConnectedRoom(roomIndex, door);
                if (connectedIndex < 0) continue;

                // Get door position from prefab or calculate
                Vector2 triggerPos;
                if (_activeRoomData != null && _activeRoomData.HasDoor(door))
                {
                    triggerPos = _activeRoomData.GetDoor(door).position;
                }
                else
                {
                    // Fallback: estimate door position
                    triggerPos = DirToVector(door) * 10f;
                }

                var doorObj = new GameObject($"Door_{door}_to_{connectedIndex}");
                doorObj.transform.position = triggerPos;

                var col = doorObj.AddComponent<BoxCollider2D>();
                col.size = (door == Direction.North || door == Direction.South)
                    ? new Vector2(3, 2) : new Vector2(2, 3);
                col.isTrigger = true;

                var rb = doorObj.AddComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Static;

                var trigger = doorObj.AddComponent<DoorTrigger>();
                trigger.FromRoomIndex = roomIndex;
                trigger.ToRoomIndex = connectedIndex;
                trigger.DoorDirection = door;

                _activeDoors.Add(doorObj);
            }
        }

        int FindConnectedRoom(int roomIndex, Direction door)
        {
            foreach (var (from, to, dir) in _layout.Connections)
            {
                if (from == roomIndex && dir == door) return to;
                if (to == roomIndex && DungeonGenerator.Opposite(dir) == door) return from;
            }
            return -1;
        }

        // === ROOM CONTENT ===

        void SpawnRoomContent(RoomData room)
        {
            switch (room.Type)
            {
                case RoomType.Combat:
                    SpawnEnemies(room);
                    break;
                case RoomType.Boss:
                    SpawnBoss();
                    break;
                case RoomType.Treasure:
                    room.IsCleared = true; // auto-clear treasure rooms
                    break;
            }
        }

        void SpawnEnemies(RoomData room)
        {
            if (!ServiceLocator.TryGet<RoomManager>(out var rm)) return;

            if (_activeRoomData != null && _activeRoomData.EnemySpawnPoints != null && _activeRoomData.EnemySpawnPoints.Length > 0)
            {
                // Use prefab spawn points
                rm.SpawnEnemiesAtPoints(_activeRoomData.EnemySpawnPoints, room.Difficulty);
            }
            else
            {
                rm.SpawnEnemiesForRoom(Vector2.zero, room.Difficulty);
            }
        }

        void SpawnBoss()
        {
            if (_bossPrefab == null) return;

            Vector3 spawnPos = Vector3.zero;
            if (_activeRoomData != null && _activeRoomData.BossSpawnPoint != null)
                spawnPos = _activeRoomData.BossSpawnPoint.position;

            Instantiate(_bossPrefab, spawnPos, Quaternion.identity);
        }

        public void OnCurrentRoomCleared()
        {
            if (CurrentRoom != null) CurrentRoom.IsCleared = true;
        }

        static Vector2 DirToVector(Direction dir) => dir switch
        {
            Direction.North => Vector2.up,
            Direction.South => Vector2.down,
            Direction.East => Vector2.right,
            Direction.West => Vector2.left,
            _ => Vector2.zero
        };
    }
}

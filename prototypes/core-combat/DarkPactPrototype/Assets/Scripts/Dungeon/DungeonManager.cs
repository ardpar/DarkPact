using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace DarkPact.Core
{
    public class DungeonManager : MonoBehaviour
    {
        [Header("Room Dimensions (tiles)")]
        [SerializeField] int _roomWidth = 22;
        [SerializeField] int _roomHeight = 16;
        [SerializeField] int _doorWidth = 4;

        [Header("Tiles")]
        [SerializeField] TileBase _floorTile;
        [SerializeField] TileBase _wallTile;

        [Header("References")]
        [SerializeField] Tilemap _groundTilemap;
        [SerializeField] Tilemap _wallsTilemap;

        [Header("Prefabs")]
        [SerializeField] GameObject _bossPrefab;

        DungeonLayout _layout;
        int _currentRoomIndex = -1;
        readonly List<GameObject> _doorTriggers = new();
        readonly List<GameObject> _spawnedEnemies = new();

        public RoomData CurrentRoom => _layout != null && _currentRoomIndex >= 0
            ? _layout.Rooms[_currentRoomIndex] : null;
        public DungeonLayout Layout => _layout;
        public int CurrentRoomIndex => _currentRoomIndex;

        void Awake()
        {
            ServiceLocator.Register(this);
        }

        // === BUILD ===

        public void BuildDungeon(DungeonLayout layout)
        {
            _layout = layout;
            ClearDungeon();

            // Draw the current room only (node-based: one room at a time)
            EnterRoom(layout.StartRoomIndex);
        }

        void ClearDungeon()
        {
            _groundTilemap.ClearAllTiles();
            _wallsTilemap.ClearAllTiles();
            ClearDoors();
            ClearSpawnedEnemies();
        }

        // === ROOM RENDERING ===

        void DrawCurrentRoom(RoomData room)
        {
            _groundTilemap.ClearAllTiles();
            _wallsTilemap.ClearAllTiles();

            // Room is always drawn at world origin (0,0) centered
            int halfW = _roomWidth / 2;
            int halfH = _roomHeight / 2;

            for (int x = 0; x < _roomWidth; x++)
            {
                for (int y = 0; y < _roomHeight; y++)
                {
                    var tilePos = new Vector3Int(x - halfW, y - halfH, 0);
                    bool isEdge = x == 0 || x == _roomWidth - 1 || y == 0 || y == _roomHeight - 1;

                    if (isEdge)
                    {
                        if (IsDoorPosition(room, x, y))
                            _groundTilemap.SetTile(tilePos, _floorTile);
                        else
                            _wallsTilemap.SetTile(tilePos, _wallTile);
                    }
                    else
                    {
                        _groundTilemap.SetTile(tilePos, _floorTile);
                    }
                }
            }

            // Refresh composite collider
            var composite = _wallsTilemap.GetComponent<CompositeCollider2D>();
            if (composite != null) composite.GenerateGeometry();
        }

        bool IsDoorPosition(RoomData room, int localX, int localY)
        {
            int halfDoor = _doorWidth / 2;
            int cx = _roomWidth / 2;
            int cy = _roomHeight / 2;

            if (room.Doors.Contains(Direction.North) && localY == _roomHeight - 1)
                if (localX >= cx - halfDoor && localX < cx + halfDoor) return true;

            if (room.Doors.Contains(Direction.South) && localY == 0)
                if (localX >= cx - halfDoor && localX < cx + halfDoor) return true;

            if (room.Doors.Contains(Direction.East) && localX == _roomWidth - 1)
                if (localY >= cy - halfDoor && localY < cy + halfDoor) return true;

            if (room.Doors.Contains(Direction.West) && localX == 0)
                if (localY >= cy - halfDoor && localY < cy + halfDoor) return true;

            return false;
        }

        // === DOOR TRIGGERS ===

        void SpawnDoorTriggers(RoomData room, int roomIndex)
        {
            ClearDoors();

            int halfW = _roomWidth / 2;
            int halfH = _roomHeight / 2;

            foreach (var door in room.Doors)
            {
                // Find connected room
                int connectedIndex = -1;
                foreach (var (from, to, dir) in _layout.Connections)
                {
                    if (from == roomIndex && dir == door) { connectedIndex = to; break; }
                    if (to == roomIndex && DungeonGenerator.Opposite(dir) == door) { connectedIndex = from; break; }
                }
                if (connectedIndex < 0) continue;

                // Position trigger at door opening
                Vector2 triggerPos = door switch
                {
                    Direction.North => new Vector2(0, halfH - 0.5f),
                    Direction.South => new Vector2(0, -halfH + 0.5f),
                    Direction.East => new Vector2(halfW - 0.5f, 0),
                    Direction.West => new Vector2(-halfW + 0.5f, 0),
                    _ => Vector2.zero
                };

                Vector2 triggerSize = (door == Direction.North || door == Direction.South)
                    ? new Vector2(_doorWidth, 2)
                    : new Vector2(2, _doorWidth);

                var doorObj = new GameObject($"Door_{door}_{connectedIndex}");
                doorObj.transform.position = triggerPos;

                var col = doorObj.AddComponent<BoxCollider2D>();
                col.size = triggerSize;
                col.isTrigger = true;

                var rb = doorObj.AddComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Static;

                var trigger = doorObj.AddComponent<DoorTrigger>();
                trigger.FromRoomIndex = roomIndex;
                trigger.ToRoomIndex = connectedIndex;
                trigger.DoorDirection = door;

                _doorTriggers.Add(doorObj);
            }
        }

        void ClearDoors()
        {
            foreach (var d in _doorTriggers)
                if (d != null) Destroy(d);
            _doorTriggers.Clear();
        }

        // === ROOM TRANSITION ===

        public void TransitionToRoom(int targetRoomIndex, Direction fromDirection)
        {
            ClearSpawnedEnemies();
            EnterRoom(targetRoomIndex);

            // Place player at opposite door
            var oppositeDir = DungeonGenerator.Opposite(fromDirection);
            int halfW = _roomWidth / 2;
            int halfH = _roomHeight / 2;

            Vector2 spawnPos = oppositeDir switch
            {
                Direction.North => new Vector2(0, halfH - 3),
                Direction.South => new Vector2(0, -halfH + 3),
                Direction.East => new Vector2(halfW - 3, 0),
                Direction.West => new Vector2(-halfW + 3, 0),
                _ => Vector2.zero
            };

            if (ServiceLocator.TryGet<PlayerController>(out var player))
                player.transform.position = spawnPos;
        }

        void EnterRoom(int roomIndex)
        {
            if (roomIndex < 0 || roomIndex >= _layout.Rooms.Count) return;
            _currentRoomIndex = roomIndex;
            var room = _layout.Rooms[roomIndex];

            // Draw room tiles
            DrawCurrentRoom(room);

            // Spawn door triggers
            SpawnDoorTriggers(room, roomIndex);

            // Set camera bounds
            if (ServiceLocator.TryGet<CameraController>(out var cam))
            {
                int halfW = _roomWidth / 2;
                int halfH = _roomHeight / 2;
                cam.SetRoomBounds(new Vector2(-halfW, -halfH), new Vector2(halfW, halfH));
            }

            // Notify RunManager
            if (ServiceLocator.TryGet<RunManager>(out var run))
                run.OnRoomEntered(roomIndex, room.Type);

            // Spawn content based on room type
            if (!room.IsCleared)
            {
                switch (room.Type)
                {
                    case RoomType.Combat:
                        if (ServiceLocator.TryGet<RoomManager>(out var roomMgr))
                            roomMgr.SpawnEnemiesForRoom(Vector2.zero, room.Difficulty);
                        break;
                    case RoomType.Boss:
                        if (_bossPrefab != null)
                            _spawnedEnemies.Add(Instantiate(_bossPrefab, Vector2.zero, Quaternion.identity));
                        break;
                    case RoomType.Treasure:
                        // TODO: spawn treasure chest
                        room.IsCleared = true;
                        break;
                }
            }

            Debug.Log($"[Dungeon] Entered room {roomIndex}: {room.Type} (difficulty {room.Difficulty:F1})");
        }

        public void OnCurrentRoomCleared()
        {
            if (CurrentRoom != null) CurrentRoom.IsCleared = true;
        }

        void ClearSpawnedEnemies()
        {
            foreach (var e in _spawnedEnemies)
                if (e != null) Destroy(e);
            _spawnedEnemies.Clear();
        }
    }
}

using UnityEngine;

namespace DarkPact.Core
{
    public class RoomPrefabData : MonoBehaviour
    {
        [Header("Door Markers")]
        [SerializeField] Transform _doorNorth;
        [SerializeField] Transform _doorSouth;
        [SerializeField] Transform _doorEast;
        [SerializeField] Transform _doorWest;

        [Header("Spawn")]
        [SerializeField] Transform[] _enemySpawnPoints;
        [SerializeField] Transform _playerSpawnPoint;
        [SerializeField] Transform _bossSpawnPoint;

        [Header("Room Bounds")]
        [SerializeField] BoxCollider2D _roomBounds;

        public Transform GetDoor(Direction dir) => dir switch
        {
            Direction.North => _doorNorth,
            Direction.South => _doorSouth,
            Direction.East => _doorEast,
            Direction.West => _doorWest,
            _ => null
        };

        public bool HasDoor(Direction dir) => GetDoor(dir) != null;

        public Transform[] EnemySpawnPoints => _enemySpawnPoints;
        public Transform PlayerSpawnPoint => _playerSpawnPoint;
        public Transform BossSpawnPoint => _bossSpawnPoint;

        public Bounds GetBounds()
        {
            if (_roomBounds != null)
                return _roomBounds.bounds;
            return new Bounds(transform.position, new Vector3(22, 16, 0));
        }
    }
}

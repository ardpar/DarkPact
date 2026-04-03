using System.Collections.Generic;
using UnityEngine;

namespace DarkPact.Core
{
    public class RoomManager : MonoBehaviour
    {
        [SerializeField] GameObject _enemyPrefab;
        [SerializeField] Transform[] _spawnPoints;
        [SerializeField] int _baseEnemyCount = 4;

        readonly List<EnemyAI> _activeEnemies = new();
        bool _isCleared;

        public event System.Action OnRoomCleared;

        void Awake()
        {
            ServiceLocator.Register(this);
        }

        void Start()
        {
            // Only auto-spawn if no DungeonManager (legacy single-room mode)
            if (!ServiceLocator.TryGet<DungeonManager>(out _))
                SpawnEnemiesForRoom(transform.position, 1f);
        }

        public void SpawnEnemiesForRoom(Vector2 roomCenter, float difficulty)
        {
            ClearEnemies();
            _isCleared = false;

            if (_enemyPrefab == null) return;

            int count = Mathf.RoundToInt(_baseEnemyCount * difficulty);
            count = Mathf.Clamp(count, 2, 10);

            for (int i = 0; i < count; i++)
            {
                // Spawn around room center with offset
                float angle = (360f / count) * i * Mathf.Deg2Rad;
                float radius = 3f;
                Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                Vector2 spawnPos = roomCenter + offset;

                var enemyObj = Instantiate(_enemyPrefab, spawnPos, Quaternion.identity);
                var enemy = enemyObj.GetComponent<EnemyAI>();

                if (enemy != null)
                {
                    if (ServiceLocator.TryGet<PactManager>(out var pact) && pact.IsKatliamActive)
                        enemy.CanRespawn = true;

                    enemy.OnEnemyDied += HandleEnemyDied;
                    _activeEnemies.Add(enemy);
                }
            }
        }

        void ClearEnemies()
        {
            foreach (var enemy in _activeEnemies)
            {
                if (enemy != null)
                {
                    enemy.OnEnemyDied -= HandleEnemyDied;
                    Destroy(enemy.gameObject);
                }
            }
            _activeEnemies.Clear();
        }

        void HandleEnemyDied(EnemyAI enemy)
        {
            enemy.OnEnemyDied -= HandleEnemyDied;
            _activeEnemies.Remove(enemy);

            if (_activeEnemies.Count == 0 && !_isCleared)
            {
                _isCleared = true;
                OnRoomCleared?.Invoke();

                if (ServiceLocator.TryGet<DungeonManager>(out var dungeon))
                    dungeon.OnCurrentRoomCleared();
                if (ServiceLocator.TryGet<RunManager>(out var run))
                    run.OnRoomCleared();

                Debug.Log("Room Cleared!");
            }
        }
    }
}

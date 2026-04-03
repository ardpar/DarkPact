using System.Collections.Generic;
using UnityEngine;

namespace DarkPact.Core
{
    public class RoomManager : MonoBehaviour
    {
        [SerializeField] GameObject _enemyPrefab;
        [SerializeField] Transform[] _spawnPoints;
        [SerializeField] int _enemyCount = 4;

        readonly List<EnemyAI> _activeEnemies = new();
        bool _isCleared;

        public event System.Action OnRoomCleared;

        void Start()
        {
            SpawnEnemies();
        }

        void SpawnEnemies()
        {
            if (_enemyPrefab == null || _spawnPoints == null || _spawnPoints.Length == 0) return;

            for (int i = 0; i < _enemyCount; i++)
            {
                var spawnPoint = _spawnPoints[i % _spawnPoints.Length];
                var enemyObj = Instantiate(_enemyPrefab, spawnPoint.position, Quaternion.identity);
                var enemy = enemyObj.GetComponent<EnemyAI>();

                if (enemy != null)
                {
                    // Katliam Paktı check
                    var pact = FindAnyObjectByType<PactManager>();
                    if (pact != null && pact.IsKatliamActive)
                        enemy.CanRespawn = true;

                    enemy.OnEnemyDied += HandleEnemyDied;
                    _activeEnemies.Add(enemy);
                }
            }
        }

        void HandleEnemyDied(EnemyAI enemy)
        {
            enemy.OnEnemyDied -= HandleEnemyDied;
            _activeEnemies.Remove(enemy);

            if (_activeEnemies.Count == 0 && !_isCleared)
            {
                _isCleared = true;
                OnRoomCleared?.Invoke();

                // Notify RunManager
                if (ServiceLocator.TryGet<RunManager>(out var run))
                    run.OnRoomCleared();

                Debug.Log("Room Cleared!");
            }
        }
    }
}

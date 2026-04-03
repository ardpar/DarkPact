using UnityEngine;
using UnityEngine.InputSystem;

namespace DarkPact.Core
{
    public class GameBootstrap : MonoBehaviour
    {
        Health _playerHealth;

        void Start()
        {
            GameManager.OnGameStateChanged += OnGameStateChanged;

            // Auto-start run when Gameplay scene loads
            if (ServiceLocator.TryGet<RunManager>(out var run))
                run.StartNewRun();
        }

        void OnDestroy()
        {
            GameManager.OnGameStateChanged -= OnGameStateChanged;
            UnsubscribePlayerHealth();
        }

        void Update()
        {
            HitstopManager.Tick();

            if (_playerHealth == null)
            {
                if (ServiceLocator.TryGet<PlayerController>(out var player))
                {
                    _playerHealth = player.GetComponent<Health>();
                    if (_playerHealth != null)
                    {
                        _playerHealth.OnDamaged += OnPlayerDamaged;
                        _playerHealth.OnDeath += OnPlayerDied;
                    }
                }
            }
        }

        void OnGameStateChanged(GameState oldState, GameState newState)
        {
            // No longer needed — run starts automatically in Start()
        }

        void OnPlayerDamaged(int amount, Vector2 dir)
        {
            if (ServiceLocator.TryGet<RunManager>(out var run))
                run.RecordDamageTaken(amount);
        }

        void OnPlayerDied()
        {
            if (ServiceLocator.TryGet<RunManager>(out var run))
                run.OnPlayerDied();
        }

        void UnsubscribePlayerHealth()
        {
            if (_playerHealth != null)
            {
                _playerHealth.OnDamaged -= OnPlayerDamaged;
                _playerHealth.OnDeath -= OnPlayerDied;
            }
        }
    }
}

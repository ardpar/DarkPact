using UnityEngine;

namespace DarkPact.Core
{
    public class GameBootstrap : MonoBehaviour
    {
        Health _playerHealth;

        void Start()
        {
            // Wire GameManager state changes to RunManager
            GameManager.OnGameStateChanged += OnGameStateChanged;
        }

        void OnDestroy()
        {
            GameManager.OnGameStateChanged -= OnGameStateChanged;
            UnsubscribePlayerHealth();
        }

        void Update()
        {
            HitstopManager.Tick();

            // Lazy-bind to player health for stat tracking
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
            // Auto-start a new run when entering Playing from MainMenu
            if (oldState == GameState.MainMenu && newState == GameState.Playing)
            {
                if (ServiceLocator.TryGet<RunManager>(out var run))
                    run.StartNewRun();
            }
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

        // Prototype shortcut: press N to start new run manually
        void LateUpdate()
        {
            if (Input.GetKeyDown(KeyCode.N))
            {
                if (ServiceLocator.TryGet<RunManager>(out var run))
                    run.StartNewRun();
            }
        }
    }
}

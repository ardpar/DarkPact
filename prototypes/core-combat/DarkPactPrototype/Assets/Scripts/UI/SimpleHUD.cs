using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace DarkPact.Core
{
    public class SimpleHUD : MonoBehaviour
    {
        [SerializeField] Slider _hpBar;
        [SerializeField] Image _hpFill;
        [SerializeField] GameObject _pactIcon;
        [SerializeField] TMPro.TextMeshProUGUI _gameOverText;
        [SerializeField] TMPro.TextMeshProUGUI _restartText;
        [SerializeField] TMPro.TextMeshProUGUI _roomCounterText;
        [SerializeField] TMPro.TextMeshProUGUI _killCountText;

        Health _playerHealth;
        PactManager _pactManager;
        InputAction _submitAction;

        void Start()
        {
            if (_gameOverText) _gameOverText.gameObject.SetActive(false);
            if (_restartText) _restartText.gameObject.SetActive(false);
            if (_pactIcon) _pactIcon.SetActive(false);

            GameManager.OnGameStateChanged += OnGameStateChanged;

            // Cache PactManager
            ServiceLocator.TryGet<PactManager>(out _pactManager);

            // Bind restart to Submit action (Enter / left-click) from UI map
            var playerInput = FindAnyObjectByType<PlayerInput>();
            if (playerInput != null)
            {
                _submitAction = playerInput.actions.FindAction("Submit");
            }
        }

        void OnDestroy()
        {
            GameManager.OnGameStateChanged -= OnGameStateChanged;
            if (_playerHealth != null)
                _playerHealth.OnHealthChanged -= UpdateHP;
        }

        void Update()
        {
            // Find player if not yet
            if (_playerHealth == null)
            {
                if (ServiceLocator.TryGet<PlayerController>(out var player))
                {
                    _playerHealth = player.GetComponent<Health>();
                    _playerHealth.OnHealthChanged += UpdateHP;
                    UpdateHP(_playerHealth.CurrentHP, _playerHealth.MaxHP);
                }
            }

            // Restart on GameOver
            if (ServiceLocator.TryGet<GameManager>(out var gm) && gm.CurrentState == GameState.GameOver)
            {
                if (Keyboard.current != null)
                {
                    if (Keyboard.current.rKey.wasPressedThisFrame)
                        gm.RestartGameplay();
                    else if (Keyboard.current.escapeKey.wasPressedThisFrame)
                        gm.LoadMainMenuScene();
                }
            }

            // Pact icon visibility
            if (_pactIcon != null)
            {
                if (_pactManager == null) ServiceLocator.TryGet<PactManager>(out _pactManager);
                _pactIcon.SetActive(_pactManager != null && _pactManager.IsKatliamActive);
            }

            // Run stats display
            if (ServiceLocator.TryGet<RunManager>(out var run))
            {
                if (_roomCounterText) _roomCounterText.text = $"Room {run.CurrentRoom + 1}";
                if (_killCountText) _killCountText.text = $"Kills: {run.Stats.KillCount}";
            }
        }

        void UpdateHP(int current, int max)
        {
            if (_hpBar == null) return;
            _hpBar.maxValue = max;
            _hpBar.value = current;

            if (_hpFill != null)
            {
                float ratio = (float)current / max;
                _hpFill.color = ratio > 0.5f ? Color.green : ratio > 0.25f ? Color.yellow : Color.red;
            }
        }

        void OnGameStateChanged(GameState oldState, GameState newState)
        {
            if (_gameOverText) _gameOverText.gameObject.SetActive(newState == GameState.GameOver);
            if (_restartText) _restartText.gameObject.SetActive(newState == GameState.GameOver);
        }
    }
}

using System;
using UnityEngine;

namespace DarkPact.Core
{
    public class GameManager : MonoBehaviour
    {
        public static event Action<GameState, GameState> OnGameStateChanged;

        public GameState CurrentState { get; private set; } = GameState.Boot;

        bool _isTransitioning;

        void Awake()
        {
            DontDestroyOnLoad(gameObject);
            ServiceLocator.Register(this);
        }

        void Start()
        {
            // Boot complete — go to MainMenu
            TransitionTo(GameState.MainMenu);
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (CurrentState == GameState.Playing)
                    TransitionTo(GameState.Paused);
                else if (CurrentState == GameState.Paused)
                    TransitionTo(GameState.Playing);
            }
        }

        public bool RequestStateChange(GameState newState)
        {
            return TransitionTo(newState);
        }

        bool TransitionTo(GameState newState)
        {
            if (_isTransitioning) return false;
            if (newState == CurrentState) return false;
            if (!IsValidTransition(CurrentState, newState)) return false;

            _isTransitioning = true;
            var oldState = CurrentState;
            CurrentState = newState;

            ApplyTimeScale(newState);
            OnGameStateChanged?.Invoke(oldState, newState);

            _isTransitioning = false;
            return true;
        }

        void ApplyTimeScale(GameState state)
        {
            Time.timeScale = state == GameState.Paused ? 0f : 1f;
        }

        bool IsValidTransition(GameState from, GameState to)
        {
            return (from, to) switch
            {
                (GameState.Boot, GameState.MainMenu) => true,
                (GameState.MainMenu, GameState.Loading) => true,
                (GameState.MainMenu, GameState.Playing) => true, // prototype shortcut
                (GameState.Loading, GameState.Playing) => true,
                (GameState.Loading, GameState.MainMenu) => true,
                (GameState.Playing, GameState.Paused) => true,
                (GameState.Playing, GameState.GameOver) => true,
                (GameState.Playing, GameState.Loading) => true,
                (GameState.Paused, GameState.Playing) => true,
                (GameState.Paused, GameState.MainMenu) => true,
                (GameState.Paused, GameState.GameOver) => true, // death during pause
                (GameState.GameOver, GameState.Loading) => true,
                (GameState.GameOver, GameState.MainMenu) => true,
                (GameState.GameOver, GameState.Playing) => true, // prototype quick restart
                _ => false
            };
        }
    }
}

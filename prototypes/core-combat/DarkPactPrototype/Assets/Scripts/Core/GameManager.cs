using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace DarkPact.Core
{
    public class GameManager : MonoBehaviour
    {
        public static event Action<GameState, GameState> OnGameStateChanged;

        public const string MainMenuScene = "MainMenu";
        public const string GameplayScene = "Gameplay";

        public GameState CurrentState { get; private set; } = GameState.Boot;

        bool _isTransitioning;

        void Awake()
        {
            DontDestroyOnLoad(gameObject);
            ServiceLocator.Register(this);
        }

        void Start()
        {
            TransitionTo(GameState.MainMenu);
        }

        void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
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

        public void LoadGameplayScene()
        {
            TransitionTo(GameState.Loading);
            SceneManager.LoadScene(GameplayScene);
            TransitionTo(GameState.Playing);
        }

        public void LoadMainMenuScene()
        {
            ServiceLocator.Reset();
            ServiceLocator.Register(this);
            Time.timeScale = 1f;
            SceneManager.LoadScene(MainMenuScene);
            CurrentState = GameState.Boot;
            TransitionTo(GameState.MainMenu);
        }

        public void RestartGameplay()
        {
            ServiceLocator.Reset();
            ServiceLocator.Register(this);
            Time.timeScale = 1f;
            SceneManager.LoadScene(GameplayScene);
            TransitionTo(GameState.Playing);
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
                (GameState.MainMenu, GameState.Playing) => true,
                (GameState.Loading, GameState.Playing) => true,
                (GameState.Loading, GameState.MainMenu) => true,
                (GameState.Playing, GameState.Paused) => true,
                (GameState.Playing, GameState.GameOver) => true,
                (GameState.Playing, GameState.Loading) => true,
                (GameState.Paused, GameState.Playing) => true,
                (GameState.Paused, GameState.MainMenu) => true,
                (GameState.Paused, GameState.GameOver) => true,
                (GameState.GameOver, GameState.Loading) => true,
                (GameState.GameOver, GameState.MainMenu) => true,
                (GameState.GameOver, GameState.Playing) => true,
                _ => false
            };
        }
    }
}

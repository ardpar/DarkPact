using System;
using UnityEngine;

namespace DarkPact.Core
{
    public enum RunState
    {
        Inactive,
        Starting,
        PactSelection,
        DungeonPhase,
        BossPhase,
        RunEnd
    }

    [Serializable]
    public class RunStatistics
    {
        public int KillCount;
        public int DamageTaken;
        public int DamageDealt;
        public int RoomsExplored;
    }

    public class RunManager : MonoBehaviour
    {
        public static event Action<RunState, RunState> OnRunStateChanged;
        public static event Action OnMilestoneReached;
        public static event Action<RunStatistics> OnRunEnded;

        [Header("Tuning")]
        [SerializeField] int _milestoneInterval = 8;
        [SerializeField] int _roomsPerAct = 15;
        [SerializeField] float _difficultyPerRoom = 0.1f;

        public RunState CurrentRunState { get; private set; } = RunState.Inactive;
        public int CurrentAct { get; private set; } = 1;
        public int CurrentRoom { get; private set; }
        public int TotalRoomsCleared { get; private set; }
        public float RunTimer { get; private set; }
        public float CurrentDifficulty => 1f + (CurrentRoom * _difficultyPerRoom);

        public RunStatistics Stats { get; private set; } = new();

        int _seed;

        void Awake()
        {
            ServiceLocator.Register(this);
        }

        void Update()
        {
            if (CurrentRunState == RunState.DungeonPhase || CurrentRunState == RunState.BossPhase)
                RunTimer += Time.deltaTime;
        }

        public void StartNewRun()
        {
            _seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
            CurrentAct = 1;
            CurrentRoom = 0;
            TotalRoomsCleared = 0;
            RunTimer = 0f;
            Stats = new RunStatistics();

            TransitionTo(RunState.Starting);

            // Deactivate any leftover pacts
            if (ServiceLocator.TryGet<PactManager>(out var pact))
                pact.DeactivateAll();

            // Go to pact selection
            TransitionTo(RunState.PactSelection);
        }

        public void OnPactSelected()
        {
            TransitionTo(RunState.DungeonPhase);

            // Generate and build dungeon
            if (ServiceLocator.TryGet<DungeonGenerator>(out var gen))
            {
                var layout = gen.Generate(GetSubSeed("dungeon"));
                if (ServiceLocator.TryGet<DungeonManager>(out var dungeonMgr))
                    dungeonMgr.BuildDungeon(layout, gen.FixedLayout);
            }

            // Resume gameplay
            if (ServiceLocator.TryGet<GameManager>(out var gm))
                gm.RequestStateChange(GameState.Playing);
        }

        public void OnRoomEntered(int roomIndex, RoomType type)
        {
            CurrentRoom = roomIndex;
            Stats.RoomsExplored++;

            if (type == RoomType.Boss)
                TransitionTo(RunState.BossPhase);
        }

        public void OnRoomCleared()
        {
            TotalRoomsCleared++;
            CurrentRoom++;
            Stats.RoomsExplored++;

            // Milestone check
            if (TotalRoomsCleared > 0 && TotalRoomsCleared % _milestoneInterval == 0)
            {
                OnMilestoneReached?.Invoke();
                TransitionTo(RunState.PactSelection);
                return;
            }

            // Act boss check
            if (CurrentRoom >= _roomsPerAct)
            {
                TransitionTo(RunState.BossPhase);
            }
        }

        public void OnPlayerDied()
        {
            TransitionTo(RunState.RunEnd);
            OnRunEnded?.Invoke(Stats);
        }

        public void OnBossDefeated()
        {
            CurrentAct++;
            CurrentRoom = 0;

            if (CurrentAct > 3)
            {
                // Game won
                TransitionTo(RunState.RunEnd);
                OnRunEnded?.Invoke(Stats);
            }
            else
            {
                TransitionTo(RunState.PactSelection);
            }
        }

        public void RecordKill() => Stats.KillCount++;
        public void RecordDamageTaken(int amount) => Stats.DamageTaken += amount;
        public void RecordDamageDealt(int amount) => Stats.DamageDealt += amount;

        public int CalculateRunScore()
        {
            int score = (Stats.KillCount * 10) + (Stats.RoomsExplored * 50) - (Stats.DamageTaken * 2);
            int timeBonus = Mathf.Max(0, 5000 - Mathf.FloorToInt(RunTimer * 3.3f));
            return score + timeBonus;
        }

        public int GetSubSeed(string systemName)
        {
            return _seed ^ systemName.GetHashCode();
        }

        void TransitionTo(RunState newState)
        {
            if (newState == CurrentRunState) return;
            var old = CurrentRunState;
            CurrentRunState = newState;
            OnRunStateChanged?.Invoke(old, newState);
            Debug.Log($"[RunManager] {old} → {newState}");
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace DarkPact.Core
{
    public class PactManager : MonoBehaviour
    {
        [SerializeField] PactDefinition[] _allPacts;

        readonly HashSet<PactId> _activePacts = new();

        public bool IsKatliamActive => _activePacts.Contains(PactId.Katliam);
        public bool IsKanKalkaniActive => _activePacts.Contains(PactId.KanKalkani);
        public bool IsGolgeAdimiActive => _activePacts.Contains(PactId.GolgeAdimi);
        public bool IsLanetliDokunusActive => _activePacts.Contains(PactId.LanetliDokunus);
        public bool IsAcgozlulukActive => _activePacts.Contains(PactId.Acgozluluk);

        public IReadOnlyCollection<PactId> ActivePacts => _activePacts;

        void Awake()
        {
            ServiceLocator.Register(this);
        }

        public PactDefinition[] GetRandomPactOptions(int count, System.Random rng = null)
        {
            if (_allPacts == null || _allPacts.Length == 0) return System.Array.Empty<PactDefinition>();

            // Filter out already active pacts
            var available = new List<PactDefinition>();
            foreach (var p in _allPacts)
                if (!_activePacts.Contains(p.Id)) available.Add(p);

            if (available.Count == 0) return System.Array.Empty<PactDefinition>();

            // Shuffle and pick
            rng ??= new System.Random();
            for (int i = available.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (available[i], available[j]) = (available[j], available[i]);
            }

            int pickCount = Mathf.Min(count, available.Count);
            var result = new PactDefinition[pickCount];
            for (int i = 0; i < pickCount; i++) result[i] = available[i];
            return result;
        }

        public void ActivatePact(PactId id)
        {
            if (_activePacts.Contains(id)) return;
            _activePacts.Add(id);

            switch (id)
            {
                case PactId.Katliam:
                    ApplyKatliam();
                    break;
                case PactId.KanKalkani:
                    ApplyKanKalkani();
                    break;
                case PactId.GolgeAdimi:
                    ApplyGolgeAdimi();
                    break;
                case PactId.LanetliDokunus:
                    ApplyLanetliDokunus();
                    break;
                case PactId.Acgozluluk:
                    ApplyAcgozluluk();
                    break;
            }

            Debug.Log($"[Pact] {id} activated! Total active: {_activePacts.Count}");
        }

        public void DeactivateAll()
        {
            _activePacts.Clear();
            if (ServiceLocator.TryGet<PlayerController>(out var player))
            {
                player.DamageMultiplier = 1f;
                player.DashCooldownOverride = -1f;
            }
        }

        // === Katliam Paktı: +%60 hasar, düşmanlar 1 kez dirilir ===
        void ApplyKatliam()
        {
            if (ServiceLocator.TryGet<PlayerController>(out var player))
                player.DamageMultiplier *= 1.6f;
        }

        // === Kan Kalkanı: Her öldürme +5 TempHP, can iksiri yok ===
        void ApplyKanKalkani()
        {
            // OnKill hook — handled in Update/event
        }

        // === Gölge Adımı: Dash sınırsız (cooldown 0), durağan hasar ===
        void ApplyGolgeAdimi()
        {
            if (ServiceLocator.TryGet<PlayerController>(out var player))
                player.DashCooldownOverride = 0f;
        }

        // === Lanetli Dokunuş: Saldırılar zehirler, self-poison ===
        void ApplyLanetliDokunus()
        {
            // Poison on hit — handled in PlayerController attack
        }

        // === Açgözlülük: Altın %200, ekipman satın alınamaz ===
        void ApplyAcgozluluk()
        {
            // Loot modifier — handled in LootSystem
        }

        float _golgeTickTimer;

        void LateUpdate()
        {
            // Gölge Adımı tick (1 damage per 0.5s while standing)
            if (IsGolgeAdimiActive)
            {
                _golgeTickTimer -= Time.deltaTime;
                if (_golgeTickTimer <= 0)
                {
                    _golgeTickTimer = 0.5f;
                    if (ServiceLocator.TryGet<PlayerController>(out var p))
                    {
                        var rb = p.GetComponent<Rigidbody2D>();
                        if (rb != null && rb.linearVelocity.sqrMagnitude < 0.1f)
                        {
                            var health = p.GetComponent<Health>();
                            if (health != null && health.IsAlive)
                                health.ApplyDamage(2, 0, 1f, Vector2.zero);
                        }
                    }
                }
            }

            // Kan Kalkanı: on kill → +5 TempHP (tracked via RunManager kill count change)
        }
    }
}

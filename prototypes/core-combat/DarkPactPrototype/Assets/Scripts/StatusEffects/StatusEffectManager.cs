using System.Collections.Generic;
using UnityEngine;

namespace DarkPact.Core
{
    public class StatusEffectManager : MonoBehaviour
    {
        readonly List<StatusEffect> _activeEffects = new();
        Health _health;

        public IReadOnlyList<StatusEffect> ActiveEffects => _activeEffects;

        void Awake()
        {
            _health = GetComponent<Health>();
        }

        public void Apply(StatusEffect effect)
        {
            // Don't stack same type from same source — refresh duration
            for (int i = 0; i < _activeEffects.Count; i++)
            {
                if (_activeEffects[i].Type == effect.Type && _activeEffects[i].Source == effect.Source)
                {
                    _activeEffects[i] = effect;
                    effect.Start();
                    return;
                }
            }

            effect.Start();
            _activeEffects.Add(effect);
        }

        public void RemoveAll(StatusEffectType type)
        {
            _activeEffects.RemoveAll(e => e.Type == type);
        }

        public bool Has(StatusEffectType type)
        {
            foreach (var e in _activeEffects)
                if (e.Type == type) return true;
            return false;
        }

        public float GetSpeedMultiplier()
        {
            float mult = 1f;
            foreach (var e in _activeEffects)
            {
                if (e.Type == StatusEffectType.Slow)
                    mult *= e.ValuePerTick; // ValuePerTick = 0.5 means 50% speed
                else if (e.Type == StatusEffectType.SpeedBoost)
                    mult *= e.ValuePerTick;
            }
            return mult;
        }

        void Update()
        {
            if (_health == null) return;

            for (int i = _activeEffects.Count - 1; i >= 0; i--)
            {
                _activeEffects[i].Tick(Time.deltaTime, _health);
                if (_activeEffects[i].IsExpired)
                    _activeEffects.RemoveAt(i);
            }
        }
    }
}

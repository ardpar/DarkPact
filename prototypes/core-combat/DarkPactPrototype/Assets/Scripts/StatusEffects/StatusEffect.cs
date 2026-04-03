using UnityEngine;

namespace DarkPact.Core
{
    public enum StatusEffectType { Poison, Slow, Bleed, Shield, SpeedBoost }

    [System.Serializable]
    public class StatusEffect
    {
        public StatusEffectType Type;
        public float Duration;
        public float TickInterval;
        public float ValuePerTick; // damage for poison/bleed, multiplier for slow
        public object Source; // who applied it

        float _timer;
        float _tickTimer;

        public bool IsExpired => _timer <= 0;

        public void Start()
        {
            _timer = Duration;
            _tickTimer = TickInterval;
        }

        public bool Tick(float dt, Health target)
        {
            _timer -= dt;
            _tickTimer -= dt;

            if (_tickTimer <= 0 && TickInterval > 0)
            {
                _tickTimer = TickInterval;
                ApplyTick(target);
                return true; // ticked
            }
            return false;
        }

        void ApplyTick(Health target)
        {
            switch (Type)
            {
                case StatusEffectType.Poison:
                case StatusEffectType.Bleed:
                    target.ApplyDamage(Mathf.RoundToInt(ValuePerTick), 0, 1f, Vector2.zero);
                    break;
                case StatusEffectType.Shield:
                    target.AddTempHP(Mathf.RoundToInt(ValuePerTick));
                    break;
            }
        }
    }
}

using System;
using UnityEngine;

namespace DarkPact.Core
{
    public class Health : MonoBehaviour
    {
        public event Action<int, int> OnHealthChanged; // current, max
        public event Action<int, Vector2> OnDamaged; // amount, hitDirection
        public event Action OnDeath;

        [SerializeField] int _maxHP = 100;

        public int CurrentHP { get; private set; }
        public int MaxHP => _maxHP;
        public bool IsAlive => CurrentHP > 0;
        public bool IsInvulnerable { get; set; }

        int _temporaryHP;
        public int TemporaryHP => _temporaryHP;

        void Awake()
        {
            CurrentHP = _maxHP;
        }

        public void ApplyDamage(int baseDamage, int defense, float damageMultiplier, Vector2 hitDirection)
        {
            if (!IsAlive || IsInvulnerable) return;

            int finalDamage = Mathf.Max(1, Mathf.FloorToInt(baseDamage * damageMultiplier) - defense);

            // TempHP absorbs first
            if (_temporaryHP > 0)
            {
                int absorbed = Mathf.Min(_temporaryHP, finalDamage);
                _temporaryHP -= absorbed;
                finalDamage -= absorbed;
            }

            CurrentHP = Mathf.Max(0, CurrentHP - finalDamage);
            OnHealthChanged?.Invoke(CurrentHP, _maxHP);
            OnDamaged?.Invoke(finalDamage, hitDirection);

            if (CurrentHP <= 0)
                OnDeath?.Invoke();
        }

        public void Heal(int amount)
        {
            if (!IsAlive) return;
            CurrentHP = Mathf.Min(CurrentHP + amount, _maxHP);
            OnHealthChanged?.Invoke(CurrentHP, _maxHP);
        }

        public void AddTemporaryHP(int amount, int maxTempHP)
        {
            _temporaryHP = Mathf.Min(_temporaryHP + amount, maxTempHP);
            OnHealthChanged?.Invoke(CurrentHP, _maxHP);
        }

        public void AddTempHP(int amount) => AddTemporaryHP(amount, _maxHP);

        public void AddMaxHP(int amount)
        {
            _maxHP += amount;
            CurrentHP += amount;
            OnHealthChanged?.Invoke(CurrentHP, _maxHP);
        }

        public void ResetHealth()
        {
            CurrentHP = _maxHP;
            _temporaryHP = 0;
            OnHealthChanged?.Invoke(CurrentHP, _maxHP);
        }
    }
}

using UnityEngine;

namespace DarkPact.Core
{
    [RequireComponent(typeof(Health))]
    public class DamageNumberSpawner : MonoBehaviour
    {
        [SerializeField] GameObject _damageNumberPrefab;

        Health _health;

        void Awake()
        {
            _health = GetComponent<Health>();
            _health.OnDamaged += SpawnDamageNumber;
        }

        void OnDestroy()
        {
            if (_health != null)
                _health.OnDamaged -= SpawnDamageNumber;
        }

        void SpawnDamageNumber(int amount, Vector2 hitDirection)
        {
            if (_damageNumberPrefab == null) return;

            var go = Instantiate(_damageNumberPrefab);
            var dmgNum = go.GetComponent<DamageNumber>();
            if (dmgNum != null)
            {
                bool isCrit = amount >= 20;
                dmgNum.Setup(amount, (Vector2)transform.position, isCrit);
            }
        }
    }
}

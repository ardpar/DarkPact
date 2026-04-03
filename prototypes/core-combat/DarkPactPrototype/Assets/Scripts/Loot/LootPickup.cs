using UnityEngine;

namespace DarkPact.Core
{
    public class LootPickup : MonoBehaviour
    {
        [SerializeField] float _bobSpeed = 2f;
        [SerializeField] float _bobHeight = 0.15f;
        [SerializeField] float _magnetRange = 1.5f;
        [SerializeField] float _magnetSpeed = 8f;

        ItemDefinition _item;
        SpriteRenderer _sr;
        Vector2 _basePos;
        bool _collected;

        public void Setup(ItemDefinition item, Vector2 position)
        {
            _item = item;
            _basePos = position;
            transform.position = position;

            _sr = GetComponent<SpriteRenderer>();
            if (_sr != null)
            {
                _sr.sprite = item.Icon;
                _sr.color = item.RarityColor;
            }
        }

        void Update()
        {
            if (_collected || _item == null) return;

            // Bob animation
            float bob = Mathf.Sin(Time.time * _bobSpeed) * _bobHeight;
            Vector2 pos = _basePos + Vector2.up * bob;

            // Magnet toward player
            if (ServiceLocator.TryGet<PlayerController>(out var player))
            {
                float dist = Vector2.Distance(transform.position, player.transform.position);
                if (dist < _magnetRange)
                {
                    pos = Vector2.MoveTowards(pos, player.transform.position, _magnetSpeed * Time.deltaTime);

                    if (dist < 0.3f)
                    {
                        Collect(player);
                        return;
                    }
                }
            }

            transform.position = pos;
        }

        void Collect(PlayerController player)
        {
            _collected = true;
            ApplyStat(player);
            Destroy(gameObject);
        }

        void ApplyStat(PlayerController player)
        {
            var health = player.GetComponent<Health>();

            switch (_item.BonusStat)
            {
                case StatType.MaxHP:
                    if (health != null) health.AddMaxHP(Mathf.RoundToInt(_item.BonusValue));
                    break;
                case StatType.AttackDamage:
                    player.DamageMultiplier += _item.BonusValue / 100f;
                    break;
                case StatType.MoveSpeed:
                    // Would need a speed multiplier on PlayerController
                    break;
                case StatType.CritChance:
                    // Would need a crit modifier on PlayerController
                    break;
                case StatType.Defense:
                    // Would need defense on Health
                    break;
            }

            Debug.Log($"[Loot] Collected {_item.ItemName} ({_item.Rarity}): +{_item.BonusValue} {_item.BonusStat}");
        }
    }
}

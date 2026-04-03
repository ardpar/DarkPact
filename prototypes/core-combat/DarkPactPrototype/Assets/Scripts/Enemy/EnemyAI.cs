using UnityEngine;

namespace DarkPact.Core
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Health))]
    public class EnemyAI : MonoBehaviour
    {
        [Header("Stats")]
        [SerializeField] float _moveSpeed = 3f;
        [SerializeField] float _detectionRange = 6f;
        [SerializeField] float _attackRange = 1.2f;
        [SerializeField] int _attackDamage = 10;
        [SerializeField] float _attackCooldown = 1.5f;
        [SerializeField] float _telegraphDuration = 0.3f;

        Rigidbody2D _rb;
        Health _health;
        SpriteRenderer _sprite;
        Transform _player;

        enum State { Idle, Chase, Telegraph, Attack, Dead }
        State _state = State.Idle;

        float _attackCooldownTimer;
        float _telegraphTimer;
        float _attackAnimTimer;

        // Respawn (Katliam Paktı)
        bool _hasRespawned;
        public bool CanRespawn { get; set; }
        public bool IsRespawnKill => _hasRespawned;

        // A* pathfinding
        System.Collections.Generic.List<Vector2> _path;
        int _pathIndex;
        float _pathRecalcTimer;
        const float PathRecalcInterval = 0.3f;

        public event System.Action<EnemyAI> OnEnemyDied;

        void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _health = GetComponent<Health>();
            _sprite = GetComponentInChildren<SpriteRenderer>();

            _health.OnDeath += HandleDeath;
            _health.OnDamaged += OnDamaged;
        }

        void OnDestroy()
        {
            _health.OnDeath -= HandleDeath;
            _health.OnDamaged -= OnDamaged;
        }

        void Update()
        {
            if (_state == State.Dead) return;

            _attackCooldownTimer -= Time.deltaTime;

            if (_player == null)
            {
                var pc = FindAnyObjectByType<PlayerController>();
                if (pc != null) _player = pc.transform;
                else return;
            }

            float dist = Vector2.Distance(transform.position, _player.position);

            switch (_state)
            {
                case State.Idle:
                    _rb.linearVelocity = Vector2.zero;
                    if (dist <= _detectionRange)
                        _state = State.Chase;
                    break;

                case State.Chase:
                    Vector2 dir = GetMoveDirection();
                    _rb.linearVelocity = dir * _moveSpeed;
                    UpdateSpriteDirection(dir);

                    if (dist <= _attackRange && _attackCooldownTimer <= 0)
                    {
                        _state = State.Telegraph;
                        _telegraphTimer = _telegraphDuration;
                        _rb.linearVelocity = Vector2.zero;
                    }
                    else if (dist > _detectionRange * 1.5f)
                    {
                        _state = State.Idle;
                    }
                    break;

                case State.Telegraph:
                    _telegraphTimer -= Time.deltaTime;
                    // Flash red during telegraph
                    if (_sprite) _sprite.color = Color.Lerp(Color.white, Color.red, Mathf.PingPong(Time.time * 10, 1));
                    if (_telegraphTimer <= 0)
                    {
                        PerformAttack();
                        _state = State.Attack;
                        _attackAnimTimer = 0.2f;
                    }
                    break;

                case State.Attack:
                    _attackAnimTimer -= Time.deltaTime;
                    if (_attackAnimTimer <= 0)
                    {
                        _state = State.Chase;
                        if (_sprite) _sprite.color = Color.white;
                    }
                    break;
            }
        }

        void PerformAttack()
        {
            _attackCooldownTimer = _attackCooldown;
            if (_sprite) _sprite.color = Color.white;

            if (_player == null) return;
            float dist = Vector2.Distance(transform.position, _player.position);
            if (dist > _attackRange * 1.5f) return;

            var playerHealth = _player.GetComponent<Health>();
            if (playerHealth != null && playerHealth.IsAlive)
            {
                Vector2 hitDir = ((Vector2)_player.position - (Vector2)transform.position).normalized;
                playerHealth.ApplyDamage(_attackDamage, 0, 1f, hitDir);
            }
        }

        void HandleDeath()
        {
            if (CanRespawn && !_hasRespawned)
            {
                _hasRespawned = true;
                Invoke(nameof(Respawn), 2f);
                return;
            }

            _state = State.Dead;
            _rb.linearVelocity = Vector2.zero;
            if (_sprite) _sprite.color = new Color(1, 1, 1, 0.3f);

            // Track kill in RunManager
            if (ServiceLocator.TryGet<RunManager>(out var run))
                run.RecordKill();

            // Kan Kalkanı: +5 TempHP on kill
            if (ServiceLocator.TryGet<PactManager>(out var pact) && pact.IsKanKalkaniActive)
            {
                if (ServiceLocator.TryGet<PlayerController>(out var player))
                    player.GetComponent<Health>()?.AddTempHP(5);
            }

            // Loot drop
            var dropper = GetComponent<LootDropper>();
            if (dropper != null) dropper.DropLoot(transform.position);

            OnEnemyDied?.Invoke(this);
            Destroy(gameObject, 1f);
        }

        void Respawn()
        {
            _health.ResetHealth();
            _health.ApplyDamage(_health.MaxHP / 2, 0, 1f, Vector2.zero); // respawn at 50% HP
            _state = State.Chase;
            if (_sprite) _sprite.color = new Color(0.8f, 0.2f, 0.8f); // purple tint = respawned
        }

        void OnDamaged(int amount, Vector2 dir)
        {
            // Knockback — GDD formula: baseKnockback × (1 + damageDealt / 100)
            float baseKnockback = 0.5f; // Kılıç default
            float knockbackForce = baseKnockback * (1f + amount / 100f);
            _rb.AddForce(dir * knockbackForce, ForceMode2D.Impulse);
            if (_sprite) StartCoroutine(FlashWhite());
        }

        System.Collections.IEnumerator FlashWhite()
        {
            if (_sprite == null) yield break;
            var origColor = _sprite.color;
            _sprite.color = Color.white;
            yield return new WaitForSeconds(0.1f);
            if (_sprite) _sprite.color = origColor;
        }

        Vector2 GetMoveDirection()
        {
            if (_player == null) return Vector2.zero;

            // Try A* pathfinding
            _pathRecalcTimer -= Time.deltaTime;
            if (_pathRecalcTimer <= 0 || _path == null)
            {
                _pathRecalcTimer = PathRecalcInterval;
                if (ServiceLocator.TryGet<AStarPathfinder>(out var pathfinder))
                {
                    _path = pathfinder.FindPath(transform.position, _player.position);
                    _pathIndex = 0;
                }
            }

            if (_path != null && _pathIndex < _path.Count)
            {
                Vector2 waypoint = _path[_pathIndex];
                float distToWaypoint = Vector2.Distance(transform.position, waypoint);
                if (distToWaypoint < 0.5f)
                    _pathIndex++;

                if (_pathIndex < _path.Count)
                    return (_path[_pathIndex] - (Vector2)transform.position).normalized;
            }

            // Fallback: direct chase
            return ((Vector2)_player.position - (Vector2)transform.position).normalized;
        }

        void UpdateSpriteDirection(Vector2 dir)
        {
            if (_sprite == null) return;
            if (dir.x < -0.1f) _sprite.flipX = true;
            else if (dir.x > 0.1f) _sprite.flipX = false;
        }
    }
}

using System.Collections;
using UnityEngine;

namespace DarkPact.Core
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Health))]
    public class BossAI : MonoBehaviour
    {
        [Header("Stats")]
        [SerializeField] float _moveSpeed = 2f;
        [SerializeField] int _chargeDamage = 20;
        [SerializeField] int _stompDamage = 15;
        [SerializeField] float _chargeSpeed = 12f;
        [SerializeField] float _chargeDuration = 0.6f;
        [SerializeField] float _stompRadius = 2.5f;
        [SerializeField] float _attackCooldown = 2f;
        [SerializeField] float _telegraphDuration = 0.5f;

        Rigidbody2D _rb;
        Health _health;
        SpriteRenderer _sprite;
        Transform _player;
        Color _origColor;

        enum BossState { Idle, Chase, TelegraphCharge, Charging, TelegraphStomp, Stomping, Dead }
        BossState _state = BossState.Idle;

        float _cooldownTimer;
        float _stateTimer;
        Vector2 _chargeDir;
        int _attackPattern;

        public event System.Action OnBossDefeated;

        void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _health = GetComponent<Health>();
            _sprite = GetComponentInChildren<SpriteRenderer>();
            _origColor = _sprite ? _sprite.color : Color.white;

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
            if (_state == BossState.Dead) return;

            // Find player
            if (_player == null && ServiceLocator.TryGet<PlayerController>(out var pc))
                _player = pc.transform;
            if (_player == null) return;

            _cooldownTimer -= Time.deltaTime;
            float dist = Vector2.Distance(transform.position, _player.position);

            switch (_state)
            {
                case BossState.Idle:
                    if (dist < 10f) _state = BossState.Chase;
                    break;

                case BossState.Chase:
                    ChasePlayer();
                    if (_cooldownTimer <= 0 && dist < 6f)
                    {
                        _attackPattern = (_attackPattern + 1) % 3;
                        if (_attackPattern < 2) // 2/3 charge, 1/3 stomp
                            StartTelegraph(BossState.TelegraphCharge);
                        else
                            StartTelegraph(BossState.TelegraphStomp);
                    }
                    break;

                case BossState.TelegraphCharge:
                    _stateTimer -= Time.deltaTime;
                    if (_stateTimer <= 0) StartCharge();
                    break;

                case BossState.Charging:
                    _stateTimer -= Time.deltaTime;
                    if (_stateTimer <= 0)
                    {
                        _rb.linearVelocity = Vector2.zero;
                        _state = BossState.Chase;
                        _cooldownTimer = _attackCooldown;
                    }
                    break;

                case BossState.TelegraphStomp:
                    _stateTimer -= Time.deltaTime;
                    if (_stateTimer <= 0) PerformStomp();
                    break;

                case BossState.Stomping:
                    _stateTimer -= Time.deltaTime;
                    if (_stateTimer <= 0)
                    {
                        _state = BossState.Chase;
                        _cooldownTimer = _attackCooldown;
                    }
                    break;
            }
        }

        void ChasePlayer()
        {
            Vector2 dir = ((Vector2)_player.position - (Vector2)transform.position).normalized;
            _rb.linearVelocity = dir * _moveSpeed;
            if (_sprite) _sprite.flipX = dir.x < 0;
        }

        void StartTelegraph(BossState nextState)
        {
            _state = nextState;
            _stateTimer = _telegraphDuration;
            _rb.linearVelocity = Vector2.zero;
            if (_sprite) _sprite.color = Color.red; // telegraph flash

            if (nextState == BossState.TelegraphCharge)
                _chargeDir = ((Vector2)_player.position - (Vector2)transform.position).normalized;
        }

        void StartCharge()
        {
            _state = BossState.Charging;
            _stateTimer = _chargeDuration;
            _rb.linearVelocity = _chargeDir * _chargeSpeed;
            if (_sprite) _sprite.color = _origColor;
        }

        void PerformStomp()
        {
            _state = BossState.Stomping;
            _stateTimer = 0.3f;
            _rb.linearVelocity = Vector2.zero;
            if (_sprite) _sprite.color = _origColor;

            // AOE damage
            var hits = Physics2D.OverlapCircleAll(transform.position, _stompRadius, LayerMask.GetMask("Player"));
            foreach (var hit in hits)
            {
                var health = hit.GetComponent<Health>();
                if (health != null)
                {
                    Vector2 knockDir = ((Vector2)hit.transform.position - (Vector2)transform.position).normalized;
                    health.ApplyDamage(_stompDamage, 0, 1f, knockDir);
                }
            }

            // Screen shake
            if (ServiceLocator.TryGet<VFXManager>(out var vfx))
                vfx.ShakeCamera(0.6f, 0.2f);
        }

        void OnCollisionStay2D(Collision2D col)
        {
            if (_state != BossState.Charging) return;
            var health = col.gameObject.GetComponent<Health>();
            if (health != null && col.gameObject.layer == LayerMask.NameToLayer("Player"))
            {
                Vector2 knockDir = ((Vector2)col.transform.position - (Vector2)transform.position).normalized;
                health.ApplyDamage(_chargeDamage, 0, 1f, knockDir);
            }
        }

        void OnDamaged(int amount, Vector2 dir)
        {
            if (_sprite) StartCoroutine(FlashWhite());
        }

        IEnumerator FlashWhite()
        {
            if (_sprite == null) yield break;
            Color prev = _sprite.color;
            _sprite.color = Color.white;
            yield return new WaitForSeconds(0.1f);
            if (_sprite) _sprite.color = prev;
        }

        void HandleDeath()
        {
            _state = BossState.Dead;
            _rb.linearVelocity = Vector2.zero;
            if (_sprite) _sprite.color = new Color(1, 1, 1, 0.3f);

            if (ServiceLocator.TryGet<RunManager>(out var run))
            {
                run.RecordKill();
                run.OnBossDefeated();
            }

            OnBossDefeated?.Invoke();
            Destroy(gameObject, 2f);
        }
    }
}

using UnityEngine;
using UnityEngine.InputSystem;

namespace DarkPact.Core
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Health))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] float _moveSpeed = 5f;
        [SerializeField] float _dashSpeed = 15f;
        [SerializeField] float _dashDuration = 0.15f;
        [SerializeField] float _dashCooldown = 1f;

        [Header("Attack")]
        [SerializeField] float _attackRange = 1.5f;
        [SerializeField] float _attackArc = 90f;
        [SerializeField] int _attackDamage = 12;
        [SerializeField] float _attackCooldown = 0.3f;
        [SerializeField] LayerMask _enemyLayer;

        Rigidbody2D _rb;
        Health _health;
        SpriteRenderer _sprite;
        Animator _animator;

        Vector2 _moveInput;
        Vector2 _aimWorldPos;
        Vector2 _lastFacingDir = Vector2.right;

        // Dash
        bool _isDashing;
        float _dashTimer;
        float _dashCooldownTimer;
        Vector2 _dashDirection;

        // Attack
        float _attackCooldownTimer;
        bool _isAttacking;
        float _attackAnimTimer;
        const float AttackAnimDuration = 0.25f;

        // Stagger
        bool _isStaggered;
        float _staggerTimer;
        const float StaggerDuration = 0.2f;

        public float DamageMultiplier { get; set; } = 1f;
        public float DashCooldownOverride { get; set; } = -1f; // -1 = use default

        float EffectiveDashCooldown => DashCooldownOverride >= 0 ? DashCooldownOverride : _dashCooldown;

        void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _health = GetComponent<Health>();
            _sprite = GetComponentInChildren<SpriteRenderer>();
            _animator = GetComponentInChildren<Animator>();

            _health.OnDamaged += OnDamaged;
            _health.OnDeath += OnDeath;

            ServiceLocator.Register(this);
        }

        void OnDestroy()
        {
            _health.OnDamaged -= OnDamaged;
            _health.OnDeath -= OnDeath;
        }

        void Update()
        {
            if (_health == null || !_health.IsAlive) return;

            _dashCooldownTimer -= Time.deltaTime;
            _attackCooldownTimer -= Time.deltaTime;

            if (_isStaggered)
            {
                _staggerTimer -= Time.deltaTime;
                if (_staggerTimer <= 0) _isStaggered = false;
                return;
            }

            if (_isDashing)
            {
                _dashTimer -= Time.deltaTime;
                if (_dashTimer <= 0)
                {
                    _isDashing = false;
                    _health.IsInvulnerable = false;
                }
                return;
            }

            if (_isAttacking)
            {
                _attackAnimTimer -= Time.deltaTime;
                if (_attackAnimTimer <= 0) _isAttacking = false;
            }

            // Aim direction
            _aimWorldPos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            Vector2 aimDir = ((Vector2)_aimWorldPos - (Vector2)transform.position).normalized;
            if (aimDir.sqrMagnitude > 0.01f) _lastFacingDir = aimDir;

            UpdateSpriteDirection();
        }

        void FixedUpdate()
        {
            if (!_health.IsAlive) { _rb.linearVelocity = Vector2.zero; return; }
            if (_isStaggered) { _rb.linearVelocity = Vector2.zero; return; }

            if (_isDashing)
            {
                _rb.linearVelocity = _dashDirection * _dashSpeed;
                return;
            }

            float speedMod = _isAttacking ? 0.3f : 1f;
            _rb.linearVelocity = _moveInput.normalized * _moveSpeed * speedMod;
        }

        // Input System callbacks
        public void OnMove(InputAction.CallbackContext ctx)
        {
            _moveInput = ctx.ReadValue<Vector2>();
        }

        public void OnAttack(InputAction.CallbackContext ctx)
        {
            if (!ctx.performed) return;
            if (_isDashing || _isStaggered || !_health.IsAlive) return;
            if (_attackCooldownTimer > 0) return;

            PerformAttack();
        }

        public void OnDash(InputAction.CallbackContext ctx)
        {
            if (!ctx.performed) return;
            if (_isDashing || _isStaggered || !_health.IsAlive) return;
            if (_dashCooldownTimer > 0) return;

            PerformDash();
        }

        void PerformAttack()
        {
            _isAttacking = true;
            _attackAnimTimer = AttackAnimDuration;
            _attackCooldownTimer = _attackCooldown;

            if (_animator) _animator.SetTrigger("Attack");

            // Arc hitbox detection
            Vector2 origin = transform.position;
            float halfArc = _attackArc * 0.5f;

            var hits = Physics2D.OverlapCircleAll(origin, _attackRange, _enemyLayer);
            foreach (var hit in hits)
            {
                Vector2 dirToTarget = ((Vector2)hit.transform.position - origin).normalized;
                float angle = Vector2.Angle(_lastFacingDir, dirToTarget);

                if (angle <= halfArc)
                {
                    var enemyHealth = hit.GetComponent<Health>();
                    if (enemyHealth != null && enemyHealth.IsAlive)
                    {
                        int damage = Mathf.FloorToInt(_attackDamage * DamageMultiplier);
                        enemyHealth.ApplyDamage(damage, 0, 1f, _lastFacingDir);

                        // Track damage dealt
                        if (ServiceLocator.TryGet<RunManager>(out var run))
                            run.RecordDamageDealt(damage);

                        // Hitstop
                        HitstopManager.TriggerHitstop(0.05f);

                        // VFX hit spark
                        Vector2 hitPos = (origin + (Vector2)hit.transform.position) * 0.5f;
                        VFXManager.Instance?.PlayHitSpark(hitPos);
                    }
                }
            }
        }

        void PerformDash()
        {
            _isDashing = true;
            _dashTimer = _dashDuration;
            _dashCooldownTimer = EffectiveDashCooldown;
            _health.IsInvulnerable = true;

            _dashDirection = _moveInput.sqrMagnitude > 0.01f
                ? _moveInput.normalized
                : _lastFacingDir;

            _isAttacking = false; // dash cancels attack
        }

        void OnDamaged(int amount, Vector2 hitDir)
        {
            if (_isDashing) return;
            _isStaggered = true;
            _staggerTimer = StaggerDuration;
            _isAttacking = false;

            // White flash
            if (_sprite) StartCoroutine(FlashWhite());
        }

        void OnDeath()
        {
            _rb.linearVelocity = Vector2.zero;
            if (_animator) _animator.SetTrigger("Death");

            var gm = ServiceLocator.Get<GameManager>();
            gm.RequestStateChange(GameState.GameOver);
        }

        System.Collections.IEnumerator FlashWhite()
        {
            if (_sprite == null) yield break;
            _sprite.color = Color.white;
            yield return new WaitForSeconds(0.1f);
            if (_sprite) _sprite.color = Color.white; // reset to default
        }

        void UpdateSpriteDirection()
        {
            if (_sprite == null) return;
            if (_lastFacingDir.x < -0.1f) _sprite.flipX = true;
            else if (_lastFacingDir.x > 0.1f) _sprite.flipX = false;
        }
    }
}

using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float jumpForce = 12f; // Boosted slightly for better feel
    private bool _isFacingRight = true;
    public bool isGrounded;
    private int _jumpCount = 0; 
    private float _jumpCooldownTimer; // NEW: Prevents instant ground reset

    [Header("Detection")]
    public Transform _attackPoint;
    public float attackRadius = 0.5f;
    public LayerMask interactableLayer;

    [Header("Combat Settings")]
    public float attackCooldown = 2f;
    private bool _isBlocking = false;
    private bool _canAttack = true;

    [Header("Ground Check")]
    public Transform groundCheckPoint;
    public float groundRadius = 0.1f; // Keep this small
    public LayerMask groundLayer;
    
    [Header("Deflect Visuals")]
    [SerializeField] private SpriteRenderer deflectSprite; 
    [SerializeField] private float fadeSpeed = 5f;
    private Coroutine _fadeCoroutine;
    
    [Header("Knockback")]
    public float knockbackForce;
    public float upForceMultiplier = 1.5f;

    [Header("Zip Combat")]
    [SerializeField] private float detectionRadius = 5f; 
    [SerializeField] private float zipDuration = 0.1f;
    [SerializeField] private float airTime = 0.5f;
    private bool _isZipping = false;

    [Header("Fall Settings")]
    [SerializeField] private float slowFallGravity = 0.2f; 
    [SerializeField] private float maxSlowFallSpeed = 2f; 
    private float _originalGravityScale;

    private Vector2 _movement;
    private Rigidbody2D _rb;
    private PlayerInput _playerInput;
    private PlayerAnimations _playerAnim;
    private GravityManager _gravityManager;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _playerInput = GetComponent<PlayerInput>();
        _playerAnim = GetComponentInChildren<PlayerAnimations>();
        _gravityManager = GetComponent<GravityManager>();
        
        _originalGravityScale = Mathf.Abs(_rb.gravityScale);
    }

    private void OnEnable()
    {
        _playerInput.actions["Move"].performed += OnMove;
        _playerInput.actions["Move"].canceled += OnMove;
        _playerInput.actions["Jump"].started += OnJump;
        _playerInput.actions["Attack"].started += OnAttack;
        _playerInput.actions["Attack"].canceled += OnAttack; 
        _playerInput.actions["Deflect"].performed += OnDeflect;
        _playerInput.actions["Deflect"].canceled += OnDeflect;
        _playerInput.actions["Interact"].started += OnInteract;
    }

    private void OnDisable()
    {
        _playerInput.actions["Move"].performed -= OnMove;
        _playerInput.actions["Move"].canceled -= OnMove;
        _playerInput.actions["Jump"].started -= OnJump;
        _playerInput.actions["Attack"].started -= OnAttack;
        _playerInput.actions["Attack"].canceled -= OnAttack; 
        _playerInput.actions["Deflect"].performed -= OnDeflect;
        _playerInput.actions["Deflect"].canceled -= OnDeflect;
        _playerInput.actions["Interact"].started -= OnInteract;
    }

    public void OnMove(InputAction.CallbackContext context) => _movement = context.ReadValue<Vector2>();

    public void OnJump(InputAction.CallbackContext context)
    {
        if (!context.started || _isBlocking) return;

        // Logic: Jump if grounded OR if we are in the air and have only jumped once
        if (isGrounded || _jumpCount == 1)
        {
            _jumpCount++;
            _jumpCooldownTimer = 0.15f; // Block ground reset for 0.15s
            isGrounded = false;         // Force false so FixedUpdate doesn't reset it same frame

            // Gravity flip on 2nd jump
            if (_jumpCount == 2 && _gravityManager != null)
            {
                _gravityManager.ToggleGravity();
            }

            float gravityDir = Mathf.Sign(_rb.gravityScale);
            
            // Kill existing vertical velocity so the double jump feels snappy
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, 0); 
            _rb.AddForce(Vector2.up * jumpForce * gravityDir, ForceMode2D.Impulse);
        }
    }

    // ... (Keep OnDeflect, StartFade, FadeShield, OnAttack, ZipToEnemy as is) ...

    public void OnDeflect(InputAction.CallbackContext context)
    {
        if (context.performed) { _isBlocking = true; StartFade(0.5f); }
        else if (context.canceled) { _isBlocking = false; StartFade(0f); }
    }
    
    private void StartFade(float targetAlpha)
    {
        if (deflectSprite == null) return; 
        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        _fadeCoroutine = StartCoroutine(FadeShield(targetAlpha));
    }
    
    private IEnumerator FadeShield(float targetAlpha)
    {
        Color color = deflectSprite.color;
        float startAlpha = color.a;
        float time = 0;
        while (time < 1)
        {
            time += Time.deltaTime * fadeSpeed;
            color.a = Mathf.Lerp(startAlpha, targetAlpha, time);
            deflectSprite.color = color;
            yield return null;
        }
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (context.started && !_isBlocking && _canAttack)
        {
            _canAttack = false;
            Collider2D[] potentialTargets = Physics2D.OverlapCircleAll(transform.position, detectionRadius, interactableLayer);
            Transform bestTarget = null;

            foreach (var col in potentialTargets)
            {
                if (col.CompareTag("Enemy"))
                {
                    float gravityDir = Mathf.Sign(_rb.gravityScale);
                    bool enemyIsAhead = (gravityDir > 0) ? col.transform.position.y > (transform.position.y + 0.5f) : col.transform.position.y < (transform.position.y - 0.5f);
                    if (enemyIsAhead || _isZipping) { bestTarget = col.transform; break; }
                }
            }

            StartCoroutine(AttackCooldownRoutine((bestTarget != null || _isZipping) ? 0.1f : attackCooldown));

            if (bestTarget != null)
            {
                if (_gravityManager != null) _gravityManager.ToggleGravity();
                _zipTimer = airTime;
                if (_zipRoutine != null) StopCoroutine(_zipRoutine);
                _zipRoutine = StartCoroutine(ZipToEnemy(bestTarget));
            }
            if (_playerAnim != null) _playerAnim.PlayAttackLogic(_isZipping);
        }
    }

    private IEnumerator AttackCooldownRoutine(float delay) { yield return new WaitForSeconds(delay); _canAttack = true; }

    private float _zipTimer; 
    private Coroutine _zipRoutine; 
    private IEnumerator ZipToEnemy(Transform target)
    {
        _isZipping = true;
        _rb.bodyType = RigidbodyType2D.Kinematic;
        _rb.linearVelocity = Vector2.zero;
        if ((target.position.x - transform.position.x > 0 && !_isFacingRight) || (target.position.x - transform.position.x < 0 && _isFacingRight)) Flip();
        Vector3 targetPos = target.position + new Vector3((transform.position.x < target.position.x ? -0.8f : 0.8f), 0, 0);
        Vector3 startPos = transform.position;
        float elapsed = 0;
        while (elapsed < zipDuration) { transform.position = Vector3.Lerp(startPos, targetPos, elapsed / zipDuration); elapsed += Time.deltaTime; yield return null; }
        transform.position = targetPos;
        while (_zipTimer > 0) { _zipTimer -= Time.deltaTime; _rb.linearVelocity = Vector2.zero; yield return null; }
        _rb.bodyType = RigidbodyType2D.Dynamic;
        _rb.gravityScale = slowFallGravity * Mathf.Sign(_rb.gravityScale); 
        _isZipping = false;
        _zipRoutine = null;
    }

    public void TriggerAttackImpact() => DoNormalHitLogic();

    public IEnumerator HitStop(float duration)
    {
        float originalTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = originalTimeScale;
    }

    private void DoNormalHitLogic()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(_attackPoint.position, attackRadius, interactableLayer);
        foreach (Collider2D hit in hits)
        {
            if (hit.TryGetComponent(out IDamageable target))
            {
                target.Damage(1f);
                
                if (hit.TryGetComponent(out Rigidbody2D enemyRb))
                {
                    // --- NEW: Subtle flinch knockback ---
                    ApplyTinyFlinch(enemyRb);
                    // ------------------------------------

                    if (isGrounded) LaunchEnemy(enemyRb);
                    else ApplyAirJuggle(enemyRb);
                }
            }
        }
    }

    // Helper method for the "teeny tiny" knockback
    private void ApplyTinyFlinch(Rigidbody2D enemyRb)
    {
        // Calculate direction away from the player
        float dir = Mathf.Sign(enemyRb.transform.position.x - transform.position.x);
        
        // Reset velocity first for consistent feel
        enemyRb.linearVelocity = new Vector2(0, enemyRb.linearVelocity.y);
        
        // Apply a very small horizontal nudge (adjust 2f to your liking)
        enemyRb.AddForce(new Vector2(dir * 2f, 0f), ForceMode2D.Impulse);
    }

    public void TriggerAttackKnockback()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(_attackPoint.position, attackRadius, interactableLayer);
        bool hitGravityObject = false;
        foreach (Collider2D hit in hits)
        {
            if (hit.TryGetComponent(out IDamageable target))
            {
                target.Damage(1f);
                if (hit.TryGetComponent(out Rigidbody2D enemyRb)) 
                {
                    ApplyTinyFlinch(enemyRb); // Apply tiny flinch here too
                    ApplyKnockbackToObject(enemyRb);
                }
                if (hit.GetComponent<GravityManager>() != null) hitGravityObject = true;
            }
        }
        if (hitGravityObject && _gravityManager != null) _gravityManager.ToggleGravity();
    }

    private void LaunchEnemy(Rigidbody2D enemyRb)
    {
        enemyRb.linearVelocity = Vector2.zero;
        float dir = Mathf.Sign(enemyRb.transform.position.x - transform.position.x);
        enemyRb.AddForce(new Vector2(dir * 0.2f, 1.5f).normalized * knockbackForce * 1.5f, ForceMode2D.Impulse);
    }

    private void ApplyAirJuggle(Rigidbody2D enemyRb)
    {
        enemyRb.linearVelocity = Vector2.zero;
        enemyRb.AddForce(Vector2.up * 2f, ForceMode2D.Impulse);
    }

   
    private void ApplyKnockbackToObject(Rigidbody2D targetRb)
    {
        targetRb.linearVelocity = Vector2.zero; 
        targetRb.AddForce(new Vector2(Mathf.Sign(targetRb.transform.position.x - transform.position.x) * 0.5f, upForceMultiplier).normalized * knockbackForce, ForceMode2D.Impulse);
    }

    private void FixedUpdate()
    {
        // 1. Update Cooldown Timer
        if (_jumpCooldownTimer > 0) _jumpCooldownTimer -= Time.fixedDeltaTime;

        // 2. Perform Ground Check
        bool check = Physics2D.OverlapCircle(groundCheckPoint.position, groundRadius, groundLayer);

        // 3. Only set isGrounded if we aren't in the middle of a jump burst
        if (_jumpCooldownTimer <= 0)
        {
            isGrounded = check;
        }
        else
        {
            isGrounded = false;
        }
        
        if (isGrounded)
        {
            _jumpCount = 0; 
            if (!_isZipping)
            {
                _rb.gravityScale = _originalGravityScale * Mathf.Sign(_rb.gravityScale);
            }
        }

        if (!_isZipping && Mathf.Abs(_rb.gravityScale) < _originalGravityScale)
        {
            float clampedY = Mathf.Clamp(_rb.linearVelocity.y, -maxSlowFallSpeed, maxSlowFallSpeed);
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, clampedY);
        }

        float currentSpeed = _isBlocking ? 0 : moveSpeed;
        _rb.linearVelocity = new Vector2(_movement.x * currentSpeed, _rb.linearVelocity.y);

        if (!_isBlocking)
        {
            if (_movement.x > 0 && !_isFacingRight) Flip();
            else if (_movement.x < 0 && _isFacingRight) Flip();
        }
    }

    private void Flip()
    {
        _isFacingRight = !_isFacingRight;
        Vector3 localScale = transform.localScale;
        localScale.x *= -1f;
        transform.localScale = localScale;
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (context.started && !_isBlocking)
        {
            Collider2D[] potentialTargets = Physics2D.OverlapCircleAll(transform.position, attackRadius * 2f, interactableLayer);
            foreach (var col in potentialTargets)
            {
                if (col.CompareTag("Enemy") && col.TryGetComponent(out PatrolEnemy enemy)) 
                {
                    if (enemy.healthPoints > 5) {PerformSuperLaunch(col.gameObject); break; }
                }
            }
        }
    }

    private void PerformSuperLaunch(GameObject enemy)
    {
        if (enemy.TryGetComponent(out Rigidbody2D enemyRb))
        {
            enemyRb.linearVelocity = Vector2.zero;
            enemyRb.AddForce((_isFacingRight ? new Vector2(1, 0.5f) : new Vector2(-1, 0.5f)).normalized * 20f, ForceMode2D.Impulse);
            if (!enemy.GetComponent<SlamImpact>()) enemy.AddComponent<SlamImpact>().Initialize(10f);
        }
    }

    public bool IsBlocking() => _isBlocking;
    private void OnDrawGizmos() { if(groundCheckPoint != null) Gizmos.DrawWireSphere(groundCheckPoint.position, groundRadius); }
}
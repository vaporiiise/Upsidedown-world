using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float jumpForce = 5f;
    private bool _isFacingRight = true;
    public bool isGrounded;

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
    public float groundRadius = 0.2f;
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

    private bool _isHoldingAttack;
    private Rigidbody2D _rb;
    private PlayerInput _playerInput;
    private PlayerAnimations _playerAnim;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _playerInput = GetComponent<PlayerInput>();
        _playerAnim = GetComponentInChildren<PlayerAnimations>();
        
        // Store base gravity for resetting
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

    private Vector2 _movement;
    public void OnMove(InputAction.CallbackContext context) => _movement = context.ReadValue<Vector2>();

    public void OnDeflect(InputAction.CallbackContext context)
    {
        if (context.performed) 
        {
            _isBlocking = true;
            StartFade(0.5f); 
        }
        else if (context.canceled) 
        {
            _isBlocking = false;
            StartFade(0f); 
        }
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

    public void OnJump(InputAction.CallbackContext context)
    {
        if (isGrounded && !_isBlocking)
        {
            float gravityDir = Mathf.Sign(_rb.gravityScale);
            _rb.AddForce(Vector2.up * jumpForce * gravityDir, ForceMode2D.Impulse);
        }
    }

    private float _attackStartTime;
    public float heavyAttackThreshold = 0.5f;

    private float _zipTimer; 
    private Coroutine _zipRoutine; 

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
                    // GRAVITY-AWARE HEIGHT CHECK
                    float gravityDir = Mathf.Sign(_rb.gravityScale);
                    bool enemyIsAhead;

                    // If gravity is normal (down), look for enemies above
                    if (gravityDir > 0) 
                        enemyIsAhead = col.transform.position.y > (transform.position.y + 0.5f);
                    // If gravity is flipped (up), look for enemies below
                    else 
                        enemyIsAhead = col.transform.position.y < (transform.position.y - 0.5f);
                    
                    if (enemyIsAhead || _isZipping)
                    {
                        bestTarget = col.transform;
                        break;
                    }
                }
            }

            float cooldown = (bestTarget != null || _isZipping) ? 0.1f : attackCooldown;
            StartCoroutine(AttackCooldownRoutine(cooldown));

            if (bestTarget != null)
            {
                _zipTimer = airTime;
                if (_zipRoutine != null) StopCoroutine(_zipRoutine);
                _zipRoutine = StartCoroutine(ZipToEnemy(bestTarget));
            }

            if (_playerAnim != null) _playerAnim.PlayAttackLogic(_isZipping);
        }
    }

    private IEnumerator AttackCooldownRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        _canAttack = true;
    }

    private IEnumerator ZipToEnemy(Transform target)
    {
        _isZipping = true;
        _rb.bodyType = RigidbodyType2D.Kinematic;
        _rb.linearVelocity = Vector2.zero;

        // FLIP GRAVITY EVERY ZIP START
        if (TryGetComponent(out GravityManager gm))
        {
            gm.ToggleGravity();
        }

        // Face the enemy
        float directionToEnemy = target.position.x - transform.position.x;
        if ((directionToEnemy > 0 && !_isFacingRight) || (directionToEnemy < 0 && _isFacingRight))
        {
            Flip();
        }

        // Calculate position
        float side = (transform.position.x < target.position.x) ? -0.8f : 0.8f;
        Vector3 targetPos = target.position + new Vector3(side, 0, 0);

        // Movement
        Vector3 startPos = transform.position;
        float elapsed = 0;
        while (elapsed < zipDuration)
        {
            transform.position = Vector3.Lerp(startPos, targetPos, elapsed / zipDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = targetPos;

        // Hover logic
        while (_zipTimer > 0)
        {
            _zipTimer -= Time.deltaTime;
            _rb.linearVelocity = Vector2.zero; 
            yield return null;
        }

        // TRANSITION TO SLOW FALL
        _rb.bodyType = RigidbodyType2D.Dynamic;
        float currentSign = Mathf.Sign(_rb.gravityScale);
        _rb.gravityScale = slowFallGravity * currentSign; 

        _isZipping = false;
        _zipRoutine = null;
    }

    public void TriggerAttackImpact()
    {
        DoNormalHitLogic();
    }

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
                    // Allow juggling if zipping OR if we are currently in slow-fall
                    bool inAirState = _isZipping || Mathf.Abs(_rb.gravityScale) < _originalGravityScale;

                    if (isGrounded && !inAirState)
                    {
                        LaunchEnemy(enemyRb);
                    }
                    else if (inAirState)
                    {
                        ApplyAirJuggle(enemyRb);
                    }
                }
            }
        }
    }
    
    private void LaunchEnemy(Rigidbody2D enemyRb)
    {
        enemyRb.linearVelocity = Vector2.zero;
        float dir = Mathf.Sign(enemyRb.transform.position.x - transform.position.x);
        Vector2 launchVector = new Vector2(dir * 0.2f, 1.5f).normalized; 
        enemyRb.AddForce(launchVector * knockbackForce * 1.5f, ForceMode2D.Impulse);
    }

    private void ApplyAirJuggle(Rigidbody2D enemyRb)
    {
        enemyRb.linearVelocity = Vector2.zero;
        enemyRb.AddForce(Vector2.up * 2f, ForceMode2D.Impulse);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
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
                    ApplyKnockbackToObject(enemyRb);
                }
                if (hit.GetComponent<GravityManager>() != null) hitGravityObject = true;
            }
        }
        if (hitGravityObject)
        {
            if (TryGetComponent(out GravityManager myGravity)) myGravity.ToggleGravity();
        }
    }

    private void ApplyKnockbackToObject(Rigidbody2D targetRb)
    {
        float horizontalDir = Mathf.Sign(targetRb.transform.position.x - transform.position.x);
        Vector2 launchDirection = new Vector2(horizontalDir * 0.5f, upForceMultiplier).normalized;
        targetRb.linearVelocity = Vector2.zero; 
        targetRb.AddForce(launchDirection * knockbackForce, ForceMode2D.Impulse);
    }

    private IEnumerator AttackCooldownRoutine()
    {
        yield return new WaitForSeconds(attackCooldown);
        _canAttack = true;
    }

    private void FixedUpdate()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheckPoint.position, groundRadius, groundLayer);
        
        if (isGrounded && !_isZipping)
        {
            float currentSign = Mathf.Sign(_rb.gravityScale);
            _rb.gravityScale = _originalGravityScale * currentSign;
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
    
    [Header("Finisher Settings")]
    public float finisherLaunchForce = 20f;
    public float slamDamage = 10f;

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (context.started && !_isBlocking)
        {
            Collider2D[] potentialTargets = Physics2D.OverlapCircleAll(transform.position, attackRadius * 2f, interactableLayer);
            foreach (var col in potentialTargets)
            {
                if (col.CompareTag("Enemy"))
                {
                    if (col.TryGetComponent(out PatrolEnemy enemy)) 
                    {
                        if (enemy.healthPoints > 5)
                        {
                            enemy.PrepareForSlam(); 
                            PerformSuperLaunch(col.gameObject);
                            break; 
                        }
                    }
                }
            }
        }
    }

    private void PerformSuperLaunch(GameObject enemy)
    {
        if (enemy.TryGetComponent(out Rigidbody2D enemyRb))
        {
            Vector2 launchDir = _isFacingRight ? new Vector2(1, 0.5f) : new Vector2(-1, 0.5f);
            enemyRb.linearVelocity = Vector2.zero;
            enemyRb.AddForce(launchDir.normalized * finisherLaunchForce, ForceMode2D.Impulse);
            if (!enemy.GetComponent<SlamImpact>())
            {
                var slam = enemy.AddComponent<SlamImpact>();
                slam.Initialize(slamDamage);
            }
            Debug.Log("Super Launch Initiated!");
        }
    }

    public bool IsBlocking() => _isBlocking;
}
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
    
    

    private bool _isHoldingAttack;
    private Rigidbody2D _rb;
    private PlayerInput _playerInput;
    private PlayerAnimations _playerAnim;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _playerInput = GetComponent<PlayerInput>();
        _playerAnim = GetComponentInChildren<PlayerAnimations>();
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

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (context.started && !_isBlocking && _canAttack)
        {
            _canAttack = false;
            StartCoroutine(AttackCooldownRoutine());
        
            if (_playerAnim != null) _playerAnim.PlayAttack(); 

            TriggerAttackImpact(); 
        }
    }
    private IEnumerator HitResponse()
    {
        float originalScale = Time.timeScale;
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(0.07f); 
        Time.timeScale = originalScale;

    }
    [Header("Zip Combat")]
[SerializeField] private float detectionRadius = 5f; 
[SerializeField] private float zipDuration = 0.1f;
private bool _isZipping = false;

public void TriggerAttackImpact()
{
    Collider2D[] potentialTargets = Physics2D.OverlapCircleAll(transform.position, detectionRadius, interactableLayer);
    Transform bestTarget = null;

    foreach (var col in potentialTargets)
    {
        if (col.CompareTag("Enemy"))
        {
            bestTarget = col.transform;
            break; // Found one!
        }
    }

    if (bestTarget != null && !_isZipping)
    {
        Debug.Log("Floating Enemy Detected! Zipping...");
        StartCoroutine(ZipToEnemy(bestTarget));
    }
    else
    {
        DoNormalHitLogic();
    }
}

private void DoNormalHitLogic()
{
    Collider2D[] hits = Physics2D.OverlapCircleAll(_attackPoint.position, attackRadius, interactableLayer);
    
    foreach (Collider2D hit in hits)
    {
        if (hit.TryGetComponent(out IDamageable target))
        {
            target.Damage(1f);

            if (_isZipping && hit.CompareTag("Enemy"))
            {
                Debug.Log("Impact during Zip! Flipping Gravity.");
                if (TryGetComponent(out GravityManager gm)) gm.ToggleGravity();
            }
            else if (hit.GetComponent<GravityManager>() != null)
            {
                if (TryGetComponent(out GravityManager gm)) gm.ToggleGravity();
            }
        }
    }
}

private IEnumerator ZipToEnemy(Transform target)
{
    _isZipping = true;
    _rb.bodyType = RigidbodyType2D.Kinematic;
    _rb.linearVelocity = Vector2.zero;

    Vector3 startPos = transform.position;
    float side = (startPos.x < target.position.x) ? -0.8f : 0.8f;
    Vector3 targetPos = target.position + new Vector3(side, 0, 0);

    float elapsed = 0;
    while (elapsed < zipDuration)
    {
        transform.position = Vector3.Lerp(startPos, targetPos, elapsed / zipDuration);
        elapsed += Time.deltaTime;
        yield return null;
    }

    transform.position = targetPos;

    DoNormalHitLogic();

    yield return new WaitForSeconds(0.1f);
    _rb.bodyType = RigidbodyType2D.Dynamic;
    _isZipping = false;
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
    

    public bool IsBlocking() => _isBlocking;
}
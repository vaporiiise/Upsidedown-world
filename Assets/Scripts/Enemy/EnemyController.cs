using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyController : MonoBehaviour, IDamageable
{
    [Header("Movement")]
    public float moveSpeed = 4f;
    public float jumpForce = 12f;
    private bool _isFacingRight = true;
    public bool isGrounded;
    private int _jumpCount = 0;
    private float _jumpCooldownTimer;

    [Header("Detection & Combat")]
    public Transform _attackPoint;
    public float attackRadius = 0.6f;
    public float attackCooldown = 1.5f;
    public LayerMask targetLayer; // Set this to "Player"
    private bool _canAttack = true;

    [Header("Launch & Airtime")]
    public float healthPoints = 5f;
    public float airFreezeTime = 5f;
    private bool _isLaunched = false;

    [Header("Physics References")]
    public Transform groundCheckPoint;
    public float groundRadius = 0.15f;
    public LayerMask groundLayer;
    
    private Vector2 _movementInput;
    private Rigidbody2D _rb;
    private PlayerAnimations _enemyAnim; // Reusing your anim script
    private float _originalGravityScale;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _enemyAnim = GetComponentInChildren<PlayerAnimations>();
        _originalGravityScale = Mathf.Abs(_rb.gravityScale);
    }

    // --- AI CONTROL METHODS ---
    public void SetMoveInput(float horizontal) => _movementInput = new Vector2(horizontal, 0);
    
    public void TryJump()
    {
        if ((isGrounded || _jumpCount == 1) && !_isLaunched)
        {
            _jumpCount++;
            _jumpCooldownTimer = 0.15f;
            isGrounded = false;
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, 0);
            _rb.AddForce(Vector2.up * jumpForce * Mathf.Sign(_rb.gravityScale), ForceMode2D.Impulse);
        }
    }

    public void TryAttack()
    {
        if (_canAttack && !IsLaunchedState())
        {
            StartCoroutine(TelegraphedAttack());
        }
    }

    private IEnumerator TelegraphedAttack()
    {
        _canAttack = false;
        
        // Wind-up: Stop moving and "charge" (You can trigger a flash here)
        _movementInput = Vector2.zero;
        yield return new WaitForSeconds(0.4f); // This makes the attack "easily seen"

        if (_enemyAnim != null) _enemyAnim.PlayAttackLogic(false);
        PerformHitDetection();

        yield return new WaitForSeconds(attackCooldown);
        _canAttack = true;
    }

    private void PerformHitDetection()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(_attackPoint.position, attackRadius, targetLayer);
        foreach (Collider2D hit in hits)
        {
            if (hit.TryGetComponent(out IDamageable target))
                target.Damage(1f);
        }
    }

    // --- IDAMAGEABLE & LAUNCH LOGIC ---
    public void Damage(float damage)
    {
        healthPoints -= damage;
        if (healthPoints <= 0) { Destroy(gameObject); return; }
        
        if (!_isLaunched) StartCoroutine(GetLaunchedRoutine());
    }

    private IEnumerator GetLaunchedRoutine()
    {
        _isLaunched = true;
        _rb.bodyType = RigidbodyType2D.Dynamic;
        float gravityDir = Mathf.Sign(_rb.gravityScale);

        _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, 8f * gravityDir);
        yield return new WaitForSeconds(0.2f);

        _rb.linearVelocity = Vector2.zero;
        _rb.gravityScale = 0; // The "Airtime" freeze
        
        yield return new WaitForSeconds(airFreezeTime);

        _rb.gravityScale = _originalGravityScale * gravityDir;
        _isLaunched = false;
    }

    public bool IsLaunchedState() => _isLaunched;

    private void FixedUpdate()
    {
        if (_isLaunched) return;

        if (_jumpCooldownTimer > 0) _jumpCooldownTimer -= Time.fixedDeltaTime;
        isGrounded = Physics2D.OverlapCircle(groundCheckPoint.position, groundRadius, groundLayer) && _jumpCooldownTimer <= 0;

        if (isGrounded) _jumpCount = 0;

        _rb.linearVelocity = new Vector2(_movementInput.x * moveSpeed, _rb.linearVelocity.y);

        if (_movementInput.x > 0 && !_isFacingRight) Flip();
        else if (_movementInput.x < 0 && _isFacingRight) Flip();
    }

    private void Flip()
    {
        _isFacingRight = !_isFacingRight;
        transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);
    }
}
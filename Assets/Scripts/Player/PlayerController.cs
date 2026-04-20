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
        _playerInput.actions["Deflect"].performed += OnDeflect;
        _playerInput.actions["Deflect"].canceled += OnDeflect;
    }

    private void OnDisable()
    {
        _playerInput.actions["Move"].performed -= OnMove;
        _playerInput.actions["Jump"].started -= OnJump;
        _playerInput.actions["Attack"].started -= OnAttack;
        _playerInput.actions["Deflect"].performed -= OnDeflect;
    }

    private Vector2 _movement;
    public void OnMove(InputAction.CallbackContext context) => _movement = context.ReadValue<Vector2>();

    public void OnDeflect(InputAction.CallbackContext context)
    {
        if (context.performed) 
        {
            _isBlocking = true;
            StartFade(0.5f); // Fade shield in
            // (Optional) Tell the Animator if you have a block stance
            // if (_playerAnim != null) _anim.SetBool("isBlocking", true); 
        }
        else if (context.canceled) 
        {
            _isBlocking = false;
            StartFade(0f); // Fade shield out
            // if (_playerAnim != null) _anim.SetBool("isBlocking", false);
        }
    }
    
    private void StartFade(float targetAlpha)
    {
        if (deflectSprite == null) return; // Safety check
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

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (context.started && !_isBlocking && _canAttack)
        {
            _canAttack = false;
            StartCoroutine(AttackCooldownRoutine());
            _playerAnim.PlayAttack();
        }
    }

    public void TriggerAttackImpact()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(_attackPoint.position, attackRadius, interactableLayer);
        bool hitGravityObject = false;

        foreach (Collider2D hit in hits)
        {
            if (hit.TryGetComponent(out IDamageable target))
            {
                target.Damage(1f);
                if (hit.GetComponent<GravityManager>() != null) hitGravityObject = true;
            }
        }

        if (hitGravityObject)
        {
            if (TryGetComponent(out GravityManager myGravity)) myGravity.ToggleGravity();
        }
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
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class PatrolEnemy : MonoBehaviour, IDamageable
{
    [Header("Patrol Settings")]
    public Transform pointA;
    public Transform pointB;
    public float patrolSpeed = 2f;
    public float chaseSpeed = 4f;
    private Vector3 _target;
    private bool _isFacingRight = true;

    [Header("Detection Settings")]
    public float detectionRadius = 5f;
    public float verticalThreshold = 2.0f; 
    public LayerMask playerLayer;

    [Header("Combat Stats")]
    public float healthPoints = 5f;
    public float attackRange = 2.0f; 
    public float contactDamage = 1f;
    public float attackInterval = 1.5f; 
    private float _nextAttackTime;

    [Header("Launch Settings")]
    [SerializeField] private float launchForce = 5f;
    private bool _isLaunched = false;
    
    [Header("Advanced AI Settings")]
    public float jumpForce = 10f;
    public float gravityFlipCooldown = 1.5f;
    private float _gravityTimer;
    private bool _canJump = true;
    
    private Rigidbody2D _rb;
    private bool _canFlipGravity = true;
    private Transform _playerTransform;
    private EnemyAnimations _anim; 
    private CameraShakeTrigger _shakeTrigger;
    public AnimationCurve launchCurve;
    public float launchDuration;
    public float launchHeight;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _anim = GetComponent<EnemyAnimations>(); 
        _shakeTrigger = Object.FindFirstObjectByType<CameraShakeTrigger>();
        launchCurve = new AnimationCurve();
        
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) _playerTransform = playerObj.transform;
    }

    void Start()
    {
        if (pointB != null) _target = pointB.position;
        else if (pointA != null) _target = pointA.position;
    }

    void Update()
    {
        if (_isLaunched || _playerTransform == null) return;

        if (_gravityTimer > 0) _gravityTimer -= Time.deltaTime;

        float distanceToPlayer = Vector2.Distance(transform.position, _playerTransform.position);

        if (distanceToPlayer <= attackRange)
        {
            _rb.linearVelocity = new Vector2(0, _rb.linearVelocity.y);
            FaceTarget(_playerTransform.position.x);
            HandleCombatLogic(); 
        }
        else
        {
            HandleBehavior();
            CheckVerticalNavigation();
        }
    }
    
    private void CheckVerticalNavigation()
    {
        float yDiff = _playerTransform.position.y - transform.position.y;
        float gravityDir = Mathf.Sign(_rb.gravityScale);


        bool playerIsAbove = (gravityDir > 0) ? yDiff > verticalThreshold : yDiff < -verticalThreshold;

        if (playerIsAbove)
        {
            if (Mathf.Abs(yDiff) > verticalThreshold * 2f && _gravityTimer <= 0)
            {
                ToggleEnemyGravity();
            }
            else if (_canJump && IsGrounded())
            {
                StartCoroutine(EnemyJumpRoutine());
            }
        }
    }
    
    private void ToggleEnemyGravity()
    {
        if (TryGetComponent(out GravityManager gm))
        {
            gm.ToggleGravity();
            _gravityTimer = gravityFlipCooldown;
        }
    }

    private IEnumerator EnemyJumpRoutine()
    {
        _canJump = false;
        float gravityDir = Mathf.Sign(_rb.gravityScale);
        
        _rb.AddForce(Vector2.up * jumpForce * -gravityDir, ForceMode2D.Impulse);
        
        yield return new WaitForSeconds(2.0f); 
        _canJump = true;
    }

    private bool IsGrounded()
    {
        float gravityDir = Mathf.Sign(_rb.gravityScale);
    
        Vector2 checkDir = (gravityDir > 0) ? Vector2.down : Vector2.up;
    
        Vector2 origin = (Vector2)transform.position + (checkDir * 0.5f);

        RaycastHit2D hit = Physics2D.CircleCast(origin, 0.3f, checkDir, 0.2f, LayerMask.GetMask("Ground"));
    
        Debug.DrawRay(origin, checkDir * 0.5f, hit.collider != null ? Color.green : Color.blue);
    
        return hit.collider != null;
    }

    private void HandleBehavior()
    {
        float distanceToPlayer = Vector2.Distance(transform.position, _playerTransform.position);
        float xDiff = Mathf.Abs(transform.position.x - _playerTransform.position.x);

        if (distanceToPlayer <= detectionRadius)
        {
            float yDiff = Mathf.Abs(transform.position.y - _playerTransform.position.y);

            if (yDiff <= verticalThreshold) 
            {

                if (xDiff > 0.8f) 
                {
                    ChasePlayer();
                }
            }
            else 
            {
                MoveTowardsX(_playerTransform.position.x, chaseSpeed);
            }
        }
        else
        {
            NormalPatrol();
        }
    }

    private void HandleCombatLogic()
    {
        float distanceToPlayer = Vector2.Distance(transform.position, _playerTransform.position);
        if (distanceToPlayer > attackRange) return;

        if (_playerTransform.TryGetComponent(out PlayerController pc))
        {
            if (pc.IsBlocking())
            {
                if (Time.time >= _nextAttackTime) 
                {
                    SuccessfulDeflectHeal();
                }
            
                if (_canFlipGravity) CheckForPlayerBlock();
            }
            else
            {
                if (Time.time >= _nextAttackTime) HandleRaycastAttack();
            }
        }
    }

    private void SuccessfulDeflectHeal()
    {
        _nextAttackTime = Time.time + attackInterval;

        if (_playerTransform.TryGetComponent(out PlayerHealth ph))
        {
            ph.Heal(1f); 
        }
    
        if (_anim != null) _anim.PlayGotHit();
    }

    private void HandleRaycastAttack()
    {
        if (Time.time < _nextAttackTime) return;
        _nextAttackTime = Time.time + attackInterval;
        
        if(_anim != null) _anim.PlayAttack();
        
        Vector2 direction = (_playerTransform.position - transform.position).normalized;
        Vector2 rayStart = (Vector2)transform.position + (direction * 0.7f);

        RaycastHit2D hit = Physics2D.CircleCast(rayStart, 0.5f, direction, attackRange, playerLayer);
        
        Debug.DrawRay(rayStart, direction * attackRange, Color.red, 0.5f);

        if (hit.collider != null)
        {
            PlayerHealth player = hit.collider.GetComponentInParent<PlayerHealth>();

            if (player != null)
            {
                player.Damage(contactDamage);
                Debug.Log("<color=green>Direct Hit:</color> PlayerHealth found and damaged!");
            }
            else
            {
                Debug.LogWarning($"Hit {hit.collider.name} but it doesn't have PlayerHealth!");
            }
        }
    }

    private void FaceTarget(float targetX)
    {
        if (targetX > transform.position.x && !_isFacingRight) FlipSprite();
        else if (targetX < transform.position.x && _isFacingRight) FlipSprite();
    }

    private void MoveTowardsX(float targetX, float speed)
    {
        float newX = Mathf.MoveTowards(transform.position.x, targetX, speed * Time.deltaTime);
        transform.position = new Vector3(newX, transform.position.y, transform.position.z);
        FaceTarget(targetX);
    }

    private void NormalPatrol()
    {
        if (pointA == null || pointB == null) return;
        MoveTowardsX(_target.x, patrolSpeed);
        if (Mathf.Abs(transform.position.x - _target.x) < 0.1f)
        {
            _target = (_target.x == pointA.position.x) ? pointB.position : pointA.position;
        }
    }

    private void ChasePlayer()
    {
        MoveTowardsX(_playerTransform.position.x, chaseSpeed);
    }

    public void Damage(float damage)
    {
        healthPoints -= damage;
        if(_anim != null) _anim.PlayGotHit();

        if (healthPoints <= 0) { Die(); return; }
        
        if (!_isLaunched) 
        {
            StopAllCoroutines();
            StartCoroutine(GetLaunched());
        }
    }
    IEnumerator GetLaunched()
    {
        _isLaunched = true;
        
        // Store original state
        float originalGravity = _rb.gravityScale;
        Vector2 startPos = _rb.position;
        
        // Prepare Rigidbody
        _rb.gravityScale = 0;
        _rb.linearVelocity = Vector2.zero;

        float elapsed = 0f;
        while (elapsed < launchDuration)
        {
            elapsed += Time.deltaTime;
            float percent = elapsed / launchDuration;

            // Evaluate the curve (0 to 1) and multiply by height
            float curveValue = launchCurve.Evaluate(percent);
            float targetY = startPos.y + (curveValue * launchHeight);

            // MovePosition is better for physics-friendly interpolation
            _rb.MovePosition(new Vector2(_rb.position.x, targetY));

            yield return null;
        }

        // Reset
        _rb.gravityScale = originalGravity;
        _isLaunched = false;
    }
    

    private void CheckForPlayerBlock()
    {
        if (TryGetComponent(out GravityManager gm))
        {
            gm.ToggleGravity();
            StartCoroutine(BlockCooldown());
        }
    }

    private IEnumerator BlockCooldown()
    {
        _canFlipGravity = false;
        yield return new WaitForSeconds(1f);
        _canFlipGravity = true;
    }

    private void FlipSprite()
    {
        _isFacingRight = !_isFacingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    private void Die()
    {
        EnemySpawner spawner = Object.FindFirstObjectByType<EnemySpawner>();
    
        if (spawner != null)
        {
            spawner.AddScore(_isLaunched); 
        }

        SlowDownTime();

        if (_shakeTrigger != null) _shakeTrigger.ShakeCameraCustom(0.8f); 
        if (deathEffectPrefab != null) Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
    
        Destroy(gameObject);
    }

    private void SlowDownTime()
    {
        if (CombatCinematics.Instance != null)
        {
            CombatCinematics.Instance.TriggerKillEffect();
        }
    }
    public GameObject deathEffectPrefab; 

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
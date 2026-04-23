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
    
    private Rigidbody2D _rb;
    private bool _canFlipGravity = true;
    private Transform _playerTransform;
    private EnemyAnimations _anim; 
    private CameraShakeTrigger _shakeTrigger;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _anim = GetComponent<EnemyAnimations>(); // Ensure this is on the same object
        _shakeTrigger = Object.FindFirstObjectByType<CameraShakeTrigger>();
        
        // Find player by Tag
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
        // 1. If launched or no player, do absolutely nothing
        if (_isLaunched || _playerTransform == null) return;

        // 2. Decide Movement
        HandleBehavior();

        // 3. Handle Combat (Gravity Flip vs Shooting)
        HandleCombatLogic();
    }

    private void HandleBehavior()
    {
        float distanceToPlayer = Vector2.Distance(transform.position, _playerTransform.position);
        float xDiff = Mathf.Abs(transform.position.x - _playerTransform.position.x);

        if (distanceToPlayer <= detectionRadius)
        {
            float yDiff = Mathf.Abs(transform.position.y - _playerTransform.position.y);

            // Case A: Player is nearby and on roughly the same level
            if (yDiff <= verticalThreshold) 
            {
                // Only move if we aren't already hugging the player
                if (xDiff > 0.7f) ChasePlayer();
                else FaceTarget(_playerTransform.position.x);
            }
            // Case B: Player is above/below - move to their X but stay on current Y
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

        // Try to find the PlayerController script
        if (_playerTransform.TryGetComponent(out PlayerController pc))
        {
            // If they are blocking, flip gravity
            if (pc.IsBlocking())
            {
                if (_canFlipGravity) CheckForPlayerBlock();
            }
            // If they aren't blocking, try to shoot
            else
            {
                if (Time.time >= _nextAttackTime) HandleRaycastAttack();
            }
        }
    }

    private void HandleRaycastAttack()
    {
        _nextAttackTime = Time.time + attackInterval;
        
        if(_anim != null) _anim.PlayAttack();
        
        Vector2 direction = (_playerTransform.position - transform.position).normalized;
        Vector2 rayStart = (Vector2)transform.position;

        RaycastHit2D hit = Physics2D.Raycast(rayStart, direction, attackRange, playerLayer);
        Debug.DrawRay(rayStart, direction * attackRange, Color.red, 0.5f);

        if (hit.collider != null && !hit.collider.isTrigger)
        {
            if (hit.collider.TryGetComponent(out IDamageable victim))
            {
                victim.Damage(contactDamage);
                Debug.Log("Dealt damage to player!");
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
        
        // Launch logic
        if (!_isLaunched) 
        {
            StopAllCoroutines();
            StartCoroutine(GetLaunched());
        }
    }

    private IEnumerator GetLaunched()
    {
        _isLaunched = true; 
    
        // Ensure we are dynamic to receive forces
        _rb.bodyType = RigidbodyType2D.Dynamic;
    
        float gravityDir = Mathf.Sign(_rb.gravityScale);
        float originalGravity = _rb.gravityScale;

        // Initial launch up
        _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, launchForce * gravityDir);
    
        yield return new WaitForSeconds(0.2f); 

        // Instead of STATIC, we just kill gravity and velocity
        _rb.linearVelocity = Vector2.zero;
        _rb.gravityScale = 0; 

        // Wait 5 seconds - during this time, Player knockback will work!
        yield return new WaitForSeconds(5.0f); 

        // Reset back to normal
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
        if (_shakeTrigger != null) _shakeTrigger.ShakeCameraCustom(0.8f); 
        if (deathEffectPrefab != null) Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
        Destroy(gameObject);
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
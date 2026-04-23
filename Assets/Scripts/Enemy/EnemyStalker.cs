using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class StalkerEnemy : MonoBehaviour
{
    [Header("Movement")]
    public float chaseSpeed = 3.5f;
    public float stopDistance = 0.8f;
    public float detectionRadius = 8f;

    [Header("Combat")]
    public float damageValue = 1f;
    public float attackCooldown = 1.2f;
    private float _nextAttackTime;

    private Transform _player;
    private Rigidbody2D _rb;
    private EnemyAnimations _anim;
    private EnemyLaunchHandler _launcher;
    private bool _isFacingRight = true;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _anim = GetComponent<EnemyAnimations>();
        _launcher = GetComponent<EnemyLaunchHandler>();
        
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) _player = p.transform;
    }

    void Update()
    {
        // Don't move or attack if we are currently launched/frozen
        if (_launcher != null && _launcher.IsLaunched) return;
        if (_player == null) return;

        HandleMovement();
    }

    private void HandleMovement()
    {
        float distance = Vector2.Distance(transform.position, _player.position);

        if (distance <= detectionRadius && distance > stopDistance)
        {
            // Move towards player
            Vector2 target = new Vector2(_player.position.x, transform.position.y);
            transform.position = Vector2.MoveTowards(transform.position, target, chaseSpeed * Time.deltaTime);
            
            _anim?.PlayMove(true);
            FlipCheck(_player.position.x);
        }
        else
        {
            _anim?.PlayMove(false);
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (_launcher != null && _launcher.IsLaunched) return;

        if (collision.gameObject.CompareTag("Player") && Time.time >= _nextAttackTime)
        {
            if (collision.gameObject.TryGetComponent(out IDamageable health))
            {
                _anim?.PlayAttack();
                health.Damage(damageValue);
                _nextAttackTime = Time.time + attackCooldown;
            }
        }
    }

    private void FlipCheck(float targetX)
    {
        if (targetX > transform.position.x && !_isFacingRight) Flip();
        else if (targetX < transform.position.x && _isFacingRight) Flip();
    }

    private void Flip()
    {
        _isFacingRight = !_isFacingRight;
        transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);
    }
}
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class PatrolEnemy : MonoBehaviour, IDamageable
{
    [Header("Patrol Settings")]
    public Transform pointA;
    public Transform pointB;
    public float speed = 2f;
    private Vector3 _target;
    private bool _isFacingRight = true;

    [Header("Combat Stats")]
    public float healthPoints = 5f;
    public float attackRange = 1.5f;
    public LayerMask playerLayer;

    [Header("Launch Settings")]
    [SerializeField] private float launchForce = 5f;
    [SerializeField] private float hangTime = 0.5f;
    
    private Rigidbody2D _rb;
    private bool _canFlipGravity = true;
    private bool _isLaunched = false;
    private PlayerController _player;
    

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _shakeTrigger = Object.FindFirstObjectByType<CameraShakeTrigger>();
    }

    void Start()
    {
        _target = pointB.position;
    }

    void Update()
    {
        if (!_isLaunched)
        {
            Move();
            CheckForPlayerBlock();
        }
    }

    private void Move()
    {
        float newX = Mathf.MoveTowards(transform.position.x, _target.x, speed * Time.deltaTime);
        transform.position = new Vector3(newX, transform.position.y, transform.position.z);

        if (Mathf.Abs(transform.position.x - _target.x) < 0.1f)
        {
            _target = (_target.x == pointA.position.x) ? pointB.position : pointA.position;
            FlipSprite();
        }
    }
    
    public void PrepareForSlam()
    {
        if (_launchRoutine != null) StopCoroutine(_launchRoutine);
        _isLaunched = true; 
        _rb.bodyType = RigidbodyType2D.Dynamic; 
        _rb.linearVelocity = Vector2.zero;
    }

    private void CheckForPlayerBlock()
    {
        if (!_canFlipGravity) return;

        Collider2D player = Physics2D.OverlapCircle(transform.position, attackRange, playerLayer);
        if (player != null && player.TryGetComponent(out PlayerController pc))
        {
            if (pc.IsBlocking())
            {
                if (TryGetComponent(out GravityManager gm))
                {
                    gm.ToggleGravity();
                    StartCoroutine(BlockCooldown());
                }
            }
        }
    }

    private Coroutine _launchRoutine;

    public void Damage(float damage)
    {
        healthPoints -= damage;
        
        if (healthPoints <= 0) { Die(); return; }

        if (!_isLaunched) 
        {
            if (_launchRoutine != null) StopCoroutine(_launchRoutine);
            _launchRoutine = StartCoroutine(GetLaunched());
        }
    }

    private IEnumerator GetLaunched()
    {
        _isLaunched = true;
        float gravityDir = Mathf.Sign(_rb.gravityScale);
    
        _rb.bodyType = RigidbodyType2D.Dynamic;
        _rb.linearVelocity = new Vector2(0, launchForce * gravityDir);
    
        yield return new WaitForSeconds(0.15f); 

        _rb.linearVelocity = Vector2.zero;
        _rb.bodyType = RigidbodyType2D.Static; 

        yield return new WaitForSeconds(5.0f); 

        _rb.bodyType = RigidbodyType2D.Dynamic;
        _isLaunched = false;
        _launchRoutine = null;
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
    
    [Header("VFX")]
    public GameObject deathEffectPrefab; 
    private CameraShakeTrigger _shakeTrigger; 

    private void Die()
    {
        if (_shakeTrigger != null)
        {
            _shakeTrigger.ShakeCameraCustom(0.8f); 
        }

        PlayerController player = Object.FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
            player.StartCoroutine(player.HitStop(0.2f)); 
        
            if (player.TryGetComponent(out GravityManager gm))
            {
                gm.ToggleGravity();
            }
        }

        if (deathEffectPrefab != null)
        {
            Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
        }

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
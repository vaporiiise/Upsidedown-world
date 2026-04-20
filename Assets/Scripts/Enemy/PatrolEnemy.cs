using UnityEngine;
using System.Collections;

public class PatrolEnemy : MonoBehaviour, IDamageable
{
    public Transform pointA;
    public Transform pointB;
    public float speed = 2f;
    public float attackRange = 1f;
    public LayerMask playerLayer;
    
    private Vector3 _target;
    private bool _canFlip = true;

    void Start() => _target = pointB.position;

    void Update()
    {
        float newX = Mathf.MoveTowards(transform.position.x, _target.x, speed * Time.deltaTime);
        transform.position = new Vector3(newX, transform.position.y, transform.position.z);

        if (Mathf.Abs(transform.position.x - _target.x) < 0.1f)
        {
            _target = (_target.x == pointA.position.x) ? pointB.position : pointA.position;
            FlipSprite();
        }
        CheckForPlayer();
    }

    void CheckForPlayer()
    {
        if (!_canFlip) return;
        Collider2D player = Physics2D.OverlapCircle(transform.position, attackRange, playerLayer);
        if (player != null && player.TryGetComponent(out PlayerController pc))
        {
            if (pc.IsBlocking())
            {
                if (TryGetComponent(out GravityManager gm))
                {
                    gm.ToggleGravity();
                    StartCoroutine(FlipCooldown());
                }
            }
        }
    }

    private IEnumerator FlipCooldown()
    {
        _canFlip = false;
        yield return new WaitForSeconds(1f);
        _canFlip = true;
    }

    public void Damage(float damage) => Destroy(gameObject);

    void FlipSprite()
    {
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }
}
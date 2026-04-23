using UnityEngine;
using System.Collections;

public class EnemyLaunchHandler : MonoBehaviour, IDamageable
{
    [Header("Launch Stats")]
    public float health = 5f;
    public float launchForce = 6f;
    public float airFreezeTime = 3.0f;
    
    public bool IsLaunched { get; private set; }

    private Rigidbody2D _rb;
    private EnemyAnimations _anim;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _anim = GetComponent<EnemyAnimations>();
    }

    public void Damage(float amount)
    {
        health -= amount;
        _anim?.PlayGotHit();

        if (health <= 0)
        {
            Die();
        }
        else if (!IsLaunched)
        {
            StartCoroutine(LaunchRoutine());
        }
    }

    private IEnumerator LaunchRoutine()
    {
        IsLaunched = true;
        
        // Use the current gravity scale to know which way is "up"
        float dir = Mathf.Sign(_rb.gravityScale);
        
        _rb.bodyType = RigidbodyType2D.Dynamic;
        _rb.linearVelocity = new Vector2(0, launchForce * dir);

        // Short wait to reach peak of launch
        yield return new WaitForSeconds(0.2f);

        // Freeze in air
        _rb.linearVelocity = Vector2.zero;
        _rb.bodyType = RigidbodyType2D.Static;

        yield return new WaitForSeconds(airFreezeTime);

        // Drop back down
        _rb.bodyType = RigidbodyType2D.Dynamic;
        IsLaunched = false;
    }

    private void Die()
    {
        // Add death particles/sound here
        Destroy(gameObject);
    }
}
using UnityEngine;
using System.Collections;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("Health Settings")]
    public float maxHealth = 10f;
    public float currentHealth;

    [Header("I-Frames")]
    [SerializeField] private float iFrameDuration = 1.0f;
    private bool _isInvulnerable = false;

    [Header("Death Settings")]
    [SerializeField] private float deathDelay = 1.5f; 

    private PlayerAnimations _anim;
    private SpriteRenderer _sprite;
    private bool _isDead = false;

    void Awake()
    {
        currentHealth = maxHealth;
        _sprite = GetComponentInChildren<SpriteRenderer>();
        _anim = GetComponentInChildren<PlayerAnimations>();
    }

    public void Damage(float damage)
    {
        if (_isDead || _isInvulnerable) return;

        currentHealth -= damage;
        Debug.Log($"<color=magenta>Damage Taken! Remaining:</color> {currentHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            if (_anim != null) _anim.PlayGotHit();
            StartCoroutine(HandleIFrames());
        }
    }

    private IEnumerator HandleIFrames()
    {
        _isInvulnerable = true;
        float elapsed = 0;
        while (elapsed < iFrameDuration)
        {
            if (_sprite) _sprite.color = new Color(1, 1, 1, 0.2f);
            yield return new WaitForSeconds(0.1f);
            if (_sprite) _sprite.color = Color.white;
            yield return new WaitForSeconds(0.1f);
            elapsed += 0.2f;
        }
        _isInvulnerable = false;
    }

    private void Die()
    {
        if (_isDead) return;
        _isDead = true;
        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        if (_anim != null) _anim.PlayDeath();
        
        // Stop player from moving while dying
        if (TryGetComponent(out PlayerController pc)) pc.enabled = false;
        if (TryGetComponent(out Rigidbody2D rb)) rb.linearVelocity = Vector2.zero;

        yield return new WaitForSeconds(deathDelay);

        // Deactivate or Reload Scene
        gameObject.SetActive(false);
    }
}
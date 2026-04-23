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

    private PlayerAnimations _anim;

    private SpriteRenderer _sprite;

    void Awake()
    {
        currentHealth = maxHealth;
        _sprite = GetComponent<SpriteRenderer>();
        _anim = GetComponent<PlayerAnimations>();
    }

    public void Damage(float damage)
    {
        if (_isInvulnerable) return;

        currentHealth -= damage;
        
        _anim.PlayGotHit();
        
        Debug.Log($"Player hit! Health remaining: {currentHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            StartCoroutine(HandleIFrames());
        }
    }

    private IEnumerator HandleIFrames()
    {
        _isInvulnerable = true;
        
        // Simple flicker effect
        for (float i = 0; i < iFrameDuration; i += 0.2f)
        {
            _sprite.color = new Color(1, 1, 1, 0.2f);
            yield return new WaitForSeconds(0.1f);
            _sprite.color = Color.white;
            yield return new WaitForSeconds(0.1f);
        }

        _isInvulnerable = false;
    }

    private void Die()
    {
        Debug.Log("Player Died!");
        // Add your death logic here (reload scene, show UI, etc.)
        _anim.PlayDeath();
        
        gameObject.SetActive(false);
    }
}
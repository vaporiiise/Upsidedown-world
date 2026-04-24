using UnityEngine;
using System;

public class GravityManager : MonoBehaviour, IDamageable
{
    private Rigidbody2D _rb;
    private bool _isUpsideDown = false;
    private float _baseGravity;
    private float _initialScaleY;

    [SerializeField] private bool isFixedObject = false;

    // This is the "Global Radio Station"
    public static Action OnGlobalFlip;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _baseGravity = Mathf.Abs(_rb.gravityScale);
        _initialScaleY = transform.localScale.y;
    }

    // When the object is created, it starts listening for the flip
    private void OnEnable() => OnGlobalFlip += ToggleGravity;
    
    // When the object is destroyed, it stops listening
    private void OnDisable() => OnGlobalFlip -= ToggleGravity;

    public void Damage(float damage)
    {
        // If you want hitting an enemy to trigger a global flip, keep this:
        // OnGlobalFlip?.Invoke(); 
        
        // Otherwise, just kill the enemy here
    }

    public void ToggleGravity()
    {
        if (isFixedObject) return;

        _isUpsideDown = !_isUpsideDown;
        _rb.gravityScale = _isUpsideDown ? -_baseGravity : _baseGravity;

        Vector3 scale = transform.localScale;
        scale.y = _isUpsideDown ? -_initialScaleY : _initialScaleY;
        transform.localScale = scale;
        }
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (_rb.linearVelocity.magnitude > 10f) 
        {
            if (TryGetComponent(out IDamageable health))
            {
                health.Damage(1f); 
            }
        }
    }
}
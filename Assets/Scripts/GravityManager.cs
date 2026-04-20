using UnityEngine;

public class GravityManager : MonoBehaviour, IDamageable
{
    private Rigidbody2D _rb;
    private bool _isUpsideDown = false;
    private float _baseGravity;
    private float _initialScaleY;

    [SerializeField] private bool isFixedObject = false;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _baseGravity = Mathf.Abs(_rb.gravityScale);
        _initialScaleY = transform.localScale.y;
    }

    public void Damage(float damage)
    {
        ToggleGravity();
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
}
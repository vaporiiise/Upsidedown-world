using UnityEngine;

public class GravityBox : MonoBehaviour, IDamageable
{
    private Rigidbody2D _rb;

    void Awake() => _rb = GetComponent<Rigidbody2D>();

    public void Damage(float damage)
    {
        FlipGravity();
    }

    private void FlipGravity()
    {
        _rb.gravityScale *= -1;
        transform.Rotate(180f, 0f, 0f);
    }
}
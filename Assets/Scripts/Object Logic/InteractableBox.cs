using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class InteractableBox : MonoBehaviour, IDamageable
{
    private Rigidbody2D _rb;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        
        _rb.mass = 1f; 
        _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    public void Damage(float amount)
    {
        Debug.Log("Box Hit!");
        // if (health <= 0) Destroy(gameObject);
    }
}

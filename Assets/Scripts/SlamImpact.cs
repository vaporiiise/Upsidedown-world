using UnityEngine;

public class SlamImpact : MonoBehaviour
{
    private float _damage;
    private bool _hasImpacted = false;

    public void Initialize(float damage) => _damage = damage;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (_hasImpacted) return;

        if (collision.gameObject.CompareTag("Ground") || collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            _hasImpacted = true;
            
            if (TryGetComponent(out IDamageable healthInterface))
            {
                healthInterface.Damage(_damage);
                Debug.Log("Slam successful!");
            }

            Destroy(this);
        }
    }
}
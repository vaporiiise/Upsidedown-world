using UnityEngine;

public class BreakableObject : MonoBehaviour, IDamageable
{
    [SerializeField] private float health = 1f;
    [SerializeField] private GameObject breakEffect; 

    public void Damage(float damage)
    {
        health -= damage;
        if (health <= 0) Break();
    }

    private void Break()
    {
        if (breakEffect != null) Instantiate(breakEffect, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }
}
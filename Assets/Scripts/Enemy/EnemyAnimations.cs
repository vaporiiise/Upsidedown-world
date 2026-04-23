using UnityEngine;

public class EnemyAnimations : MonoBehaviour
{
    private Animator _animator;

    void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    public void PlayMove(bool isWalking)
    {
        if (_animator != null)
            _animator.SetBool("IsWalking", isWalking); 

    }

    public void PlayGotHit()
    {
        _animator.SetTrigger("GotHit");
    }

    public void PlayAttack()
    {
        _animator.SetTrigger("Attack");
    }
}
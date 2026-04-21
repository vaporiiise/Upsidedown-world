using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PlayerAnimations : MonoBehaviour
{
    private Animator _anim;
    private Rigidbody2D _rb;
    private PlayerController _player;

    public float heavyAttackTimer = 0.5f;

    void Awake()
    {
        _anim = GetComponent<Animator>();
        _rb = GetComponentInParent<Rigidbody2D>();
        _player = GetComponentInParent<PlayerController>();
    }

    void Update()
    {
        if (_player == null || _rb == null) return;

        _anim.SetFloat("Speed", Mathf.Abs(_rb.linearVelocity.x));
        _anim.SetBool("isGrounded", _player.isGrounded);

        float relativeVerticalSpeed = _rb.linearVelocity.y * Mathf.Sign(_rb.gravityScale);
        _anim.SetFloat("VerticalVelocity", relativeVerticalSpeed);

        _anim.SetBool("isBlocking", _player.IsBlocking());
    }
    

    public void AE_AttackImpact()
    {
        _player.TriggerAttackImpact();
    }

    public void AE_AttackKnockback()
    {
        _player.TriggerAttackKnockback();
    }

    public void PlayLightAttack() => _anim.SetTrigger("Attack");
    public void PlayHeavyAttack() => _anim.SetTrigger("Attack2");
    public void SetAttackHold() => _anim.SetBool("isHoldingAttack", true);
    public void UnSetAttackHold() => _anim.SetBool("isHoldingAttack", false);
}
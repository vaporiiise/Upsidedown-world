using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Animator))]
public class PlayerAnimations : MonoBehaviour
{
    private Animator _anim;
    private Rigidbody2D _rb;
    private PlayerController _player;
    private Coroutine _comboResetCoroutine;

    public float heavyAttackTimer = 0.5f;
    public float comboResetDelay = 1.0f; 
    
    private int _attackStep = 0;

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

    public void PlayGotHit()
    {
        _anim.SetTrigger("Hit");
    }

    public void PlayDeath()
    {
        _anim.SetTrigger("Die");
    }

    public void PlayAttack()
    {
        _anim.SetInteger("AttackIndex", _attackStep);
        
        _anim.SetTrigger("Attack");

        _attackStep = (_attackStep == 0) ? 1 : 0;
        
        if (_comboResetCoroutine != null) StopCoroutine(_comboResetCoroutine);
        _comboResetCoroutine = StartCoroutine(ResetComboTimer());
    }

    public void PlayCombo()
    {
        _anim.SetTrigger("IsOnAir");
        _anim.SetTrigger("Combo");
    }
    
    public void PlayAttackLogic(bool isInAir)
    {
        _anim.SetBool("IsOnAir", isInAir);

        if (isInAir)
        {
            _anim.ResetTrigger("Attack");
            _anim.ResetTrigger("Combo");

            _anim.SetTrigger("Combo");
        }
        else
        {
            _anim.ResetTrigger("Combo");
            _anim.SetInteger("AttackIndex", _attackStep);
            _anim.SetTrigger("Attack");

            _attackStep = (_attackStep == 0) ? 1 : 0;

            if (_comboResetCoroutine != null) StopCoroutine(_comboResetCoroutine);
            _comboResetCoroutine = StartCoroutine(ResetComboTimer());
        }
    }

    private IEnumerator ResetComboTimer()
    {
        yield return new WaitForSeconds(comboResetDelay);
        _attackStep = 0; 
        _comboResetCoroutine = null;
    }    public void PlayHeavyAttack() => _anim.SetTrigger("Attack2");
    public void SetAttackHold() => _anim.SetBool("isHoldingAttack", true);
    public void UnSetAttackHold() => _anim.SetBool("isHoldingAttack", false);
    
    public void AE_FinishAirAttack()
    {

        _anim.ResetTrigger("Combo");
        _anim.SetBool("IsOnAir",false);
    }
}
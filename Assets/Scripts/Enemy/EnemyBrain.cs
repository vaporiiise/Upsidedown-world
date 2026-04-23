using UnityEngine;

public class EnemyBrain : MonoBehaviour
{
    private EnemyController _controller;
    private Transform _player;

    [Header("Settings")]
    public float chaseRange = 8f;
    public float attackRange = 1.2f;

    void Start()
    {
        _controller = GetComponent<EnemyController>();
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) _player = p.transform;
    }

    void Update()
    {
        if (_player == null || _controller.IsLaunchedState()) return;

        float distance = Vector2.Distance(transform.position, _player.position);
        float xDir = Mathf.Sign(_player.position.x - transform.position.x);

        if (distance <= chaseRange && distance > attackRange)
        {
            _controller.SetMoveInput(xDir);
        }
        else if (distance <= attackRange)
        {
            _controller.SetMoveInput(0); 
            _controller.TryAttack();
        }
        else
        {
            _controller.SetMoveInput(0);
        }
    }
}
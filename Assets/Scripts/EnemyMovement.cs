using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 1f;
    [SerializeField] private float patrolDistance = 5f;

    private Vector2 _startPosition;
    private int _direction = 1;
    private SpriteRenderer _spriteRenderer;
    private Rigidbody2D _rb2d;

    private void Awake()
    {
        _rb2d = GetComponent<Rigidbody2D>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        _startPosition = _rb2d != null ? _rb2d.position : (Vector2)transform.position;
    }

    private void FixedUpdate()
    {
        Vector2 currentPos = _rb2d != null ? _rb2d.position : (Vector2)transform.position;
        Vector2 nextPos = currentPos + Vector2.right * (_direction * moveSpeed * Time.fixedDeltaTime);

        if (_rb2d != null)
        {
            _rb2d.MovePosition(nextPos);
        }
        else
        {
            transform.position = nextPos;
        }

        if (nextPos.x >= _startPosition.x + patrolDistance)
        {
            _direction = -1;
            if (_spriteRenderer != null)
            {
                _spriteRenderer.flipX = true;
            }
        }
        else if (nextPos.x <= _startPosition.x - patrolDistance)
        {
            _direction = 1;
            if (_spriteRenderer != null)
            {
                _spriteRenderer.flipX = false;
            }
        }
    }
}

using FishNet.Object;
using UnityEngine;

public class BallController : NetworkBehaviour
{
    [SerializeField] private float speed = 6f;
    [SerializeField] private float speedIncrease = 0.1f;

    private Rigidbody2D rb;
    private Vector2 startPosition;
    private GameManager gameManager;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        startPosition = transform.position;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        gameManager = FindObjectOfType<GameManager>();
        LaunchBall();
    }

    [Server]
    private void LaunchBall()
    {
        // Случайное направление (влево или вправо, с небольшим вертикальным отклонением)
        float randomY = Random.Range(-0.8f, 0.8f);
        Vector2 direction = new Vector2(Random.Range(0, 2) == 0 ? -1 : 1, randomY).normalized;
        rb.linearVelocity = direction * speed;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!IsServer) return;

        // Столкновение с ракеткой — увеличиваем скорость
        if (collision.gameObject.CompareTag("Paddle"))
        {
            // Немного увеличиваем скорость после каждого удара
            rb.linearVelocity = rb.linearVelocity.normalized * (rb.linearVelocity.magnitude + speedIncrease);

            // Добавляем эффект от места удара (верх/низ ракетки)
            float hitFactor = (transform.position.y - collision.transform.position.y) / collision.collider.bounds.size.y;
            Vector2 newDirection = new Vector2(rb.linearVelocity.x > 0 ? 1 : -1, hitFactor).normalized;
            rb.linearVelocity = newDirection * rb.linearVelocity.magnitude;
        }

        // Проверяем, кто забил гол
        if (collision.gameObject.CompareTag("LeftWall"))
        {
            gameManager.AddScore(1); // правый игрок забил
            ResetBall();
        }
        else if (collision.gameObject.CompareTag("RightWall"))
        {
            gameManager.AddScore(0); // левый игрок забил
            ResetBall();
        }
    }

    [Server]
    private void ResetBall()
    {
        rb.linearVelocity = Vector2.zero;
        transform.position = startPosition;
        Invoke(nameof(LaunchBall), 1f);
    }
}
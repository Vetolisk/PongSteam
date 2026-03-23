using FishNet.Object;
using UnityEngine;

public class PaddleController : NetworkBehaviour
{
    [SerializeField] private float speed = 8f;
    [SerializeField] private KeyCode upKey = KeyCode.UpArrow;
    [SerializeField] private KeyCode downKey = KeyCode.DownArrow;

    private Rigidbody2D rb;
    private float movement;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        // Только владелец управляет своей ракеткой
        if (!IsOwner) return;

        movement = 0;
        if (Input.GetKey(upKey)) movement = 1;
        if (Input.GetKey(downKey)) movement = -1;
    }

    private void FixedUpdate()
    {
        if (!IsOwner) return;

        // Отправляем движение на сервер
        if (movement != 0)
        {
            MovePaddle(movement);
        }
    }

    [ServerRpc]
    private void MovePaddle(float direction)
    {
        // Движение на сервере
        float newY = rb.position.y + direction * speed * Time.fixedDeltaTime;
        newY = Mathf.Clamp(newY, -3.5f, 3.5f);
        rb.MovePosition(new Vector2(rb.position.x, newY));
    }
}
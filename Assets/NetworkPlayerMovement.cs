using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class NetworkPlayerMovement : NetworkBehaviour
{
    public float speed = 5f;
    private Rigidbody2D rb;
    private bool cameraSet = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (!cameraSet)
        {
            var cam = Camera.main?.GetComponent<CameraFollow>();
            if (cam != null)
            {
                cam.SetTarget(transform);
                cameraSet = true;
            }
        }
    }

    private void FixedUpdate()
    {
        if (!IsOwner) return;

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector2 move = new Vector2(h, v).normalized;
        rb.linearVelocity = move * speed;
    }
}
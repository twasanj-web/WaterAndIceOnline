using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class NetworkPlayerMovement : NetworkBehaviour
{
    public float speed = 5f;
    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        // ✅ CRITICAL: Only the owner can control their character
        if (!IsOwner) 
        {
            return; // Other players' characters should NOT read input
        }

        // Only owner reads input
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector2 move = new Vector2(h, v).normalized;
        
        if (rb != null)
        {
            rb.linearVelocity = move * speed;
        }
    }

    // Optional: Stop movement when despawned
    public override void OnNetworkDespawn()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
        base.OnNetworkDespawn();
    }
}
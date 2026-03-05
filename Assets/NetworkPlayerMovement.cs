using Unity.Netcode;
using UnityEngine;
using PinePie.SimpleJoystick;

[RequireComponent(typeof(Rigidbody2D))]
public class NetworkPlayerMovement : NetworkBehaviour
{
    public float speed = 5f;
    private Rigidbody2D rb;
    private JoystickController joystick;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public override void OnNetworkSpawn()
    {
        Camera cam = GetComponentInChildren<Camera>();
        AudioListener listener = GetComponentInChildren<AudioListener>();

        if (cam != null) cam.enabled = IsOwner;
        if (listener != null) listener.enabled = IsOwner;

        if (IsOwner)
        {
            joystick = FindObjectOfType<JoystickController>(true);
        }
    }

    private void FixedUpdate()
    {
        if (!IsOwner) return;

        Vector2 move = Vector2.zero;

        if (joystick != null)
        {
            move = joystick.InputDirection;
        }
        else
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            move = new Vector2(h, v).normalized;
        }

        rb.linearVelocity = move * speed;
    }
}
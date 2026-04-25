using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class NetworkPlayerMovement : NetworkBehaviour
{
    public float speed = 5f;
    private Rigidbody2D rb;
    private Joystick joystick;

    public NetworkVariable<bool> isFrozen = new NetworkVariable<bool>(false);

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
            joystick = FindObjectOfType<Joystick>();
            if (joystick == null)
                Debug.LogWarning("لم يتم العثور على Joystick في الـ Scene!");
        }
    }

    private void FixedUpdate()
    {
        if (!IsOwner) return;

        if (isFrozen.Value)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        float h = 0f;
        float v = 0f;

        if (joystick != null)
        {
            h = joystick.Horizontal;
            v = joystick.Vertical;
        }
        else
        {
            h = Input.GetAxisRaw("Horizontal");
            v = Input.GetAxisRaw("Vertical");
        }

        Vector2 move = new Vector2(h, v).normalized;
        rb.linearVelocity = move * speed;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetFrozenServerRpc(bool frozenState)
    {
        isFrozen.Value = frozenState;
    }
}
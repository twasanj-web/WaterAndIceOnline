using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class NetworkPlayerMovement : NetworkBehaviour
{
    public float speed = 5f;
    private Rigidbody2D rb;
    private Joystick joystick;

    [Header("Audio (Assign in Prefab)")]
    public AudioSource audioSource;
    public AudioClip freezeSound;
    public AudioClip unfreezeSound;

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

        // مراقبة تغير حالة التجميد لتشغيل الصوت للشخص المعني
        isFrozen.OnValueChanged += OnFrozenStateChanged;
    }

    public override void OnNetworkDespawn()
    {
        isFrozen.OnValueChanged -= OnFrozenStateChanged;
    }

    private void OnFrozenStateChanged(bool previous, bool current)
    {
        // الشخص الذي يتم تجميده أو فك تجميده هو فقط من يسمع الصوت محلياً
        if (IsOwner)
        {
            if (current) // أصبح مجمد
                PlayLocalSound(freezeSound);
            else // تم فك تجميده
                PlayLocalSound(unfreezeSound);
        }
    }

    private void PlayLocalSound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }

    private void FixedUpdate()
    {
        if (!IsOwner) return;

        if (isFrozen.Value)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        float h = 0f; float v = 0f;
        if (joystick != null) { h = joystick.Horizontal; v = joystick.Vertical; }
        else { h = Input.GetAxisRaw("Horizontal"); v = Input.GetAxisRaw("Vertical"); }

        Vector2 move = new Vector2(h, v).normalized;
        rb.linearVelocity = move * speed;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetFrozenServerRpc(bool frozenState)
    {
        isFrozen.Value = frozenState;
    }
}

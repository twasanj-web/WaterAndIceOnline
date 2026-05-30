using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class NetworkPlayerMovement : NetworkBehaviour
{
    public float speed = 5f;
    private Rigidbody2D rb;
    private Joystick joystick;
    private NetworkPlayerVisual playerVisual;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip freezeSound;
    public AudioClip unfreezeSound;

    public NetworkVariable<bool> isFrozen = new NetworkVariable<bool>(false);
    // متغير لمزامنة الاتجاه (يمين/يسار) عبر الشبكة
    public NetworkVariable<bool> isFacingRight = new NetworkVariable<bool>(true);

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerVisual = GetComponent<NetworkPlayerVisual>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            joystick = FindObjectOfType<Joystick>();
            Camera cam = GetComponentInChildren<Camera>();
            if (cam != null) cam.enabled = true;
        }

        isFrozen.OnValueChanged += OnFrozenStateChanged;
    }

    private void Update()
    {
        HandleAnimations();
    }

    private void HandleAnimations()
    {
        Animator anim = playerVisual.GetActiveAnimator();
        SpriteRenderer sr = playerVisual.GetActiveSpriteRenderer();

        if (anim != null)
        {
            // إذا كان اللاعب يتحرك وسرعته أكبر من الصفر، فعل أنميشن المشي
            bool moving = rb.linearVelocity.magnitude > 0.1f && !isFrozen.Value;
            anim.SetBool("isMoving", moving);
        }

        if (sr != null)
        {
            // تحديث الـ Flip بناءً على المتغير المتزامن
            sr.flipX = !isFacingRight.Value;
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

        float h = 0f; float v = 0f;
        if (joystick != null) { h = joystick.Horizontal; v = joystick.Vertical; }
        else { h = Input.GetAxisRaw("Horizontal"); v = Input.GetAxisRaw("Vertical"); }

        Vector2 move = new Vector2(h, v).normalized;
        rb.linearVelocity = move * speed;

        // تحديث اتجاه الوجه وإرساله للسيرفر
        if (h > 0.1f) SetFacingRightServerRpc(true);
        else if (h < -0.1f) SetFacingRightServerRpc(false);
    }

    [ServerRpc]
    void SetFacingRightServerRpc(bool facingRight)
    {
        isFacingRight.Value = facingRight;
    }

    private void OnFrozenStateChanged(bool previous, bool current)
    {
        if (IsOwner)
        {
            if (current) PlayLocalSound(freezeSound);
            else PlayLocalSound(unfreezeSound);
        }
    }

    private void PlayLocalSound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetFrozenServerRpc(bool frozenState)
    {
        isFrozen.Value = frozenState;
    }
}

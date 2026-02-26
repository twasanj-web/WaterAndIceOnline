using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class NetworkPlayerVisual : NetworkBehaviour
{
    public Sprite waterSprite;
    public Sprite iceSprite;

    private SpriteRenderer sr;

    public NetworkVariable<int> roleIndex = new NetworkVariable<int>(0);

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    public override void OnNetworkSpawn()
    {
        roleIndex.OnValueChanged += OnRoleChanged;
        OnRoleChanged(0, roleIndex.Value);

        if (IsOwner)
        {
            int value = 0;
            if (AppSession.Instance != null)
                value = (int)AppSession.Instance.role;

            // لو ما عنده دور، خلّيه Water مؤقتاً عشان يبان (للاختبار)
            if (value == 0) value = 1;

            SetRoleServerRpc(value);
        }
    }

    [ServerRpc]
    void SetRoleServerRpc(int value)
    {
        roleIndex.Value = value;
    }

    void OnRoleChanged(int oldValue, int newValue)
    {
        if (sr == null) return;

        if (newValue == 1) sr.sprite = waterSprite;
        else if (newValue == 2) sr.sprite = iceSprite;
        else
        {
            // fallback عشان يبان
            sr.sprite = waterSprite != null ? waterSprite : sr.sprite;
        }
    }
}
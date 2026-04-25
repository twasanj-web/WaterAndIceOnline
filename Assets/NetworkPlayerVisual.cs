using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class NetworkPlayerVisual : NetworkBehaviour
{
    public Sprite waterSprite;
    public Sprite iceSprite;
    public Sprite frozenWaterSprite; // صورة الماء المجمد (fw.png)

    private SpriteRenderer sr;

    public NetworkVariable<int> roleIndex = new NetworkVariable<int>(0);
    public NetworkVariable<bool> isFrozenVisual = new NetworkVariable<bool>(false);

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    public override void OnNetworkSpawn()
    {
        roleIndex.OnValueChanged += OnRoleChanged;
        isFrozenVisual.OnValueChanged += OnFrozenChanged;

        OnRoleChanged(0, roleIndex.Value);
        OnFrozenChanged(false, isFrozenVisual.Value);

        if (IsOwner)
        {
            int value = 0;
            if (AppSession.Instance != null)
                value = (int)AppSession.Instance.role;

            if (value == 0) value = 1;

            SetRoleServerRpc(value);
        }
    }

    [ServerRpc]
    public void SetRoleServerRpc(int value)
    {
        roleIndex.Value = value;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetFrozenVisualServerRpc(bool frozen)
    {
        isFrozenVisual.Value = frozen;
    }

    void OnRoleChanged(int oldValue, int newValue)
    {
        if (sr == null) return;

        // لو مجمد، ما نغير الصورة
        if (isFrozenVisual.Value) return;

        if (newValue == 1) sr.sprite = waterSprite;
        else if (newValue == 2) sr.sprite = iceSprite;
        else sr.sprite = waterSprite != null ? waterSprite : sr.sprite;
    }

    void OnFrozenChanged(bool oldValue, bool newValue)
    {
        if (sr == null) return;

        if (newValue)
        {
            // مجمد → غير الصورة للماء المجمد
            if (frozenWaterSprite != null)
                sr.sprite = frozenWaterSprite;
        }
        else
        {
            // فك التجميد → رجّع صورة الماء الأصلية
            if (roleIndex.Value == 1 && waterSprite != null)
                sr.sprite = waterSprite;
        }
    }
}

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
            int value = 0; // None
            if (AppSession.Instance != null)
                value = (int)AppSession.Instance.role;

            SetRoleServerRpc(value);
        }
    }

    public override void OnNetworkDespawn()
    {
        roleIndex.OnValueChanged -= OnRoleChanged;
    }

    [ServerRpc]
    private void SetRoleServerRpc(int value)
    {
        roleIndex.Value = value;
    }

    private void OnRoleChanged(int oldValue, int newValue)
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();

        if (newValue == 1) sr.sprite = waterSprite; // Water
        else if (newValue == 2) sr.sprite = iceSprite; // Ice
    }
}
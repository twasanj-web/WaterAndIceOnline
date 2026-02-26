using Unity.Netcode;
using UnityEngine;

public class NetworkPlayerVisual : NetworkBehaviour
{
    public Sprite waterSprite;
    public Sprite iceSprite;

    private SpriteRenderer sr;

    public NetworkVariable<int> roleIndex = new NetworkVariable<int>();

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            // أرسل دوري للسيرفر
            int value = (int)AppSession.Instance.role;
            SetRoleServerRpc(value);
        }

        roleIndex.OnValueChanged += OnRoleChanged;
        OnRoleChanged(0, roleIndex.Value);
    }

    [ServerRpc]
    void SetRoleServerRpc(int value)
    {
        roleIndex.Value = value;
    }

    void OnRoleChanged(int oldValue, int newValue)
    {
        if (newValue == 1) // Water
            sr.sprite = waterSprite;
        else if (newValue == 2) // Ice
            sr.sprite = iceSprite;
    }
}
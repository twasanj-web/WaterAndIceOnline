using Unity.Netcode;
using Unity.Collections;

public class NetworkPlayerInfo : NetworkBehaviour
{
    public NetworkVariable<FixedString32Bytes> playerName =
        new NetworkVariable<FixedString32Bytes>("Player");

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        string name = "Player";

        if (AppSession.Instance != null && !string.IsNullOrWhiteSpace(AppSession.Instance.playerName))
            name = AppSession.Instance.playerName.Trim();

        SetNameServerRpc(name);
    }

    [ServerRpc]
    private void SetNameServerRpc(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            name = "Player";

        playerName.Value = name;
    }
}
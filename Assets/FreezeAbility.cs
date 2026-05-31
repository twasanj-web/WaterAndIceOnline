using Unity.Netcode;
using UnityEngine;

public class FreezeAbility : NetworkBehaviour
{
    [Header("Freeze Settings")]
    public float freezeDistance = 2.0f;

    private NetworkPlayerMovement myPlayer;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        myPlayer = GetComponent<NetworkPlayerMovement>();

        var session = AppSession.Instance;
        if (session != null && session.role != PlayerRole.Ice)
        {
            enabled = false;
            return;
        }

        var uiManager = FindObjectOfType<GameUIManager>();
        if (uiManager != null && uiManager.freezeButton != null)
            uiManager.freezeButton.onClick.AddListener(OnFreezeButtonClicked);
    }

    private void OnFreezeButtonClicked()
    {
        if (!IsOwner) return;

        NetworkPlayerMovement target = FindClosestWaterInRange();

        if (target == null)
        {
            Debug.Log("لا يوجد لاعب ماء قريب لتجميده.");
            return;
        }

        var targetVisual = target.GetComponent<NetworkPlayerVisual>();
        if (targetVisual == null)
        {
            Debug.Log("لا يوجد NetworkPlayerVisual على الهدف.");
            return;
        }

        target.SetFrozenServerRpc(true);
        targetVisual.SetFrozenVisualServerRpc(true);

        var uiManager = FindObjectOfType<GameUIManager>();
        if (uiManager != null)
            uiManager.PlayFreezeSoundLocal();

        Debug.Log("تم تجميد لاعب الماء!");
    }

    private NetworkPlayerMovement FindClosestWaterInRange()
    {
        NetworkPlayerMovement closest = null;
        float closestDistance = freezeDistance;

        var allPlayers = FindObjectsOfType<NetworkPlayerMovement>();

        foreach (var player in allPlayers)
        {
            if (!IsValidWaterTarget(player)) continue;

            float distance = Vector2.Distance(transform.position, player.transform.position);

            if (distance <= closestDistance)
            {
                closest = player;
                closestDistance = distance;
            }
        }

        return closest;
    }

    private bool IsValidWaterTarget(NetworkPlayerMovement otherPlayer)
    {
        if (otherPlayer == null) return false;
        if (myPlayer != null && otherPlayer == myPlayer) return false;
        if (otherPlayer.isFrozen.Value) return false;

        var otherVisual = otherPlayer.GetComponent<NetworkPlayerVisual>();
        if (otherVisual == null) return false;

        return otherVisual.roleIndex.Value == 1;
    }
}
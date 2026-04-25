using Unity.Netcode;
using UnityEngine;

public class FreezeAbility : NetworkBehaviour
{
    private bool canFreeze = false;
    private NetworkPlayerMovement targetToFreeze;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

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

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsOwner) return;

        if (collision.CompareTag("Player"))
        {
            var otherPlayer = collision.GetComponent<NetworkPlayerMovement>();
            if (otherPlayer != null && otherPlayer != GetComponent<NetworkPlayerMovement>() && !otherPlayer.isFrozen.Value)
            {
                targetToFreeze = otherPlayer;
                canFreeze = true;
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!IsOwner) return;

        if (collision.CompareTag("Player"))
        {
            var otherPlayer = collision.GetComponent<NetworkPlayerMovement>();
            if (otherPlayer != null && otherPlayer == targetToFreeze)
            {
                targetToFreeze = null;
                canFreeze = false;
            }
        }
    }

    private void OnFreezeButtonClicked()
    {
        if (!IsOwner) return;

        if (canFreeze && targetToFreeze != null)
        {
            targetToFreeze.SetFrozenServerRpc(true);

            var targetVisual = targetToFreeze.GetComponent<NetworkPlayerVisual>();
            if (targetVisual != null)
                targetVisual.SetRoleServerRpc(2);

            Debug.Log("تم تجميد لاعب الماء!");
        }
        else
        {
            Debug.Log("لا يوجد لاعب ماء قريب لتجميده.");
        }
    }
}

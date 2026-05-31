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

        if (!collision.CompareTag("Player")) return;

        var otherPlayer = collision.GetComponent<NetworkPlayerMovement>();

        if (otherPlayer == null) return;
        if (otherPlayer == GetComponent<NetworkPlayerMovement>()) return;
        if (otherPlayer.isFrozen.Value) return;

        var otherVisual = otherPlayer.GetComponent<NetworkPlayerVisual>();

        // القاعدة: الثلج يجمد الماء فقط
        // roleIndex 1 = Water
        // roleIndex 2 = Ice
        if (otherVisual == null || otherVisual.roleIndex.Value != 1)
        {
            targetToFreeze = null;
            canFreeze = false;
            return;
        }

        targetToFreeze = otherPlayer;
        canFreeze = true;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!IsOwner) return;

        if (!collision.CompareTag("Player")) return;

        var otherPlayer = collision.GetComponent<NetworkPlayerMovement>();

        if (otherPlayer != null && otherPlayer == targetToFreeze)
        {
            targetToFreeze = null;
            canFreeze = false;
        }
    }

    private void OnFreezeButtonClicked()
    {
        if (!IsOwner) return;

        if (canFreeze && targetToFreeze != null)
        {
            var targetVisual = targetToFreeze.GetComponent<NetworkPlayerVisual>();

            // تأكيد إضافي: لا تجمد إلا الماء
            if (targetVisual == null || targetVisual.roleIndex.Value != 1)
            {
                Debug.Log("لا يمكن تجميد لاعب الثلج.");
                targetToFreeze = null;
                canFreeze = false;
                return;
            }

            targetToFreeze.SetFrozenServerRpc(true);
            targetVisual.SetFrozenVisualServerRpc(true);

            var uiManager = FindObjectOfType<GameUIManager>();
            if (uiManager != null)
                uiManager.PlayFreezeSoundLocal();

            Debug.Log("تم تجميد لاعب الماء!");
        }
        else
        {
            Debug.Log("لا يوجد لاعب ماء قريب لتجميده.");
        }
    }
}
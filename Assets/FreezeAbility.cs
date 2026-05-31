using Unity.Netcode;
using UnityEngine;

public class FreezeAbility : NetworkBehaviour
{
    [Header("Freeze Settings")]
    public float freezeDistance = 1.2f;

    private bool canFreeze = false;
    private NetworkPlayerMovement targetToFreeze;
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

    private void Update()
    {
        if (!IsOwner) return;

        if (targetToFreeze != null)
        {
            float distance = Vector2.Distance(transform.position, targetToFreeze.transform.position);

            if (distance > freezeDistance || targetToFreeze.isFrozen.Value)
            {
                targetToFreeze = null;
                canFreeze = false;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsOwner) return;
        if (!collision.CompareTag("Player")) return;

        var otherPlayer = collision.GetComponent<NetworkPlayerMovement>();

        if (!IsValidWaterTarget(otherPlayer)) return;

        float distance = Vector2.Distance(transform.position, otherPlayer.transform.position);

        if (distance <= freezeDistance)
        {
            targetToFreeze = otherPlayer;
            canFreeze = true;
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (!IsOwner) return;
        if (!collision.CompareTag("Player")) return;

        var otherPlayer = collision.GetComponent<NetworkPlayerMovement>();

        if (!IsValidWaterTarget(otherPlayer)) return;

        float distance = Vector2.Distance(transform.position, otherPlayer.transform.position);

        if (distance <= freezeDistance)
        {
            targetToFreeze = otherPlayer;
            canFreeze = true;
        }
        else if (otherPlayer == targetToFreeze)
        {
            targetToFreeze = null;
            canFreeze = false;
        }
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

    private bool IsValidWaterTarget(NetworkPlayerMovement otherPlayer)
    {
        if (otherPlayer == null) return false;
        if (myPlayer != null && otherPlayer == myPlayer) return false;
        if (otherPlayer.isFrozen.Value) return false;

        var otherVisual = otherPlayer.GetComponent<NetworkPlayerVisual>();

        if (otherVisual == null) return false;

        // roleIndex 1 = Water فقط
        if (otherVisual.roleIndex.Value != 1) return false;

        return true;
    }

    private void OnFreezeButtonClicked()
    {
        if (!IsOwner) return;

        if (targetToFreeze == null)
        {
            Debug.Log("لا يوجد لاعب ماء قريب لتجميده.");
            canFreeze = false;
            return;
        }

        if (!IsValidWaterTarget(targetToFreeze))
        {
            Debug.Log("الهدف غير صالح للتجميد.");
            targetToFreeze = null;
            canFreeze = false;
            return;
        }

        float distance = Vector2.Distance(transform.position, targetToFreeze.transform.position);

        if (distance > freezeDistance)
        {
            Debug.Log("لاعب الماء بعيد جدًا عن التجميد.");
            targetToFreeze = null;
            canFreeze = false;
            return;
        }

        var targetVisual = targetToFreeze.GetComponent<NetworkPlayerVisual>();

        targetToFreeze.SetFrozenServerRpc(true);
        targetVisual.SetFrozenVisualServerRpc(true);

        var uiManager = FindObjectOfType<GameUIManager>();
        if (uiManager != null)
            uiManager.PlayFreezeSoundLocal();

        targetToFreeze = null;
        canFreeze = false;

        Debug.Log("تم تجميد لاعب الماء!");
    }
}
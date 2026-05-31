using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UnfreezeAbility : NetworkBehaviour
{
    [Header("Unfreeze Settings")]
    public float unfreezeDistance = 2.0f;
    public float requiredHoldTime = 3f;

    private NetworkPlayerMovement targetToUnfreeze;

    private bool isHolding = false;
    private float holdTimer = 0f;

    private Image loadingImage;
    private NetworkPlayerMovement myPlayer;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        myPlayer = GetComponent<NetworkPlayerMovement>();

        var session = AppSession.Instance;
        if (session != null && session.role != PlayerRole.Water)
        {
            enabled = false;
            return;
        }

        var uiManager = FindObjectOfType<GameUIManager>();
        if (uiManager != null && uiManager.unfreezeButton != null)
        {
            Transform barTransform = uiManager.unfreezeButton.transform.Find("LoadingBar");

            if (barTransform != null)
                loadingImage = barTransform.GetComponent<Image>();
            else
                loadingImage = uiManager.unfreezeButton.image;

            if (loadingImage != null)
                loadingImage.fillAmount = 0f;

            EventTrigger trigger = uiManager.unfreezeButton.gameObject.GetComponent<EventTrigger>();
            if (trigger == null)
                trigger = uiManager.unfreezeButton.gameObject.AddComponent<EventTrigger>();

            trigger.triggers.Clear();

            EventTrigger.Entry pointerDownEntry = new EventTrigger.Entry();
            pointerDownEntry.eventID = EventTriggerType.PointerDown;
            pointerDownEntry.callback.AddListener((data) => { OnPointerDown(); });
            trigger.triggers.Add(pointerDownEntry);

            EventTrigger.Entry pointerUpEntry = new EventTrigger.Entry();
            pointerUpEntry.eventID = EventTriggerType.PointerUp;
            pointerUpEntry.callback.AddListener((data) => { OnPointerUp(); });
            trigger.triggers.Add(pointerUpEntry);
        }
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (isHolding)
        {
            holdTimer += Time.deltaTime;

            if (loadingImage != null)
                loadingImage.fillAmount = holdTimer / requiredHoldTime;

            if (holdTimer >= requiredHoldTime)
            {
                targetToUnfreeze = FindClosestFrozenWaterInRange();

                if (targetToUnfreeze != null)
                {
                    PlayUnfreezeSoundImmediately();
                    UnfreezeTarget();
                }
                else
                {
                    Debug.Log("اكتمل التحميل ولكن لا يوجد لاعب ماء متجمد قريب.");
                }

                ResetLoading();
            }
        }
    }

    private NetworkPlayerMovement FindClosestFrozenWaterInRange()
    {
        NetworkPlayerMovement closest = null;
        float closestDistance = unfreezeDistance;

        var allPlayers = FindObjectsOfType<NetworkPlayerMovement>();

        foreach (var player in allPlayers)
        {
            if (!IsValidFrozenWaterTarget(player)) continue;

            float distance = Vector2.Distance(transform.position, player.transform.position);

            if (distance <= closestDistance)
            {
                closest = player;
                closestDistance = distance;
            }
        }

        return closest;
    }

    private bool IsValidFrozenWaterTarget(NetworkPlayerMovement otherPlayer)
    {
        if (otherPlayer == null) return false;
        if (myPlayer != null && otherPlayer == myPlayer) return false;
        if (!otherPlayer.isFrozen.Value) return false;

        var otherVisual = otherPlayer.GetComponent<NetworkPlayerVisual>();
        if (otherVisual == null) return false;

        return otherVisual.roleIndex.Value == 1;
    }

    private void PlayUnfreezeSoundImmediately()
    {
        var uiManager = FindObjectOfType<GameUIManager>();

        if (uiManager != null)
            uiManager.PlayUnfreezeSoundLocal();
    }

    private void ResetLoading()
    {
        isHolding = false;
        holdTimer = 0f;

        if (loadingImage != null)
            loadingImage.fillAmount = 0f;
    }

    private void OnPointerDown()
    {
        if (!IsOwner) return;

        isHolding = true;
        holdTimer = 0f;
    }

    private void OnPointerUp()
    {
        if (!IsOwner) return;

        ResetLoading();
    }

    private void UnfreezeTarget()
    {
        if (targetToUnfreeze == null) return;

        targetToUnfreeze.SetFrozenServerRpc(false);

        var targetVisual = targetToUnfreeze.GetComponent<NetworkPlayerVisual>();
        if (targetVisual != null)
            targetVisual.SetFrozenVisualServerRpc(false);

        targetToUnfreeze = null;

        Debug.Log("تم فك تجميد لاعب الماء بنجاح!");
    }
}
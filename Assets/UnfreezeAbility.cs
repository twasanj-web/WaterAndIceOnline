using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UnfreezeAbility : NetworkBehaviour
{
    private bool canUnfreeze = false;
    private NetworkPlayerMovement targetToUnfreeze;

    private bool isHolding = false;
    private float holdTimer = 0f;
    public float requiredHoldTime = 3f;

    private Image loadingImage;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

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
                if (canUnfreeze && targetToUnfreeze != null)
                {
                    PlayUnfreezeSoundImmediately();
                    UnfreezeTarget();
                }
                else
                {
                    Debug.Log("اكتمل التحميل ولكن لا يوجد لاعب متجمد قريب.");
                }

                ResetLoading();
            }
        }
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

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsOwner) return;

        if (collision.CompareTag("Player"))
        {
            var otherPlayer = collision.GetComponent<NetworkPlayerMovement>();

            if (otherPlayer != null &&
                otherPlayer != GetComponent<NetworkPlayerMovement>() &&
                otherPlayer.isFrozen.Value)
            {
                targetToUnfreeze = otherPlayer;
                canUnfreeze = true;
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!IsOwner) return;

        if (collision.CompareTag("Player"))
        {
            var otherPlayer = collision.GetComponent<NetworkPlayerMovement>();

            if (otherPlayer != null && otherPlayer == targetToUnfreeze)
            {
                targetToUnfreeze = null;
                canUnfreeze = false;
            }
        }
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

        Debug.Log("تم فك تجميد لاعب الماء بنجاح!");
    }
}
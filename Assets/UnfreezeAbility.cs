using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI; // نحتاج هذا للتعامل مع الصور

public class UnfreezeAbility : NetworkBehaviour
{
    private bool canUnfreeze = false;
    private NetworkPlayerMovement targetToUnfreeze;

    private bool isHolding = false;
    private float holdTimer = 0f;
    public float requiredHoldTime = 3f;

    // صورة التحميل (التي ستمتلئ)
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
            // الحصول على الصورة التي ستمتلئ (نفترض أنها موجودة داخل الزر)
            loadingImage = uiManager.unfreezeButton.image;

            EventTrigger trigger = uiManager.unfreezeButton.gameObject.AddComponent<EventTrigger>();

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

        if (isHolding && canUnfreeze && targetToUnfreeze != null)
        {
            holdTimer += Time.deltaTime;

            // تحديث شريط التحميل في الزر
            if (loadingImage != null)
            {
                loadingImage.fillAmount = holdTimer / requiredHoldTime;
            }

            if (holdTimer >= requiredHoldTime)
            {
                UnfreezeTarget();
                ResetLoading();
            }
        }
        else
        {
            ResetLoading();
        }
    }

    private void ResetLoading()
    {
        isHolding = false;
        holdTimer = 0f;
        if (loadingImage != null)
        {
            loadingImage.fillAmount = 0f; // إعادة تصفير التحميل
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsOwner) return;

        if (collision.CompareTag("Player"))
        {
            var otherPlayer = collision.GetComponent<NetworkPlayerMovement>();
            if (otherPlayer != null && otherPlayer != GetComponent<NetworkPlayerMovement>() &&
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
                ResetLoading();
            }
        }
    }

    private void OnPointerDown()
    {
        if (!IsOwner) return;
        if (canUnfreeze && targetToUnfreeze != null)
        {
            isHolding = true;
            holdTimer = 0f;
            Debug.Log("بدأ فك التجميد... استمر بالضغط 3 ثواني");
        }
    }

    private void OnPointerUp()
    {
        if (!IsOwner) return;
        ResetLoading();
    }

    private void UnfreezeTarget()
    {
        if (targetToUnfreeze != null)
        {
            targetToUnfreeze.SetFrozenServerRpc(false);

            var targetVisual = targetToUnfreeze.GetComponent<NetworkPlayerVisual>();
            if (targetVisual != null)
                targetVisual.SetFrozenVisualServerRpc(false);

            // تشغيل الصوت عند النجاح فقط
            var uiManager = FindObjectOfType<GameUIManager>();
            if (uiManager != null)
            {
                uiManager.PlayUnfreezeSoundLocal();
            }

            Debug.Log("تم فك تجميد لاعب الماء!");
        }
    }
}

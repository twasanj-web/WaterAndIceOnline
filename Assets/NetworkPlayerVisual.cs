using Unity.Netcode;
using UnityEngine;

public class NetworkPlayerVisual : NetworkBehaviour
{
    [Header("Visual Objects")]
    public GameObject waterVisual;
    public GameObject iceVisual;

    [Header("Frozen Sprites (Legacy Support)")]
    public Sprite frozenWaterSprite;

    public NetworkVariable<int> roleIndex = new NetworkVariable<int>(0);
    public NetworkVariable<bool> isFrozenVisual = new NetworkVariable<bool>(false);

    public override void OnNetworkSpawn()
    {
        roleIndex.OnValueChanged += OnRoleChanged;
        isFrozenVisual.OnValueChanged += OnFrozenChanged;

        // تحديث الحالة الأولية
        UpdateVisuals(roleIndex.Value, isFrozenVisual.Value);

        if (IsOwner)
        {
            int value = 0;
            if (AppSession.Instance != null)
                value = (int)AppSession.Instance.role;

            if (value == 0) value = 2; // مؤقتاً نجعل الافتراضي ثلج للتجربة

            SetRoleServerRpc(value);
        }
    }

    [ServerRpc]
    public void SetRoleServerRpc(int value)
    {
        roleIndex.Value = value;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetFrozenVisualServerRpc(bool frozen)
    {
        isFrozenVisual.Value = frozen;
    }

    void OnRoleChanged(int oldValue, int newValue) => UpdateVisuals(newValue, isFrozenVisual.Value);
    void OnFrozenChanged(bool oldValue, bool newValue) => UpdateVisuals(roleIndex.Value, newValue);

    void UpdateVisuals(int role, bool frozen)
    {
        // تفعيل الكائن المناسب حسب الدور
        if (waterVisual != null) waterVisual.SetActive(role == 1);
        if (iceVisual != null) iceVisual.SetActive(role == 2);

        // إذا كان مجمد، يمكننا إضافة تأثير هنا (مثل تغيير اللون للأزرق)
        // حالياً سنترك الأنميشن يتوقف من كود الحركة
    }

    // دالة مساعدة للحصول على الـ Animator النشط حالياً
    public Animator GetActiveAnimator()
    {
        if (roleIndex.Value == 1 && waterVisual != null) return waterVisual.GetComponent<Animator>();
        if (roleIndex.Value == 2 && iceVisual != null) return iceVisual.GetComponent<Animator>();
        return null;
    }

    // دالة مساعدة للحصول على الـ SpriteRenderer النشط حالياً (لعمل Flip)
    public SpriteRenderer GetActiveSpriteRenderer()
    {
        if (roleIndex.Value == 1 && waterVisual != null) return waterVisual.GetComponent<SpriteRenderer>();
        if (roleIndex.Value == 2 && iceVisual != null) return iceVisual.GetComponent<SpriteRenderer>();
        return null;
    }
}

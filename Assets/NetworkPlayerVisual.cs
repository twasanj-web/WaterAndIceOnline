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
        // البحث عن الكاميرا حتى لو كانت معطلة (true تعني ابحث في المعطل أيضاً)
        Camera cam = GetComponentInChildren<Camera>(true);
        AudioListener listener = GetComponentInChildren<AudioListener>(true);

        if (cam != null) 
        {
            // تفعيل الكائن نفسه أولاً ثم السكريبت للمالك فقط
            cam.gameObject.SetActive(IsOwner);
            cam.enabled = IsOwner;
        
            // إذا كان المالك، نضمن أن الكاميرا هي الأساسية
            if (IsOwner) cam.tag = "MainCamera";
        }

        if (listener != null) 
        {
            listener.gameObject.SetActive(IsOwner);
            listener.enabled = IsOwner;
        }

        // ربط الأحداث وتحديث الشكل
        roleIndex.OnValueChanged += OnRoleChanged;
        isFrozenVisual.OnValueChanged += OnFrozenChanged;
        UpdateVisuals(roleIndex.Value, isFrozenVisual.Value);

        if (IsOwner)
        {
            int value = 0;
            if (AppSession.Instance != null) value = (int)AppSession.Instance.role;
            if (value == 0) value = 2; 
            SetRoleServerRpc(value);
        }
    }


    public override void OnNetworkDespawn()
    {
        // فك الارتباط عند حذف اللاعب لتجنب الأخطاء
        roleIndex.OnValueChanged -= OnRoleChanged;
        isFrozenVisual.OnValueChanged -= OnFrozenChanged;
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

    private void OnRoleChanged(int oldValue, int newValue) => UpdateVisuals(newValue, isFrozenVisual.Value);
    private void OnFrozenChanged(bool oldValue, bool newValue) => UpdateVisuals(roleIndex.Value, newValue);

    public void UpdateVisuals(int role, bool frozen)
    {
        // تفعيل الكائن المناسب حسب الدور (1=ماء، 2=ثلج)
        if (waterVisual != null) waterVisual.SetActive(role == 1);
        if (iceVisual != null) iceVisual.SetActive(role == 2);

        // هنا يمكنك إضافة كود لتغيير لون الشخصية إذا كانت مجمدة
        // مثلاً جعل لونها مائلاً للأزرق
    }

    // دالة مساعدة للحصول على الـ Animator النشط حالياً (يستخدمها سكريبت الحركة)
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

using Unity.Netcode;
using UnityEngine;

public class NetworkPlayerVisual : NetworkBehaviour
{
    [Header("Visual Objects")]
    public GameObject waterVisual;
    public GameObject iceVisual;

    public NetworkVariable<int> roleIndex = new NetworkVariable<int>(0);
    public NetworkVariable<bool> isFrozenVisual = new NetworkVariable<bool>(false);

    public override void OnNetworkSpawn()
    {
        Debug.Log($"[PlayerVisual] Spawned! IsOwner: {IsOwner}, Role: {roleIndex.Value}");

        // 1. البحث عن الكاميرا وتفعيلها فوراً وبقوة
        Camera cam = GetComponentInChildren<Camera>(true);
        if (cam != null)
        {
            // تفعيل الكاميرا للمالك فقط
            cam.gameObject.SetActive(IsOwner);
            cam.enabled = IsOwner;
            if (IsOwner) 
            {
                cam.tag = "MainCamera";
                Debug.Log("[PlayerVisual] Camera enabled for Owner");
            }
        }
        else
        {
            Debug.LogError("[PlayerVisual] لم يتم العثور على كاميرا داخل البريفاب!");
        }

        // 2. تفعيل الـ AudioListener للمالك فقط
        AudioListener listener = GetComponentInChildren<AudioListener>(true);
        if (listener != null)
        {
            listener.enabled = IsOwner;
        }

        // 3. ربط الأحداث وتحديث الشكل
        roleIndex.OnValueChanged += OnRoleChanged;
        isFrozenVisual.OnValueChanged += OnFrozenChanged;
        
        // تحديث الشكل بناءً على الدور الحالي
        UpdateVisuals(roleIndex.Value, isFrozenVisual.Value);

        // 4. إذا كنت المالك، اطلب تحديد الدور
        if (IsOwner)
        {
            DetermineRole();
        }
    }

    private void DetermineRole()
    {
        int myRole = 0;
        if (AppSession.Instance != null)
        {
            myRole = (int)AppSession.Instance.role;
        }

        // إذا لم يجد دوراً (مثل حالة الدخول المباشر)، اجعله ماء (1) أو ثلج (2)
        if (myRole == 0) 
        {
            // الهوست عادة يكون ثلج، والداخلين ماء (مؤقتاً للتجربة)
            myRole = IsServer ? 2 : 1;
        }

        Debug.Log($"[PlayerVisual] Setting Role to: {myRole}");
        SetRoleServerRpc(myRole);
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
        if (waterVisual != null) waterVisual.SetActive(role == 1);
        if (iceVisual != null) iceVisual.SetActive(role == 2);
    }

    public Animator GetActiveAnimator()
    {
        if (roleIndex.Value == 1 && waterVisual != null) return waterVisual.GetComponent<Animator>();
        if (roleIndex.Value == 2 && iceVisual != null) return iceVisual.GetComponent<Animator>();
        return null;
    }

    public SpriteRenderer GetActiveSpriteRenderer()
    {
        if (roleIndex.Value == 1 && waterVisual != null) return waterVisual.GetComponent<SpriteRenderer>();
        if (roleIndex.Value == 2 && iceVisual != null) return iceVisual.GetComponent<SpriteRenderer>();
        return null;
    }

    public override void OnNetworkDespawn()
    {
        roleIndex.OnValueChanged -= OnRoleChanged;
        isFrozenVisual.OnValueChanged -= OnFrozenChanged;
    }
}

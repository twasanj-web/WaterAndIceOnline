using UnityEngine;

public class RoleRevealUI : MonoBehaviour
{
    [Header("Panels")]
    public GameObject waterRolePanel;
    public GameObject iceRolePanel;

    [Header("Timing")]
    public float showSeconds = 5f;

    private void Start()
    {
        // اطفي الكل أولاً
        if (waterRolePanel != null) waterRolePanel.SetActive(false);
        if (iceRolePanel != null) iceRolePanel.SetActive(false);

        var session = EnsureSession();

        // شغلي البانل حسب الدور
        switch (session.role)
        {
            case PlayerRole.Water:
                if (waterRolePanel != null) waterRolePanel.SetActive(true);
                break;

            case PlayerRole.Ice:
                if (iceRolePanel != null) iceRolePanel.SetActive(true);
                break;

            default:
                Debug.LogWarning("RoleRevealUI: role is None (لازم تحددي الدور قبل دخول GameMap)");
                break;
        }

        // اقفل بعد X ثواني
        Invoke(nameof(HidePanels), showSeconds);
    }

    private AppSession EnsureSession()
    {
        var session = AppSession.Instance;
        if (session != null) return session;

        Debug.LogWarning("RoleRevealUI: AppSession.Instance was NULL -> creating one in GameMap.");

        // اصنع AppSession تلقائياً
        var go = new GameObject("AppSession (Auto)");
        session = go.AddComponent<AppSession>();
        // Awake داخل AppSession راح يسوي DontDestroyOnLoad ويثبت Instance

        return session;
    }

    private void HidePanels()
    {
        if (waterRolePanel != null) waterRolePanel.SetActive(false);
        if (iceRolePanel != null) iceRolePanel.SetActive(false);
    }
}
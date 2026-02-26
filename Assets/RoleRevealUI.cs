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

        var session = AppSession.Instance;
        if (session == null)
        {
            Debug.LogError("RoleRevealUI: AppSession.Instance is NULL");
            return;
        }

        // شغلي البانل حسب الدور
        if (session.role == PlayerRole.Water)
        {
            if (waterRolePanel != null) waterRolePanel.SetActive(true);
        }
        else if (session.role == PlayerRole.Ice)
        {
            if (iceRolePanel != null) iceRolePanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("RoleRevealUI: role is None (ما تحدد الدور قبل دخول GameMap)");
        }

        // اقفل بعد 5 ثواني
        Invoke(nameof(HidePanels), showSeconds);
    }

    private void HidePanels()
    {
        if (waterRolePanel != null) waterRolePanel.SetActive(false);
        if (iceRolePanel != null) iceRolePanel.SetActive(false);
    }
}
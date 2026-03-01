using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;

public class RoleRevealUI : MonoBehaviour
{
    [Header("Panels")]
    public GameObject waterRolePanel;
    public GameObject iceRolePanel;

    [Header("Timing")]
    public float showSeconds = 5f;

    private async void Start()
    {
        if (waterRolePanel != null) waterRolePanel.SetActive(false);
        if (iceRolePanel != null) iceRolePanel.SetActive(false);

        var session = AppSession.Instance;
        if (session == null)
        {
            Debug.LogError("RoleRevealUI: AppSession.Instance is NULL");
            return;
        }

        // لازم يكون عندنا lobbyId + playerId
        if (string.IsNullOrWhiteSpace(session.lobbyId) || string.IsNullOrWhiteSpace(session.playerId))
        {
            Debug.LogWarning("RoleRevealUI: lobbyId/playerId missing.");
            return;
        }

        // حمّل بيانات اللوبي وحدد الدور
        await ResolveRoleFromLobby(session);

        // شغّل البانل حسب الدور
        if (session.role == PlayerRole.Ice)
        {
            if (iceRolePanel != null) iceRolePanel.SetActive(true);
        }
        else if (session.role == PlayerRole.Water)
        {
            if (waterRolePanel != null) waterRolePanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("RoleRevealUI: role still None.");
        }

        Invoke(nameof(HidePanels), showSeconds);
    }

    private async Task ResolveRoleFromLobby(AppSession session)
    {
        try
        {
            Lobby lobby = await LobbyService.Instance.GetLobbyAsync(session.lobbyId);

            string iceCsv = "";
            if (lobby.Data != null && lobby.Data.ContainsKey("iceIds"))
                iceCsv = lobby.Data["iceIds"].Value;

            var iceIds = (iceCsv ?? "")
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .ToHashSet();

            bool amIce = iceIds.Contains(session.playerId);

            session.role = amIce ? PlayerRole.Ice : PlayerRole.Water;

            Debug.Log($"✅ Role resolved from lobby: playerId={session.playerId}, role={session.role}, iceCsv={iceCsv}");
        }
        catch (Exception e)
        {
            Debug.LogError("RoleRevealUI: failed to get lobby/role: " + e);
        }
    }

    private void HidePanels()
    {
        if (waterRolePanel != null) waterRolePanel.SetActive(false);
        if (iceRolePanel != null) iceRolePanel.SetActive(false);
    }
}
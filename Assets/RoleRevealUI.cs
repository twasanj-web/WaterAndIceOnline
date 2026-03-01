using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

using Unity.Services.Core;
using Unity.Services.Core.Environments;
using Unity.Services.Authentication;
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

        var session = AppSession.Ensure();

        // ✅ إذا الدور None، حدديه من Lobby
        if (session.role == PlayerRole.None)
        {
            await EnsureServices();
            await ResolveRoleFromLobby();
        }

        // ✅ اعرضي البانل حسب الدور
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
            Debug.LogWarning("RoleRevealUI: role is None حتى بعد ResolveRoleFromLobby.");
        }

        Invoke(nameof(HidePanels), showSeconds);
    }

    private async Task EnsureServices()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            var options = new InitializationOptions().SetEnvironmentName("development");
            await UnityServices.InitializeAsync(options);
        }

        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

        var session = AppSession.Ensure();
        if (string.IsNullOrEmpty(session.playerId))
            session.playerId = AuthenticationService.Instance.PlayerId;
    }

    private async Task ResolveRoleFromLobby()
    {
        var session = AppSession.Ensure();

        if (string.IsNullOrWhiteSpace(session.lobbyId))
        {
            Debug.LogWarning("ResolveRoleFromLobby: lobbyId فاضي.");
            return;
        }

        try
        {
            Lobby lobby = await LobbyService.Instance.GetLobbyAsync(session.lobbyId);

            if (lobby.Data == null || !lobby.Data.ContainsKey("iceIds"))
            {
                Debug.LogWarning("ResolveRoleFromLobby: iceIds غير موجود.");
                return;
            }

            string iceCsv = lobby.Data["iceIds"].Value ?? "";
            var iceIds = iceCsv.Split(',')
                               .Select(s => s.Trim())
                               .Where(s => !string.IsNullOrEmpty(s))
                               .ToHashSet();

            bool isIce = iceIds.Contains(session.playerId);

            session.role = isIce ? PlayerRole.Ice : PlayerRole.Water;

            Debug.Log($"✅ Role resolved: {session.role} | playerId={session.playerId} | iceCsv={iceCsv}");
        }
        catch (Exception e)
        {
            Debug.LogError("ResolveRoleFromLobby failed: " + e);
        }
    }

    private void HidePanels()
    {
        if (waterRolePanel != null) waterRolePanel.SetActive(false);
        if (iceRolePanel != null) iceRolePanel.SetActive(false);
    }
}
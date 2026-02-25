using UnityEngine;
using TMPro;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using System.Collections;
using System.Threading.Tasks;

public class WaitingRoomUI : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text statusText;        // (1/3)
    public TMP_Text[] nameSlots;       // Name(1) ... Name(9)

    [Header("Refresh")]
    public float refreshSeconds = 1.0f;

    private Coroutine pollRoutine;

    private async void Start()
    {
        // صفّر UI أولاً
        ClearSlots();
        if (statusText != null) statusText.text = "(0/0)";

        await InitServices();

        var session = AppSession.Instance;
        if (session == null || string.IsNullOrWhiteSpace(session.lobbyId))
        {
            Debug.LogError("WaitingRoomUI: no AppSession or lobbyId is empty!");
            return;
        }

        pollRoutine = StartCoroutine(PollLobby(session.lobbyId));
    }

    private void OnDestroy()
    {
        if (pollRoutine != null)
            StopCoroutine(pollRoutine);
    }

    private void ClearSlots()
    {
        if (nameSlots == null) return;
        for (int i = 0; i < nameSlots.Length; i++)
            if (nameSlots[i] != null) nameSlots[i].text = "";
    }

    private async Task InitServices()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            var options = new InitializationOptions().SetEnvironmentName("development");
            await UnityServices.InitializeAsync(options);
        }

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
    }

    private IEnumerator PollLobby(string lobbyId)
    {
        while (true)
        {
            var task = RefreshLobbyUI(lobbyId);
            while (!task.IsCompleted) yield return null;

            yield return new WaitForSeconds(refreshSeconds);
        }
    }

    private async Task RefreshLobbyUI(string lobbyId)
    {
        try
        {
            Lobby lobby = await LobbyService.Instance.GetLobbyAsync(lobbyId);

            int current = lobby.Players != null ? lobby.Players.Count : 0;
            int max = lobby.MaxPlayers;

            // حدّث الستاتس
            if (statusText != null)
                statusText.text = $"({current}/{max})";

            // حدّث الأسماء
            ClearSlots();

            if (lobby.Players == null || nameSlots == null) return;

            int slotsCount = Mathf.Min(nameSlots.Length, lobby.Players.Count);
            for (int i = 0; i < slotsCount; i++)
            {
                string name = GetPlayerName(lobby.Players[i], i);
                if (nameSlots[i] != null) nameSlots[i].text = name;
            }

            // Debug خفيف
            // Debug.Log($"WaitingRoomUI Refresh: {current}/{max}");
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError("GetLobbyAsync failed: " + e);
        }
    }

    private string GetPlayerName(Player p, int index)
    {
        if (p != null && p.Data != null && p.Data.ContainsKey("name"))
        {
            string v = p.Data["name"].Value;
            if (!string.IsNullOrWhiteSpace(v)) return v.Trim();
        }
        return $"Player {index + 1}";
    }
}
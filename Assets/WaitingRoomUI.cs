using UnityEngine;
using TMPro;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using System.Collections;
using System.Threading.Tasks;
using System.Collections.Generic;

public class WaitingRoomUI : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text statusText;        // (1/3)
    public TMP_Text[] nameSlots;       // Name(1) ... Name(9)

    [Header("Refresh")]
    public float refreshSeconds = 1.0f;

    private Coroutine pollRoutine;
    private bool hasMovedToGame;

    private async void Start()
    {
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

        // خزني PlayerId في السيشن
        if (AppSession.Instance != null)
            AppSession.Instance.playerId = AuthenticationService.Instance.PlayerId;
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

            if (statusText != null)
                statusText.text = $"({current}/{max})";

            ClearSlots();

            if (lobby.Players != null && nameSlots != null)
            {
                int slotsCount = Mathf.Min(nameSlots.Length, lobby.Players.Count);
                for (int i = 0; i < slotsCount; i++)
                {
                    string name = GetPlayerName(lobby.Players[i], i);
                    if (nameSlots[i] != null) nameSlots[i].text = name;
                }
            }

            // ✅ لو اللعبة بدأت: حددي الدور فقط (بدون نقل للـ GameMap)
            if (!hasMovedToGame && lobby.Data != null && lobby.Data.ContainsKey("state"))
            {
                string state = lobby.Data["state"].Value;
                if (state == "started")
                {
                    hasMovedToGame = true;

                    if (pollRoutine != null)
                        StopCoroutine(pollRoutine);

                    ApplyRoleOnly(lobby);

                    if (statusText != null)
                        statusText.text = "جاري بدء اللعبة...";
                }
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError("GetLobbyAsync failed: " + e);
        }
    }

    // ✅ فقط تحديد الدور (بدون LoadScene)
    private void ApplyRoleOnly(Lobby lobby)
    {
        var session = AppSession.Instance;
        if (session == null)
        {
            Debug.LogError("ApplyRoleOnly: AppSession is null");
            return;
        }

        // اقرأ iceIds
        HashSet<string> iceSet = new HashSet<string>();
        if (lobby.Data != null && lobby.Data.ContainsKey("iceIds"))
        {
            string csv = lobby.Data["iceIds"].Value ?? "";
            foreach (var p in csv.Split(','))
            {
                string id = p.Trim();
                if (!string.IsNullOrWhiteSpace(id))
                    iceSet.Add(id);
            }
        }

        // حددي دوري حسب playerId
        if (!string.IsNullOrWhiteSpace(session.playerId) && iceSet.Contains(session.playerId))
            session.role = PlayerRole.Ice;
        else
            session.role = PlayerRole.Water;

        Debug.Log($"Role decided => {session.role} | myId={session.playerId}");
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
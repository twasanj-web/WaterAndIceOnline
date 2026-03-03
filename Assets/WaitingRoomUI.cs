using UnityEngine;
using TMPro;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

public class WaitingRoomUI : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text statusText;
    public TMP_Text[] nameSlots;

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
            var options = new InitializationOptions().SetEnvironmentName("production");
            await UnityServices.InitializeAsync(options);
        }

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

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

            // لو اللعبة بدأت
            if (!hasMovedToGame && lobby.Data != null && lobby.Data.ContainsKey("state"))
            {
                string state = lobby.Data["state"].Value;
                if (state == "started")
                {
                    hasMovedToGame = true;
                    if (pollRoutine != null) StopCoroutine(pollRoutine);

                    await ApplyRoleAndGoToGame(lobby);
                }
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError("GetLobbyAsync failed: " + e);
        }
    }

    private async Task ApplyRoleAndGoToGame(Lobby lobby)
    {
        var session = AppSession.Instance;
        if (session == null)
        {
            Debug.LogError("ApplyRoleAndGoToGame: AppSession is null");
            return;
        }

        // 1. عيّن الدور
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

        session.role = (!string.IsNullOrWhiteSpace(session.playerId) && iceSet.Contains(session.playerId))
            ? PlayerRole.Ice
            : PlayerRole.Water;

        Debug.Log($"Role decided => {session.role} | myId={session.playerId}");

        // 2. الهوست لا يحتاج Relay هنا (هو بدأه في WaitingRoomStartGame)
        if (!session.isHost)
        {
            // اقرأ relayCode من اللوبي
            if (lobby.Data == null || !lobby.Data.ContainsKey("relayCode"))
            {
                Debug.LogError("ApplyRoleAndGoToGame: relayCode missing from lobby data!");
                return;
            }

            string relayCode = lobby.Data["relayCode"].Value;
            Debug.Log($"Client joining Relay with code: {relayCode}");

            // انضم للـ Relay
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(relayCode);
            var relayData = AllocationUtils.ToRelayServerData(joinAllocation, "dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayData);
            NetworkManager.Singleton.StartClient();
        }

        // 3. انتقل للماب
        SceneManager.LoadScene("GameMap");
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

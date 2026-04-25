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
    public float refreshSeconds = 2.5f;

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

            // حدّث عدد اللاعبين في AppSession
            if (AppSession.Instance != null)
                AppSession.Instance.currentPlayerCount = current;

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

            if (!hasMovedToGame && lobby.Data != null && lobby.Data.ContainsKey("state"))
            {
                string state = lobby.Data["state"].Value;
                if (state == "started")
                {
                    hasMovedToGame = true;
                    if (pollRoutine != null) StopCoroutine(pollRoutine);
                    ApplyRoleAndGoToGame(lobby);
                }
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError("GetLobbyAsync failed: " + e);
        }
    }

    private void ApplyRoleAndGoToGame(Lobby lobby)
    {
        var session = AppSession.Instance;
        if (session == null)
        {
            Debug.LogError("ApplyRoleAndGoToGame: AppSession is null");
            return;
        }

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

        if (!session.isHost)
        {
            if (lobby.Data == null || !lobby.Data.ContainsKey("relayCode"))
            {
                Debug.LogError("ApplyRoleAndGoToGame: relayCode missing from lobby data!");
                return;
            }
            session.relayJoinCode = lobby.Data["relayCode"].Value;
            Debug.Log($"Client: relayCode saved = {session.relayJoinCode}");
        }

        Debug.Log("Moving to GameMap...");
        SceneManager.sceneLoaded += OnGameMapLoaded;
        SceneManager.LoadScene("GameMap");
    }
    private void OnGameMapLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "GameMap") return;
        SceneManager.sceneLoaded -= OnGameMapLoaded;

        StartCoroutine(StartClientWithDelay());
    }

    private IEnumerator StartClientWithDelay()
    {
        var session = AppSession.Instance;
        Debug.Log($"GameMap loaded! Waiting 1.5s for HOST... Relay code={session.relayJoinCode}");

        yield return new WaitForSeconds(1.5f);

        var task = JoinRelayAndStartClient(session.relayJoinCode);
        while (!task.IsCompleted) yield return null;

        if (task.Exception != null)
            Debug.LogError("CLIENT failed to start: " + task.Exception);
    }

    private async Task JoinRelayAndStartClient(string relayJoinCode)
    {
        var joinAllocation = await RelayService.Instance.JoinAllocationAsync(relayJoinCode);
        var relayData = AllocationUtils.ToRelayServerData(joinAllocation, "dtls");
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayData);
        NetworkManager.Singleton.StartClient();
        Debug.Log("CLIENT started in GameMap successfully!");
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

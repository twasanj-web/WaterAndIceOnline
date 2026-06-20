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
    public TMP_Text lobbyCodeText;
    public GameObject startButton; // زر السهم يظهر للهوست فقط

    [Header("Refresh")]
    public float refreshSeconds = 2.5f;

    private Coroutine pollRoutine;
    private bool hasMovedToGame;

    private async void Start()
    {
        ClearSlots();

        if (statusText != null)
            statusText.text = "(0/0)";

        await InitServices();

        var session = AppSession.Instance;

        if (startButton != null && session != null)
            startButton.SetActive(session.isHost);

        if (lobbyCodeText != null && session != null)
        {
            lobbyCodeText.text = "Code: " + session.lobbyCode;
        }

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
        {
            if (nameSlots[i] != null)
                nameSlots[i].text = "";
        }
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

            while (!task.IsCompleted)
                yield return null;

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

                    if (nameSlots[i] != null)
                        nameSlots[i].text = name;
                }
            }

            if (!hasMovedToGame && lobby.Data != null && lobby.Data.ContainsKey("state"))
            {
                string state = lobby.Data["state"].Value;

                if (state == "waiting")
                {
                    if (AppSession.Instance != null)
                        AppSession.Instance.returningToWaitingRoom = false;
                }

                if (state == "started")
                {
                    if (AppSession.Instance != null && AppSession.Instance.returningToWaitingRoom)
                        return;

                    hasMovedToGame = true;

                    if (pollRoutine != null)
                        StopCoroutine(pollRoutine);

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

            if (lobby.Data.ContainsKey("startAt"))
            {
                long.TryParse(lobby.Data["startAt"].Value, out session.gameStartUnixMs);
            }

            Debug.Log($"Client: relayCode saved = {session.relayJoinCode}");
        }

        if (session.isHost && lobby.Data.ContainsKey("startAt"))
        {
            long.TryParse(lobby.Data["startAt"].Value, out session.gameStartUnixMs);
        }

        Debug.Log("Moving to GameMap...");
        SceneManager.sceneLoaded += OnGameMapLoaded;
        SceneManager.LoadScene("GameMap");
    }

    private void OnGameMapLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "GameMap") return;

        SceneManager.sceneLoaded -= OnGameMapLoaded;

        var session = AppSession.Instance;

        if (session == null)
        {
            Debug.LogError("OnGameMapLoaded: AppSession is null");
            return;
        }

        if (session.isHost)
        {
            Debug.Log("Host already starts from WaitingRoomStartGame. Skip client start.");
            return;
        }

        Debug.Log($"GameMap loaded! Starting CLIENT via Relay code={session.relayJoinCode}");

        StartClientDelayed(session.relayJoinCode);
    }

    private async void StartClientDelayed(string relayJoinCode)
    {
        try
        {
            await Task.Delay(1500);

            var joinAllocation = await RelayService.Instance.JoinAllocationAsync(relayJoinCode);
            var relayData = AllocationUtils.ToRelayServerData(joinAllocation, "dtls");

            NetworkManager.Singleton
                .GetComponent<UnityTransport>()
                .SetRelayServerData(relayData);

            NetworkManager.Singleton.StartClient();

            Debug.Log("CLIENT started in GameMap successfully!");
        }
        catch (System.Exception e)
        {
            Debug.LogError("OnGameMapLoaded CLIENT failed: " + e);
        }
    }

    private string GetPlayerName(Player p, int index)
    {
        if (p != null && p.Data != null && p.Data.ContainsKey("name"))
        {
            string v = p.Data["name"].Value;

            if (!string.IsNullOrWhiteSpace(v))
                return v.Trim();
        }

        return $"Player {index + 1}";
    }
}
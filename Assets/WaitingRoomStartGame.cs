using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

using Unity.Netcode;

using Unity.Services.Core;
using Unity.Services.Core.Environments;
using Unity.Services.Authentication;

using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;

public class WaitingRoomStartGame : MonoBehaviour
{
    [Header("Netcode Scene Name")]
    public string gameSceneName = "GameMap";

    [Header("Client Poll (avoid 429)")]
    public float clientPollSeconds = 4f;

    [Header("Host Wait Clients")]
    public float hostWaitClientsTimeout = 20f;

    private bool _starting;
    private Coroutine _clientLoop;

    private async Task InitServices()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            var options = new InitializationOptions().SetEnvironmentName("development");
            await UnityServices.InitializeAsync(options);
        }

        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

        if (AppSession.Instance != null)
            AppSession.Instance.playerId = AuthenticationService.Instance.PlayerId;
    }

    private void Start()
    {
        var session = AppSession.Instance;
        if (session != null && !session.isHost)
        {
            _clientLoop = StartCoroutine(ClientJoinLoop());
        }
    }

    // اربطيها بزر السهم ➜ OnClick (هوست فقط)
    public async void OnArrowPressed()
    {
        Debug.Log("⬆️ Arrow Clicked -> OnArrowPressed called");

        if (_starting) return;
        _starting = true;

        try
        {
            await InitServices();

            var session = AppSession.Instance;
            if (session == null || string.IsNullOrWhiteSpace(session.lobbyId))
            {
                Debug.LogError("StartGame: AppSession/lobbyId missing");
                return;
            }

            if (!session.isHost)
            {
                Debug.Log("StartGame: only host can start.");
                return;
            }

            // 1) Get lobby and assign roles
            Lobby lobby = await LobbyService.Instance.GetLobbyAsync(session.lobbyId);
            int count = lobby.Players != null ? lobby.Players.Count : 0;

            if (!(count == 3 || count == 6 || count == 9))
            {
                Debug.LogWarning($"StartGame: invalid player count = {count}. Must be 3/6/9");
                return;
            }

            int iceCount = count / 3;
            List<string> ids = lobby.Players.Select(p => p.Id).ToList();
            List<string> iceIds = PickRandom(ids, iceCount);
            string iceCsv = string.Join(",", iceIds);

            Debug.Log($"✅ Roles: count={count}, iceCount={iceCount}, iceIds={iceCsv}");

            var data = new Dictionary<string, DataObject>
            {
                { "state",  new DataObject(DataObject.VisibilityOptions.Public, "started") },
                { "iceIds", new DataObject(DataObject.VisibilityOptions.Public, iceCsv) }
            };

            await LobbyService.Instance.UpdateLobbyAsync(session.lobbyId, new UpdateLobbyOptions { Data = data });

            // 2) Start Host Relay
            if (RelayNetworkManager.Instance == null)
            {
                Debug.LogError("❌ RelayNetworkManager.Instance is NULL (حطي RelayNetworkManager على NetworkBootstrap)");
                return;
            }

            RelayNetworkManager.Instance.gameSceneName = gameSceneName;

            bool hostOk = await RelayNetworkManager.Instance.EnsureHostRunningAsync();
            if (!hostOk)
            {
                Debug.LogError("❌ Failed to start host.");
                return;
            }

            // 3) WAIT until clients really connected (ConnectedClientsList includes host too)
            int expected = session.maxPlayers; // مثال: 3
            Debug.Log($"⏳ Waiting clients... expected={expected}");

            float t = 0f;
            while (t < hostWaitClientsTimeout)
            {
                int connected = NetworkManager.Singleton != null ? NetworkManager.Singleton.ConnectedClientsList.Count : 0;
                Debug.Log($"🔌 ConnectedClients = {connected} / {expected}");

                if (connected >= expected) break;

                t += 1f;
                await Task.Delay(1000);
            }

            int finalConnected = NetworkManager.Singleton != null ? NetworkManager.Singleton.ConnectedClientsList.Count : 0;
            Debug.Log($"✅ Done waiting. ConnectedClients = {finalConnected} / {expected}");

            // 4) Load GameMap for everyone عبر Netcode SceneManager
            RelayNetworkManager.Instance.LoadGameSceneForEveryone();
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError("StartGame failed: " + e);
        }
        finally
        {
            _starting = false;
        }
    }

    private IEnumerator ClientJoinLoop()
    {
        while (true)
        {
            if (NetworkManager.Singleton == null) { yield return new WaitForSeconds(clientPollSeconds); continue; }

            if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsHost)
            {
                Debug.Log("✅ Client already running. Waiting for host to load scene...");
                yield break;
            }

            if (RelayNetworkManager.Instance != null)
            {
                // محاولة واحدة كل كم ثواني (خفيفة لتجنب 429)
                _ = RelayNetworkManager.Instance.EnsureClientRunningFromLobbyAsync();
            }

            yield return new WaitForSeconds(clientPollSeconds);
        }
    }

    private List<string> PickRandom(List<string> source, int count)
    {
        var list = new List<string>(source);
        var result = new List<string>();

        for (int i = 0; i < count && list.Count > 0; i++)
        {
            int idx = Random.Range(0, list.Count);
            result.Add(list[idx]);
            list.RemoveAt(idx);
        }
        return result;
    }
}
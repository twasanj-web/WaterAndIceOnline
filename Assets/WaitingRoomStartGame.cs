using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

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

    [Header("Host Settings")]
    public float hostWaitForClientsSeconds = 10f; // ⭐ Host waits for clients to connect

    [Header("Client Auto Join")]
    public float clientMinRetrySeconds = 0.5f;
    public float clientMaxRetrySeconds = 5f;

    private bool isStarting;
    private Coroutine clientLoop;
    private Coroutine hostWaitLoop;

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
            // ⭐ Clients start trying to join Relay immediately
            clientLoop = StartCoroutine(ClientAutoJoinLoop());
        }
    }

    // ⭐ Called when host presses Start button
    public async void OnArrowPressed()
    {
        if (isStarting) return;
        isStarting = true;

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

            // 1) Get players and assign roles
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

            Debug.Log($"StartGame: count={count}, iceCount={iceCount}, iceIds={iceCsv}");

            // 2) ⭐ Start Relay and StartHost FIRST (before updating lobby state)
            if (RelayNetworkManager.Instance == null)
            {
                Debug.LogError("❌ RelayNetworkManager.Instance is NULL");
                return;
            }

            bool relayStarted = await RelayNetworkManager.Instance.HostStartRelayAndHostAsync();
            if (!relayStarted)
            {
                Debug.LogError("❌ Host Relay failed to start");
                return;
            }

            Debug.Log("✅ Host Relay started successfully");

            // 3) ⭐ Update lobby state to "started" (this signals clients to join Relay)
            var data = new Dictionary<string, DataObject>
            {
                { "state",  new DataObject(DataObject.VisibilityOptions.Public, "started") },
                { "iceIds", new DataObject(DataObject.VisibilityOptions.Public, iceCsv) }
            };

            await LobbyService.Instance.UpdateLobbyAsync(session.lobbyId, new UpdateLobbyOptions { Data = data });
            Debug.Log("✅ Lobby state set to 'started' - clients can now join Relay");

            // 4) ⭐ Wait for clients to connect to Netcode, then load scene for everyone
            StartCoroutine(HostWaitAndLoadScene(count));
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError("StartGame failed: " + e);
        }
        finally
        {
            isStarting = false;
        }
    }

    // ⭐ Host waits for expected number of clients to connect, then loads scene
    private IEnumerator HostWaitAndLoadScene(int expectedPlayerCount)
    {
        float timeout = hostWaitForClientsSeconds;
        float elapsed = 0f;

        Debug.Log($"⏳ Host waiting for {expectedPlayerCount - 1} clients to connect (timeout: {timeout}s)");

        while (elapsed < timeout)
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost)
            {
                // Count connected clients (total connected - 1 for host itself)
                int connectedCount = NetworkManager.Singleton.ConnectedClientsIds.Count;
                Debug.Log($"⏳ Connected clients: {connectedCount}/{expectedPlayerCount - 1}");

                if (connectedCount >= expectedPlayerCount - 1)
                {
                    Debug.Log($"✅ All {expectedPlayerCount} players connected! Loading game scene...");
                    LoadGameSceneForEveryone();
                    yield break;
                }
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        Debug.LogWarning($"⚠️ Timeout waiting for clients. Loading scene anyway with {NetworkManager.Singleton.ConnectedClientsIds.Count + 1} players");
        LoadGameSceneForEveryone();
    }

    // ⭐ Host loads scene via Netcode SceneManager (syncs to all clients)
    private void LoadGameSceneForEveryone()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("❌ No NetworkManager.Singleton");
            return;
        }

        if (!NetworkManager.Singleton.IsHost)
        {
            Debug.LogError("❌ Only host can load scene");
            return;
        }

        if (NetworkManager.Singleton.SceneManager == null)
        {
            Debug.LogError("❌ Netcode SceneManager is NULL. Enable Scene Management in NetworkManager.");
            return;
        }

        Debug.Log($"🚀 Host loading game scene for everyone: {gameSceneName}");
        NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
    }

    // ⭐ Clients auto-join Relay when they see "state=started" in lobby
    private IEnumerator ClientAutoJoinLoop()
    {
        float wait = clientMinRetrySeconds;
        var session = AppSession.Instance;

        // Clients keep trying to join until they succeed
        while (NetworkManager.Singleton != null &&
               !NetworkManager.Singleton.IsClient &&
               !NetworkManager.Singleton.IsHost)
        {
            // Check if game has started (host is waiting)
            try
            {
                if (session != null && !string.IsNullOrWhiteSpace(session.lobbyId))
                {
                    Lobby lobby = LobbyService.Instance.GetLobbyAsync(session.lobbyId).Result;
                    
                    if (lobby.Data != null && lobby.Data.ContainsKey("state") && 
                        lobby.Data["state"].Value == "started")
                    {
                        // Game started! Join Relay now
                        if (RelayNetworkManager.Instance != null)
                        {
                            bool clientJoined = RelayNetworkManager.Instance.ClientJoinRelayFromLobbyAndStartClientAsync().Result;
                            if (clientJoined)
                            {
                                Debug.Log("✅ Client successfully joined Relay!");
                                yield break;
                            }
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"⏳ Waiting for host to start: {e.Message}");
            }

            yield return new WaitForSeconds(wait);
            wait = Mathf.Min(clientMaxRetrySeconds, wait + 0.5f);
        }

        Debug.Log("✅ Client connected to Netcode. Waiting for scene load...");
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
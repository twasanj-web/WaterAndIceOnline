using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

using Unity.Services.Core;
using Unity.Services.Core.Environments;
using Unity.Services.Authentication;

using Unity.Services.Relay;
using Unity.Services.Relay.Models;

using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;

public class RelayNetworkManager : MonoBehaviour
{
    public static RelayNetworkManager Instance { get; private set; }

    [Header("Relay")]
    [Tooltip("عدد الاتصالات (بدون الهوست). مثال: لو 3 لاعبين إجمالي => 2")]
    public int maxConnections = 2;

    [Header("Netcode Scene Name")]
    public string gameSceneName = "GameMap";

    private UnityTransport transport;
    private bool servicesReady;

    private bool hostStarting;
    private bool clientStarting;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("❌ NetworkManager.Singleton is NULL. لازم يكون عندك NetworkManager موجود قبل هذا السكربت.");
            return;
        }

        transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport == null)
        {
            Debug.LogError("❌ UnityTransport غير موجود على نفس GameObject حق NetworkManager.");
            return;
        }
    }

    private async Task EnsureServices()
    {
        if (servicesReady) return;

        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            var options = new InitializationOptions().SetEnvironmentName("development");
            await UnityServices.InitializeAsync(options);
        }

        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

        if (AppSession.Instance != null)
            AppSession.Instance.playerId = AuthenticationService.Instance.PlayerId;

        servicesReady = true;
    }

    private async Task<bool> WaitUntil(Func<bool> condition, float timeoutSeconds)
    {
        float t = 0f;
        while (t < timeoutSeconds)
        {
            if (condition()) return true;
            await Task.Delay(100);
            t += 0.1f;
        }
        return condition();
    }

    // =========================
    // HOST
    // =========================
    public async Task<bool> HostStartRelayAndHostAsync()
    {
        if (hostStarting) return false;
        hostStarting = true;

        try
        {
            await EnsureServices();

            if (NetworkManager.Singleton == null || transport == null)
                return false;

            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsClient)
            {
                Debug.Log("✅ Network already running.");
                return NetworkManager.Singleton.IsHost;
            }

            var session = AppSession.Instance;
            if (session == null || string.IsNullOrEmpty(session.lobbyId))
            {
                Debug.LogError("❌ Host: AppSession أو lobbyId ناقص.");
                return false;
            }

            if (!session.isHost)
            {
                Debug.Log("⛔ HostStartRelayAndHost: هذا مو هوست.");
                return false;
            }

            maxConnections = Mathf.Max(1, session.maxPlayers - 1);

            Allocation alloc = await RelayService.Instance.CreateAllocationAsync(maxConnections);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(alloc.AllocationId);
            Debug.Log("✅ Relay JoinCode: " + joinCode);

            ConfigureHostTransport(alloc);

            var data = new Dictionary<string, DataObject>
            {
                { "relayJoinCode", new DataObject(DataObject.VisibilityOptions.Public, joinCode) }
            };

            await LobbyService.Instance.UpdateLobbyAsync(
                session.lobbyId,
                new UpdateLobbyOptions { Data = data }
            );

            bool ok = NetworkManager.Singleton.StartHost();
            Debug.Log("✅ StartHost (Relay) result: " + ok);

            return ok;
        }
        catch (Exception e)
        {
            Debug.LogError("❌ Host relay failed: " + e);
            return false;
        }
        finally
        {
            hostStarting = false;
        }
    }

    public async void HostStartRelayAndHost()
    {
        await HostStartRelayAndHostAsync();
    }

    // =========================
    // CLIENT
    // =========================
    public async Task<bool> ClientJoinRelayFromLobbyAndStartClientAsync()
    {
        if (clientStarting) return false;
        clientStarting = true;

        try
        {
            await EnsureServices();

            if (NetworkManager.Singleton == null || transport == null)
                return false;

            if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
            {
                Debug.Log("✅ Client already running.");
                return NetworkManager.Singleton.IsClient;
            }

            var session = AppSession.Instance;
            if (session == null || string.IsNullOrEmpty(session.lobbyId))
            {
                Debug.LogError("❌ Client: AppSession أو lobbyId ناقص.");
                return false;
            }

            // لا تكرري GetLobby بشكل جنوني
            Lobby lobby = await LobbyService.Instance.GetLobbyAsync(session.lobbyId);

            if (lobby.Data == null || !lobby.Data.ContainsKey("relayJoinCode"))
            {
                Debug.Log("⏳ relayJoinCode not ready yet.");
                return false;
            }

            string joinCode = lobby.Data["relayJoinCode"].Value;
            if (string.IsNullOrWhiteSpace(joinCode))
            {
                Debug.Log("⏳ relayJoinCode empty.");
                return false;
            }

            Debug.Log("✅ Relay joinCode: " + joinCode);

            JoinAllocation joinAlloc = await RelayService.Instance.JoinAllocationAsync(joinCode);
            ConfigureClientTransport(joinAlloc);

            bool ok = NetworkManager.Singleton.StartClient();
            Debug.Log("✅ StartClient (Relay) result: " + ok);

            // انتظر اتصال حقيقي
            bool connected = await WaitUntil(() =>
                NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient, 10f);

            if (!connected)
                Debug.LogError("❌ Client started but NOT connected (timeout).");

            return connected;
        }
        catch (Exception e)
        {
            Debug.LogError("❌ Client relay failed: " + e);
            return false;
        }
        finally
        {
            clientStarting = false;
        }
    }

    public async void ClientJoinRelayFromLobbyAndStartClient()
    {
        await ClientJoinRelayFromLobbyAndStartClientAsync();
    }

    // =========================
    // START GAME
    // =========================
    public async void StartGameAsHost()
    {
        if (NetworkManager.Singleton == null) return;

        var session = AppSession.Instance;
        if (session == null)
        {
            Debug.LogError("❌ AppSession غير موجود.");
            return;
        }

        if (!session.isHost)
        {
            Debug.Log("⛔ StartGameAsHost: فقط الهوست يقدر يبدأ اللعبة.");
            return;
        }

        if (!NetworkManager.Singleton.IsHost)
        {
            Debug.Log("⚠️ Host not running yet. Starting Host Relay first...");
            bool started = await HostStartRelayAndHostAsync();
            if (!started)
            {
                Debug.LogError("❌ فشل StartHost. ما أقدر أبدأ اللعبة.");
                return;
            }

            bool okHost = await WaitUntil(() => NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost, 8f);
            if (!okHost)
            {
                Debug.LogError("❌ ما صار Host خلال الوقت المحدد.");
                return;
            }
        }

        // ✅ اطبعي عدد المتصلين قبل LoadScene
        Debug.Log("ConnectedClients = " + NetworkManager.Singleton.ConnectedClientsList.Count);

        if (string.IsNullOrEmpty(gameSceneName))
        {
            Debug.LogError("❌ gameSceneName فاضي.");
            return;
        }

        Debug.Log("🚀 Loading game scene: " + gameSceneName);
        NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
    }

    // =========================
    // TRANSPORT CONFIG
    // =========================
    private void ConfigureHostTransport(Allocation alloc)
    {
        transport.SetRelayServerData(
            alloc.RelayServer.IpV4,
            (ushort)alloc.RelayServer.Port,
            alloc.AllocationIdBytes,
            alloc.Key,
            alloc.ConnectionData,
            alloc.ConnectionData,
            true
        );
    }

    private void ConfigureClientTransport(JoinAllocation joinAlloc)
    {
        transport.SetRelayServerData(
            joinAlloc.RelayServer.IpV4,
            (ushort)joinAlloc.RelayServer.Port,
            joinAlloc.AllocationIdBytes,
            joinAlloc.Key,
            joinAlloc.ConnectionData,
            joinAlloc.HostConnectionData,
            true
        );
    }
}
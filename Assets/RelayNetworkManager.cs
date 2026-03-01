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

    private UnityTransport _transport;
    private bool _servicesReady;

    private bool _hostStarting;
    private bool _clientStarting;

    private void Awake()
    {
        // Singleton
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

        _transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (_transport == null)
        {
            Debug.LogError("❌ UnityTransport غير موجود على نفس GameObject حق NetworkManager.");
            return;
        }
    }

    private async Task EnsureServices()
    {
        if (_servicesReady) return;

        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            var options = new InitializationOptions().SetEnvironmentName("development");
            await UnityServices.InitializeAsync(options);
        }

        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

        if (AppSession.Instance != null)
            AppSession.Instance.playerId = AuthenticationService.Instance.PlayerId;

        _servicesReady = true;
    }

    // =========================
    // HOST
    // =========================
    public async Task<bool> EnsureHostRunningAsync()
    {
        if (_hostStarting) return false;
        _hostStarting = true;

        try
        {
            await EnsureServices();

            if (NetworkManager.Singleton == null || _transport == null)
                return false;

            // إذا شغال أصلاً
            if (NetworkManager.Singleton.IsHost)
            {
                Debug.Log("✅ Host already running.");
                return true;
            }
            if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer)
            {
                Debug.LogWarning("⚠️ Network running but not host (client/server).");
                return false;
            }

            var session = AppSession.Instance;
            if (session == null || string.IsNullOrEmpty(session.lobbyId))
            {
                Debug.LogError("❌ Host: AppSession أو lobbyId ناقص.");
                return false;
            }
            if (!session.isHost)
            {
                Debug.Log("⛔ EnsureHostRunningAsync: هذا مو هوست.");
                return false;
            }

            maxConnections = Mathf.Max(1, session.maxPlayers - 1);

            // 1) Create Allocation
            Allocation alloc = await RelayService.Instance.CreateAllocationAsync(maxConnections);

            // 2) Join Code
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(alloc.AllocationId);
            Debug.Log("✅ Relay JoinCode: " + joinCode);

            // 3) Configure Transport
            ConfigureHostTransport(alloc);

            // 4) Save join code in lobby (مرة واحدة لكل بدء)
            var data = new Dictionary<string, DataObject>
            {
                { "relayJoinCode", new DataObject(DataObject.VisibilityOptions.Public, joinCode) }
            };

            await LobbyService.Instance.UpdateLobbyAsync(
                session.lobbyId,
                new UpdateLobbyOptions { Data = data }
            );

            // 5) Start Host
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
            _hostStarting = false;
        }
    }

    // =========================
    // CLIENT
    // =========================
    public async Task<bool> EnsureClientRunningFromLobbyAsync()
    {
        if (_clientStarting) return false;
        _clientStarting = true;

        try
        {
            await EnsureServices();

            if (NetworkManager.Singleton == null || _transport == null)
                return false;

            if (NetworkManager.Singleton.IsClient)
            {
                Debug.Log("✅ Client already running.");
                return true;
            }
            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
            {
                Debug.LogWarning("⚠️ Network running but not client (host/server).");
                return false;
            }

            var session = AppSession.Instance;
            if (session == null || string.IsNullOrEmpty(session.lobbyId))
            {
                Debug.LogError("❌ Client: AppSession أو lobbyId ناقص.");
                return false;
            }

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

            return ok;
        }
        catch (Exception e)
        {
            Debug.LogError("❌ Client relay failed: " + e);
            return false;
        }
        finally
        {
            _clientStarting = false;
        }
    }

    // =========================
    // HOST LOAD SCENE FOR ALL
    // =========================
    public void LoadGameSceneForEveryone()
    {
        if (NetworkManager.Singleton == null) return;

        if (!NetworkManager.Singleton.IsHost)
        {
            Debug.Log("⛔ LoadGameSceneForEveryone: فقط الهوست.");
            return;
        }

        Debug.Log("🚀 Loading game scene (for everyone): " + gameSceneName);
        NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
    }

    // =========================
    // TRANSPORT CONFIG
    // =========================
    private void ConfigureHostTransport(Allocation alloc)
    {
        _transport.SetRelayServerData(
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
        _transport.SetRelayServerData(
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
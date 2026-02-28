using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

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

    private UnityTransport _transport;
    private bool _servicesReady;

    private bool _hostStarting;
    private bool _clientStarting;

    private void Awake()
    {
        // لا نعتمد على NetworkManager.Singleton هنا
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private bool EnsureTransport()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogWarning("⏳ NetworkManager.Singleton not ready yet...");
            return false;
        }

        if (_transport == null)
            _transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

        if (_transport == null)
        {
            Debug.LogError("❌ UnityTransport غير موجود على نفس GameObject حق NetworkManager.");
            return false;
        }

        return true;
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
    public async void HostStartRelayAndHost()
    {
        if (_hostStarting) return;
        _hostStarting = true;

        try
        {
            if (!EnsureTransport()) return;
            await EnsureServices();

            if (NetworkManager.Singleton.IsHost ||
                NetworkManager.Singleton.IsServer ||
                NetworkManager.Singleton.IsClient)
            {
                Debug.Log("✅ HostStartRelayAndHost: Network already running.");
                return;
            }

            var session = AppSession.Instance;
            if (session == null || string.IsNullOrEmpty(session.lobbyId))
            {
                Debug.LogError("❌ Host: AppSession أو lobbyId ناقص.");
                return;
            }

            if (!session.isHost)
            {
                Debug.Log("HostStartRelayAndHost: هذا مو هوست.");
                return;
            }

            maxConnections = Mathf.Max(1, session.maxPlayers - 1);

            // 1) Create Allocation
            Allocation alloc = await RelayService.Instance.CreateAllocationAsync(maxConnections);

            // 2) Get Join Code
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(alloc.AllocationId);
            Debug.Log("✅ Relay JoinCode: " + joinCode);

            // 3) Configure Transport (متوافق مع نسختك)
            ConfigureHostTransport(alloc);

            // 4) Save Join Code in Lobby
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
        }
        catch (Exception e)
        {
            Debug.LogError("❌ Host relay failed: " + e);
        }
        finally
        {
            _hostStarting = false;
        }
    }

    // =========================
    // CLIENT
    // =========================
    public async void ClientJoinRelayFromLobbyAndStartClient()
    {
        if (_clientStarting) return;
        _clientStarting = true;

        try
        {
            if (!EnsureTransport()) return;
            await EnsureServices();

            if (NetworkManager.Singleton.IsClient ||
                NetworkManager.Singleton.IsHost ||
                NetworkManager.Singleton.IsServer)
            {
                Debug.Log("✅ ClientJoinRelay: already running.");
                return;
            }

            var session = AppSession.Instance;
            if (session == null || string.IsNullOrEmpty(session.lobbyId))
            {
                Debug.LogError("❌ Client: AppSession أو lobbyId ناقص.");
                return;
            }

            Lobby lobby = await LobbyService.Instance.GetLobbyAsync(session.lobbyId);

            if (lobby.Data == null || !lobby.Data.ContainsKey("relayJoinCode"))
            {
                Debug.Log("⏳ relayJoinCode not ready yet.");
                return;
            }

            string joinCode = lobby.Data["relayJoinCode"].Value;
            if (string.IsNullOrWhiteSpace(joinCode))
            {
                Debug.Log("⏳ relayJoinCode empty.");
                return;
            }

            Debug.Log("✅ Relay joinCode from lobby: " + joinCode);

            // 1) Join Allocation
            JoinAllocation joinAlloc = await RelayService.Instance.JoinAllocationAsync(joinCode);

            // 2) Configure Transport (متوافق مع نسختك)
            ConfigureClientTransport(joinAlloc);

            // 3) Start Client
            bool ok = NetworkManager.Singleton.StartClient();
            Debug.Log("✅ StartClient (Relay) result: " + ok);
        }
        catch (Exception e)
        {
            Debug.LogError("❌ Client relay failed: " + e);
        }
        finally
        {
            _clientStarting = false;
        }
    }

    // =========================
    // TRANSPORT CONFIG (COMPAT)
    // =========================
    private void ConfigureHostTransport(Allocation alloc)
    {
        // ✅ التوقيع اللي نسختك تحتاجه (يبغى allocationIdBytes وغيرها)
        _transport.SetRelayServerData(
            alloc.RelayServer.IpV4,
            (ushort)alloc.RelayServer.Port,
            alloc.AllocationIdBytes,
            alloc.Key,
            alloc.ConnectionData,
            alloc.ConnectionData, // hostConnectionData (في الهوست غالباً نفس ConnectionData)
            true                  // dtls
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
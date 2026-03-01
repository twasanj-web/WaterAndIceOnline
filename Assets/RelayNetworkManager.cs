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

    private UnityTransport transport;
    private bool servicesReady;
    private bool hostStarting;
    private bool clientStarting;

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
            Debug.LogError("❌ NetworkManager.Singleton is NULL. لازم يكون NetworkManager موجود على نفس الاوبجكت (NetworkBootstrap).");
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

        var session = AppSession.Ensure();
        session.playerId = AuthenticationService.Instance.PlayerId;

        servicesReady = true;
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

            var session = AppSession.Ensure();
            if (string.IsNullOrEmpty(session.lobbyId))
            {
                Debug.LogError("❌ Host: lobbyId ناقص.");
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

            // Configure transport
            transport.SetRelayServerData(
                alloc.RelayServer.IpV4,
                (ushort)alloc.RelayServer.Port,
                alloc.AllocationIdBytes,
                alloc.Key,
                alloc.ConnectionData,
                alloc.ConnectionData,
                true // dtls
            );

            // Save JoinCode in Lobby
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

            var session = AppSession.Ensure();
            if (string.IsNullOrEmpty(session.lobbyId))
            {
                Debug.LogError("❌ Client: lobbyId ناقص.");
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

            transport.SetRelayServerData(
                joinAlloc.RelayServer.IpV4,
                (ushort)joinAlloc.RelayServer.Port,
                joinAlloc.AllocationIdBytes,
                joinAlloc.Key,
                joinAlloc.ConnectionData,
                joinAlloc.HostConnectionData,
                true // dtls
            );

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
            clientStarting = false;
        }
    }

    public async void ClientJoinRelayFromLobbyAndStartClient()
    {
        await ClientJoinRelayFromLobbyAndStartClientAsync();
    }
}
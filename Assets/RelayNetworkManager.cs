using System;
using System.Collections.Generic;
using UnityEngine;

using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;

using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;

public class RelayNetworkManager : MonoBehaviour
{
    public static RelayNetworkManager Instance { get; private set; }

    [Header("Relay")]
    [Tooltip("عدد الاتصالات (بدون الهوست). مثال: لو maxPlayers=3 => هنا 2")]
    public int maxConnections = 2;

    private UnityTransport transport;

    private async void Awake()
    {
        // Singleton بسيط
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("❌ RelayNetworkManager: NetworkManager.Singleton is NULL (تأكدي NetworkManager موجود في السين).");
            return;
        }

        transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport == null)
        {
            Debug.LogError("❌ RelayNetworkManager: UnityTransport component is missing on NetworkManager.");
            return;
        }

        // Services
        try
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
                await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
        catch (Exception e)
        {
            Debug.LogError("❌ RelayNetworkManager Awake init failed: " + e);
        }
    }

    // =========================
    // HOST
    // =========================
    public async void HostStartRelayAndHost()
    {
        try
        {
            var session = AppSession.Instance;
            if (session == null || string.IsNullOrEmpty(session.lobbyId))
            {
                Debug.LogError("❌ HostStartRelayAndHost: No lobbyId in AppSession.");
                return;
            }

            // 1) Create Relay allocation
            Allocation alloc = await RelayService.Instance.CreateAllocationAsync(maxConnections);

            // 2) Create Join Code
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(alloc.AllocationId);
            Debug.Log("✅ Relay JoinCode: " + joinCode);

            // 3) Configure UnityTransport (Legacy API: ip + port)
            ConfigureTransportAsHost(alloc);

            // 4) Save joinCode into Lobby data
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
    }

    // =========================
    // CLIENT
    // =========================
    public async void ClientJoinRelayFromLobbyAndStartClient()
    {
        try
        {
            var session = AppSession.Instance;
            if (session == null || string.IsNullOrEmpty(session.lobbyId))
            {
                Debug.LogError("❌ ClientJoinRelayFromLobbyAndStartClient: No lobbyId in AppSession.");
                return;
            }

            // 1) Read lobby
            Lobby lobby = await LobbyService.Instance.GetLobbyAsync(session.lobbyId);

            if (lobby.Data == null || !lobby.Data.ContainsKey("relayJoinCode"))
            {
                Debug.Log("⏳ relayJoinCode not ready yet... (الهوست لسه ما فعل الريلاي)");
                return;
            }

            string joinCode = lobby.Data["relayJoinCode"].Value;
            if (string.IsNullOrWhiteSpace(joinCode))
            {
                Debug.LogError("❌ relayJoinCode is empty in lobby data.");
                return;
            }

            Debug.Log("✅ Relay joinCode from lobby: " + joinCode);

            // 2) Join allocation
            JoinAllocation joinAlloc = await RelayService.Instance.JoinAllocationAsync(joinCode);

            // 3) Configure transport (Legacy API: ip + port)
            ConfigureTransportAsClient(joinAlloc);

            // 4) Start client
            bool ok = NetworkManager.Singleton.StartClient();
            Debug.Log("✅ StartClient (Relay) result: " + ok);
        }
        catch (Exception e)
        {
            Debug.LogError("❌ Client relay failed: " + e);
        }
    }

    // =========================
    // Transport config (Legacy)
    // =========================
    private void ConfigureTransportAsHost(Allocation alloc)
    {
        var rs = alloc.RelayServer;

        // Legacy UTP: (string ip, ushort port, byte[] allocationId, byte[] key, byte[] connectionData, byte[] hostConnectionData)
        transport.SetRelayServerData(
            rs.IpV4,
            (ushort)rs.Port,
            alloc.AllocationIdBytes,
            alloc.Key,
            alloc.ConnectionData,
            alloc.ConnectionData
        );

        Debug.Log($"✅ Transport configured as HOST: {rs.IpV4}:{rs.Port}");
    }

    private void ConfigureTransportAsClient(JoinAllocation joinAlloc)
    {
        var rs = joinAlloc.RelayServer;

        transport.SetRelayServerData(
            rs.IpV4,
            (ushort)rs.Port,
            joinAlloc.AllocationIdBytes,
            joinAlloc.Key,
            joinAlloc.ConnectionData,
            joinAlloc.HostConnectionData
        );

        Debug.Log($"✅ Transport configured as CLIENT: {rs.IpV4}:{rs.Port}");
    }
}
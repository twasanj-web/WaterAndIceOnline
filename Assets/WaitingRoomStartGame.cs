using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;

public class WaitingRoomStartGame : MonoBehaviour
{
    private bool isStarting;

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
                isStarting = false;
                return;
            }

            if (!session.isHost)
            {
                Debug.Log("StartGame: only host can start.");
                isStarting = false;
                return;
            }

            

            int count = session.currentPlayerCount;
            int requiredPlayers = session.maxPlayers;

            if (count < requiredPlayers)
            {
                Debug.LogWarning(
                    $"StartGame: Waiting for players. Current = {count}, Required = {requiredPlayers}"
                );

                isStarting = false;
                return;
            }

            // 1. اختر الثلج عشوائياً
            // 1. اختر الثلج عشوائياً بناءً على عدد اللاعبين المختار
            int iceCount = session.maxPlayers / 3;
            Lobby lobby = await LobbyService.Instance.GetLobbyAsync(session.lobbyId);
            List<string> ids = lobby.Players.Select(p => p.Id).ToList();
            List<string> iceIds = PickRandom(ids, iceCount);
            string iceCsv = string.Join(",", iceIds);

            // 2. أنشئ Relay Allocation وخزّنه في AppSession
            //Allocation allocation = await RelayService.Instance.CreateAllocationAsync(count - 1); بجرب الان ارجع دا الاصلي 
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(Mathf.Max(1, count - 1));
            string relayJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            session.hostAllocation = allocation;

            Debug.Log($"StartGame: count={count}, iceCount={iceCount}, iceIds={iceCsv}, relayCode={relayJoinCode}");

            // 3. حدّث اللوبي
            long startAt = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 5000;
            session.gameStartUnixMs = startAt;

            await LobbyService.Instance.UpdateLobbyAsync(session.lobbyId, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    { "state",     new DataObject(DataObject.VisibilityOptions.Public, "started") },
                    { "iceIds",    new DataObject(DataObject.VisibilityOptions.Public, iceCsv) },
                    { "relayCode", new DataObject(DataObject.VisibilityOptions.Public, relayJoinCode) },
                    { "startAt",   new DataObject(DataObject.VisibilityOptions.Public, startAt.ToString()) }
                }
            });
            // 4. عيّن دور الهوست وخزّن كود Relay
            session.role = iceCsv.Contains(session.playerId) ? PlayerRole.Ice : PlayerRole.Water;
            session.relayJoinCode = relayJoinCode;

            Debug.Log($"StartGame: Host role={session.role}, loading GameMap...");

            // 5. سجّل event قبل تحميل السين
            SceneManager.sceneLoaded += OnGameMapLoaded;
            SceneManager.LoadScene("GameMap");
        }
        catch (System.Exception e)
        {
            Debug.LogError("StartGame failed: " + e);
            isStarting = false;
        }
    }

    private void OnGameMapLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "GameMap") return;
        SceneManager.sceneLoaded -= OnGameMapLoaded;

        var session = AppSession.Instance;
        Debug.Log($"GameMap loaded! Starting HOST via Relay allocation");

        try
        {
            var relayData = AllocationUtils.ToRelayServerData(session.hostAllocation, "dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayData);
            NetworkManager.Singleton.StartHost();
            Debug.Log("HOST started in GameMap!");
        }
        catch (System.Exception e)
        {
            Debug.LogError("OnGameMapLoaded HOST failed: " + e);
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

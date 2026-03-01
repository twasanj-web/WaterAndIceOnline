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

    [Header("Client Auto Join")]
    public float clientRetrySeconds = 3f;

    private bool isStarting;
    private Coroutine clientLoop;

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
            // ✅ جربي join مرة كل 3 ثواني فقط
            clientLoop = StartCoroutine(ClientJoinLoop());
        }
    }

    public async void OnArrowPressed()
    {
        Debug.Log("➡️ Arrow Clicked -> OnArrowPressed called");

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

            // 1) توزيع الأدوار في اللوبي
            Lobby lobby = await LobbyService.Instance.GetLobbyAsync(session.lobbyId);
            int count = lobby.Players != null ? lobby.Players.Count : 0;

            int iceCount = Mathf.Max(1, count / 3);
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

            // 2) شغلي Relay Host
            if (RelayNetworkManager.Instance == null)
            {
                Debug.LogError("❌ RelayNetworkManager.Instance is NULL");
                return;
            }

            bool ok = await RelayNetworkManager.Instance.HostStartRelayAndHostAsync();
            if (!ok)
            {
                Debug.LogError("❌ HostStartRelayAndHostAsync failed");
                return;
            }

            // ✅ انتظر شوي عشان الكلاينت يلحق يتصل (حتى لو ما اكتمل)
            await Task.Delay(1500);

            // 3) حملي المشهد عبر Netcode SceneManager
            RelayNetworkManager.Instance.gameSceneName = gameSceneName;
            RelayNetworkManager.Instance.StartGameAsHost();
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

    private IEnumerator ClientJoinLoop()
    {
        while (NetworkManager.Singleton != null &&
               !NetworkManager.Singleton.IsClient &&
               !NetworkManager.Singleton.IsHost)
        {
            if (RelayNetworkManager.Instance != null)
            {
                RelayNetworkManager.Instance.ClientJoinRelayFromLobbyAndStartClient();
            }

            yield return new WaitForSeconds(clientRetrySeconds);
        }

        Debug.Log("✅ Client join loop stopped (client/host is running).");
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
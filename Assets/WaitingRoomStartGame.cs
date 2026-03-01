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

    [Header("Client Auto Join (خففنا الطلبات)")]
    public float clientRetrySeconds = 5f;

    [Header("Host Wait For Players")]
    public float hostWaitTimeoutSeconds = 25f;

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

        var session = AppSession.Ensure();
        session.playerId = AuthenticationService.Instance.PlayerId;
    }

    private void Start()
    {
        var session = AppSession.Ensure();
        Debug.Log("WaitingRoom Start: isHost=" + session.isHost + " lobbyId=" + session.lobbyId);

        // ✅ الكلاينت يحاول يدخل Relay تلقائياً لكن كل 5 ثواني (بدون spam)
        if (!session.isHost)
            clientLoop = StartCoroutine(ClientAutoJoinLoop());
    }

    // اربطيها بزر السهم (هوست فقط)
    public async void OnArrowPressed()
    {
        if (isStarting) return;
        isStarting = true;

        try
        {
            await InitServices();

            var session = AppSession.Ensure();
            if (string.IsNullOrWhiteSpace(session.lobbyId))
            {
                Debug.LogError("StartGame: lobbyId missing");
                return;
            }

            if (!session.isHost)
            {
                Debug.Log("⛔ StartGame: only host can start. isHost=" + session.isHost);
                return;
            }

            // 1) جيب اللاعبين وحدد الأدوار
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

            var data = new Dictionary<string, DataObject>
            {
                { "state",  new DataObject(DataObject.VisibilityOptions.Public, "started") },
                { "iceIds", new DataObject(DataObject.VisibilityOptions.Public, iceCsv) }
            };

            await LobbyService.Instance.UpdateLobbyAsync(session.lobbyId, new UpdateLobbyOptions { Data = data });

            // 2) شغل Relay + Host
            if (RelayNetworkManager.Instance == null)
            {
                Debug.LogError("❌ RelayNetworkManager.Instance is NULL");
                return;
            }

            bool ok = await RelayNetworkManager.Instance.HostStartRelayAndHostAsync();
            if (!ok)
            {
                Debug.LogError("❌ StartHost failed.");
                return;
            }

            // 3) انتظر اتصال الجميع قبل تحميل GameMap للجميع
            StartCoroutine(HostWaitThenLoadScene(expectedPlayers: count));
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

    private IEnumerator HostWaitThenLoadScene(int expectedPlayers)
    {
        float t = 0f;
        float nextLog = 0f;

        Debug.Log("⏳ Waiting clients... expected=" + expectedPlayers);

        while (t < hostWaitTimeoutSeconds)
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost)
            {
                int connected = NetworkManager.Singleton.ConnectedClientsList.Count; // يشمل الهوست
                if (t >= nextLog)
                {
                    Debug.Log($"⏳ connected={connected}/{expectedPlayers}");
                    nextLog = t + 1f;
                }

                if (connected >= expectedPlayers)
                    break;
            }

            t += Time.deltaTime;
            yield return null;
        }

        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsHost)
        {
            Debug.LogError("❌ Host not running, cannot load scene.");
            yield break;
        }

        int finalConnected = NetworkManager.Singleton.ConnectedClientsList.Count;
        Debug.Log("✅ ConnectedClients = " + finalConnected);

        if (NetworkManager.Singleton.SceneManager == null)
        {
            Debug.LogError("❌ SceneManager is NULL. تأكدي Enable Scene Management ON.");
            yield break;
        }

        Debug.Log("🚀 Loading game scene (for everyone): " + gameSceneName);
        NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
    }

    private IEnumerator ClientAutoJoinLoop()
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

        Debug.Log("✅ Client started. Waiting for host to load scene...");
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
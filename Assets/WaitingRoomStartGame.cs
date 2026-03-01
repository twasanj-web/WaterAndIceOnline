using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

    [Header("Client Auto Join")]
    public float clientMinRetrySeconds = 2f;
    public float clientMaxRetrySeconds = 10f;

    [Header("Host wait clients")]
    public float hostWaitClientsTimeout = 20f;   // كم ثانية ننتظر الكلاينت قبل نحمّل السين
    public float hostWaitPollInterval = 0.5f;

    private bool isStarting;
    private Coroutine clientLoop;

    private async System.Threading.Tasks.Task InitServices()
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
            clientLoop = StartCoroutine(ClientAutoJoinLoop());
        }
    }

    // اربطيها بزر السهم ➜ OnClick (هوست فقط)
    public async void OnArrowPressed()
    {
        Debug.Log("🟦 Arrow Clicked -> OnArrowPressed called");

        if (isStarting) return;
        isStarting = true;

        try
        {
            await InitServices();

            var session = AppSession.Instance;
            if (session == null || string.IsNullOrWhiteSpace(session.lobbyId))
            {
                Debug.LogError("❌ StartGame: AppSession/lobbyId missing");
                return;
            }

            if (!session.isHost)
            {
                Debug.Log("⛔ StartGame: only host can start.");
                return;
            }

            // 1) جيب اللاعبين وحدد الأدوار
            Lobby lobby = await LobbyService.Instance.GetLobbyAsync(session.lobbyId);
            int count = lobby.Players != null ? lobby.Players.Count : 0;

            if (count <= 0)
            {
                Debug.LogError("❌ StartGame: lobby has no players?");
                return;
            }

            // مثال توزيع: ثلج = ثلث اللاعبين (3->1, 6->2, 9->3)
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

            // 2) شغل Relay + StartHost
            if (RelayNetworkManager.Instance == null)
            {
                Debug.LogError("❌ RelayNetworkManager.Instance is NULL (لازم يكون موجود في DontDestroyOnLoad)");
                return;
            }

            bool hostOk = await RelayNetworkManager.Instance.HostStartRelayAndHostAsync();
            if (!hostOk)
            {
                Debug.LogError("❌ Failed to StartHost (Relay).");
                return;
            }

            // 3) انتظري العملاء يتصلون فعلاً (ConnectedClientsList)
            int expectedPlayers = count; // عدد لاعبين اللوبي
            Debug.Log($"⏳ Waiting clients... expected={expectedPlayers}");

            float t = 0f;
            while (t < hostWaitClientsTimeout)
            {
                if (NetworkManager.Singleton == null) break;

                int connected = NetworkManager.Singleton.ConnectedClientsList.Count;
                Debug.Log($"🔌 ConnectedClients = {connected} / {expectedPlayers}");

                if (connected >= expectedPlayers)
                    break;

                await System.Threading.Tasks.Task.Delay((int)(hostWaitPollInterval * 1000));
                t += hostWaitPollInterval;
            }

            int finalConnected = NetworkManager.Singleton != null ? NetworkManager.Singleton.ConnectedClientsList.Count : 0;
            Debug.Log($"✅ Done waiting. ConnectedClients = {finalConnected} / {expectedPlayers}");

            // 4) الآن حمّلي السين للجميع عبر Netcode
            if (NetworkManager.Singleton.SceneManager == null)
            {
                Debug.LogError("❌ SceneManager is NULL. تأكدي Enable Scene Management ON.");
                return;
            }

            Debug.Log("🚀 Loading game scene (for everyone): " + gameSceneName);
            NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError("❌ StartGame Lobby error: " + e);
        }
        catch (System.Exception e)
        {
            Debug.LogError("❌ StartGame error: " + e);
        }
        finally
        {
            isStarting = false;
        }
    }

    private IEnumerator ClientAutoJoinLoop()
    {
        float wait = Mathf.Max(0.5f, clientMinRetrySeconds);

        while (NetworkManager.Singleton != null &&
               !NetworkManager.Singleton.IsClient &&
               !NetworkManager.Singleton.IsHost)
        {
            if (RelayNetworkManager.Instance != null)
            {
                // حاول مرة، إذا ما ضبط لا تسبّم لابي (عشان 429)
                var task = RelayNetworkManager.Instance.ClientJoinRelayFromLobbyAndStartClientAsync();
                while (!task.IsCompleted) yield return null;

                bool ok = task.Result;
                if (ok) break;
            }
            else
            {
                Debug.LogWarning("⏳ RelayNetworkManager.Instance is NULL (لازم DontDestroyOnLoad)");
            }

            yield return new WaitForSeconds(wait);
            wait = Mathf.Min(clientMaxRetrySeconds, wait + 1.5f);
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
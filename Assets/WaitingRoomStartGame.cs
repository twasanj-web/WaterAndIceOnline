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

    [Header("Client Auto Join")]
    public float clientRetrySeconds = 1f;

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
        // الكلاينت يحاول يدخل تلقائياً (بدون ما يضغط شيء)
        var session = AppSession.Instance;
        if (session != null && !session.isHost)
        {
            clientLoop = StartCoroutine(ClientAutoJoinLoop());
        }
    }

    // اربطيها بزر السهم ➜ OnClick
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

            // 2) شغل Relay + StartHost
            if (RelayNetworkManager.Instance == null)
            {
                Debug.LogError("❌ RelayNetworkManager.Instance is NULL (put it in scene)");
                return;
            }

            RelayNetworkManager.Instance.HostStartRelayAndHost();

            // 3) بعد ما يصير Host فعلاً، حمّل السين للجميع عبر Netcode
            StartCoroutine(HostLoadGameSceneWhenReady());
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

    private IEnumerator HostLoadGameSceneWhenReady()
    {
        float timeout = 10f;
        float t = 0f;

        while (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsHost && t < timeout)
        {
            t += Time.deltaTime;
            yield return null;
        }

        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("❌ No NetworkManager.Singleton");
            yield break;
        }

        if (!NetworkManager.Singleton.IsHost)
        {
            Debug.LogError("❌ Host didn't start in time.");
            yield break;
        }

        NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
        Debug.Log("✅ Host loading Game scene for everyone: " + gameSceneName);
    }

    private IEnumerator ClientAutoJoinLoop()
    {
        // يكرر محاولة الانضمام للـ Relay إلى أن يصير Client
        while (NetworkManager.Singleton != null &&
               !NetworkManager.Singleton.IsClient &&
               !NetworkManager.Singleton.IsHost)
        {
            if (RelayNetworkManager.Instance != null)
                RelayNetworkManager.Instance.ClientJoinRelayFromLobbyAndStartClient();

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
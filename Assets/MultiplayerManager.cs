using UnityEngine;
using TMPro;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Threading.Tasks;

public class MultiplayerManager : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text lobbyCodeText;

    [Header("Settings (fallback إذا ما فيه AppSession)")]
    public int maxPlayers = 4;
    public int roundTimeMinutes = 5;

    private Lobby hostLobby;
    private bool isCreatingLobby;

    private async void Start()
    {
        Debug.Log("MultiplayerManager Start - ShareCode");

        // خذي الإعدادات من AppSession (لو موجود)
        var session = AppSession.Instance;
        if (session != null)
        {
            maxPlayers = session.maxPlayers;
            roundTimeMinutes = session.roundTimeMinutes;
            session.isHost = true;
        }

        try
        {
            await InitServices();
            await CreateLobbyAutomatically();
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Start failed: " + ex);
        }
    }

    private async Task InitServices()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            var options = new InitializationOptions().SetEnvironmentName("development");
            await UnityServices.InitializeAsync(options);
            Debug.Log("UnityServices Initialized (development)");
        }

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log("Signed in anonymously");
        }
    }

    private async Task CreateLobbyAutomatically()
    {
        if (hostLobby != null)
        {
            if (lobbyCodeText != null) lobbyCodeText.text = hostLobby.LobbyCode;
            return;
        }

        if (isCreatingLobby) return;
        isCreatingLobby = true;

        try
        {
            var session = AppSession.Instance;
            string playerName = (session != null && !string.IsNullOrWhiteSpace(session.playerName))
                ? session.playerName.Trim()
                : "Player";

            Debug.Log($"Creating Lobby... maxPlayers={maxPlayers}, roundTime={roundTimeMinutes}, hostName={playerName}");

            string lobbyName = "WaterIce-" + Random.Range(1000, 9999);

            // بيانات اللاعب (الهوست) داخل اللوبي
            var playerData = new Dictionary<string, PlayerDataObject>
            {
                { "name", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, playerName) }
            };

            var lobbyOptions = new CreateLobbyOptions
            {
                IsPrivate = true,
                Player = new Player { Data = playerData }, // ✅ مهم: يرسل اسم الهوست من البداية
                Data = new Dictionary<string, DataObject>
                {
                    { "roundTime",  new DataObject(DataObject.VisibilityOptions.Public, roundTimeMinutes.ToString()) },
                    { "maxPlayers", new DataObject(DataObject.VisibilityOptions.Public, maxPlayers.ToString()) }
                }
            };

            hostLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, lobbyOptions);

            string code = hostLobby.LobbyCode;
            Debug.Log("Lobby Auto Created. Code: " + code);

            if (lobbyCodeText != null) lobbyCodeText.text = code;

            // خزّني بيانات اللوبي في AppSession
            if (session != null)
            {
                session.lobbyCode = hostLobby.LobbyCode;
                session.lobbyId = hostLobby.Id;
                session.maxPlayers = maxPlayers;
                session.roundTimeMinutes = roundTimeMinutes;
                session.isHost = true;
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError("CreateLobby failed: " + e);
            throw;
        }
        finally
        {
            isCreatingLobby = false;
        }
    }

    // زر الشير
    public void CopyCode()
    {
        if (lobbyCodeText == null) return;
        if (string.IsNullOrWhiteSpace(lobbyCodeText.text)) return;

        GUIUtility.systemCopyBuffer = lobbyCodeText.text.Trim();
        Debug.Log("Copied code: " + lobbyCodeText.text);
    }

    // ✅ زر الصح (ينقل لسين الويتنق روم)
    public void OnCheckButtonPressed()
    {
        Debug.Log("✅ Check button clicked - Going to WaitingRoom...");

        var session = AppSession.Instance;
        if (session != null)
        {
            session.maxPlayers = maxPlayers;
            session.roundTimeMinutes = roundTimeMinutes;
            session.isHost = true;

            if (hostLobby != null)
            {
                session.lobbyCode = hostLobby.LobbyCode;
                session.lobbyId = hostLobby.Id;
            }
            else if (lobbyCodeText != null)
            {
                session.lobbyCode = lobbyCodeText.text.Trim();
            }
        }

        SceneManager.LoadScene("WaitingRoom");
    }

    // لو تبين تستدعينها من CreateRoomSettings قبل ShareCode (اختياري)
    public void SetSettings(int players, int timeMinutes)
    {
        maxPlayers = players;
        roundTimeMinutes = timeMinutes;

        var session = AppSession.Instance;
        if (session != null)
        {
            session.maxPlayers = players;
            session.roundTimeMinutes = timeMinutes;
        }

        Debug.Log($"Settings received: players={maxPlayers}, time={roundTimeMinutes}");
    }
}
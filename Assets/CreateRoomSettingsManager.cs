using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

public class CreateRoomSettingsManager : MonoBehaviour
{
    [Header("UI")]
    public TMP_InputField nameInput;

    [Header("Next Scene")]
    public string shareCodeSceneName = "ShareCode";

    async void Start()
    {
        await InitServices();
    }

    async Task InitServices()
    {
        if (UnityServices.State == ServicesInitializationState.Initialized) return;

        var options = new InitializationOptions().SetEnvironmentName("development");
        await UnityServices.InitializeAsync(options);

        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    // اربطيها بزر "ابدأ"
    public async void OnClickStart()
    {
        // 1) حفظ الاسم
        var session = AppSession.Instance;
        session.playerName = (nameInput != null && !string.IsNullOrWhiteSpace(nameInput.text))
            ? nameInput.text.Trim()
            : "Player";

        // 2) إنشاء لوبي
        await CreateLobby(session);

        // 3) روح ShareCode
        SceneManager.LoadScene(shareCodeSceneName);
    }

    async Task CreateLobby(AppSession session)
    {
        try
        {
            string lobbyName = "WaterIce-" + Random.Range(1000, 9999);

            // نخزن بيانات اللاعب داخل اللوبي (عشان تظهر أسماء اللاعبين في الويتنج روم)
            var playerData = new Dictionary<string, PlayerDataObject>
            {
                { "name", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, session.playerName) }
            };

            var createOptions = new CreateLobbyOptions
            {
                IsPrivate = true,
                Player = new Player { Data = playerData },
                Data = new Dictionary<string, DataObject>
                {
                    { "roundTime", new DataObject(DataObject.VisibilityOptions.Public, session.roundTimeMinutes.ToString()) },
                    { "maxPlayers", new DataObject(DataObject.VisibilityOptions.Public, session.maxPlayers.ToString()) }
                }
            };

            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, session.maxPlayers, createOptions);

            session.lobbyId = lobby.Id;
            session.lobbyCode = lobby.LobbyCode;

            Debug.Log($"Lobby Created! Code={session.lobbyCode}  Id={session.lobbyId}");
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError("CreateLobby failed: " + e);
        }
    }

    // تربطيها بأزرار اختيار اللاعبين (3/6/9)
    public void SetPlayers(int players)
    {
        AppSession.Instance.maxPlayers = players;
        Debug.Log("Selected Players: " + players);
    }

    // تربطيها بأزرار اختيار الوقت
    public void SetRoundTime(int minutes)
    {
        AppSession.Instance.roundTimeMinutes = minutes;
        Debug.Log("Selected Time: " + minutes);
    }
}
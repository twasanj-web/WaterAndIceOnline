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

public class JoinLobbyByCodeUI : MonoBehaviour
{
    [Header("UI")]
    public TMP_InputField codeInput;
    public TMP_Text errorText;

    private bool isJoining;

    private async void Start()
    {
        await InitServices();
        if (errorText != null) errorText.text = "";
    }

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

        var session = AppSession.Instance;
        if (session != null)
            session.playerId = AuthenticationService.Instance.PlayerId;
    }

    public async void JoinWithCode()
    {
        if (isJoining) return;
        isJoining = true;

        try
        {
            if (errorText != null) errorText.text = "";
            if (codeInput == null) return;

            string code = codeInput.text.Trim().ToUpper();
            if (string.IsNullOrWhiteSpace(code))
            {
                ShowError("اكتب/ي الكود أولاً");
                return;
            }

            var session = AppSession.Instance;
            string playerName = (session != null && !string.IsNullOrWhiteSpace(session.playerName))
                ? session.playerName.Trim()
                : "Player";

            Debug.Log($"Joining lobby with code: {code} | name={playerName}");

            var joinOptions = new JoinLobbyByCodeOptions
            {
                Player = new Player
                {
                    Data = new Dictionary<string, PlayerDataObject>
                    {
                        { "name", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, playerName) }
                    }
                }
            };

            Lobby joinedLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(code, joinOptions);

            if (session != null)
            {
                session.isHost = false;
                session.lobbyCode = code;
                session.lobbyId = joinedLobby.Id;

                if (joinedLobby.Data != null)
                {
                    if (joinedLobby.Data.ContainsKey("maxPlayers"))
                        int.TryParse(joinedLobby.Data["maxPlayers"].Value, out session.maxPlayers);

                    if (joinedLobby.Data.ContainsKey("roundTime"))
                        int.TryParse(joinedLobby.Data["roundTime"].Value, out session.roundTimeMinutes);
                }
            }

            SceneManager.LoadScene("WaitingRoom-New");
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError("JoinLobbyByCode failed: " + e);
            ShowError("الكود غير صحيح أو الغرفة ممتلئة/مقفلة");
        }
        finally
        {
            isJoining = false;
        }
    }

    private void ShowError(string msg)
    {
        if (errorText != null) errorText.text = msg;
    }
}
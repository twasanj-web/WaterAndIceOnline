using UnityEngine;
using TMPro;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;

public class JoinLobbyByCodeUI : MonoBehaviour
{
    [Header("UI")]
    public TMP_InputField codeInput;   // حقل إدخال الكود
    public TMP_Text errorText;         // اختياري: نص للأخطاء

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
            var options = new InitializationOptions().SetEnvironmentName("development");
            await UnityServices.InitializeAsync(options);
        }

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
    }

    // اربطيها بزر الصح ✅
    public async void JoinWithCode()
    {
        if (isJoining) return;
        isJoining = true;

        try
        {
            if (errorText != null) errorText.text = "";

            if (codeInput == null)
            {
                Debug.LogError("codeInput not assigned!");
                return;
            }

            string code = codeInput.text.Trim().ToUpper();
            if (string.IsNullOrWhiteSpace(code))
            {
                ShowError("اكتبي/اكتب الكود أولاً");
                return;
            }

            Debug.Log("Joining lobby with code: " + code);

            Lobby joinedLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(code);

            // خزني بيانات اللوبي في AppSession
            var session = AppSession.Instance;
            if (session != null)
            {
                session.isHost = false;
                session.lobbyCode = code;
                session.lobbyId = joinedLobby.Id;

                // لو الهوست مخزنها في Data (زي كودك الحالي)
                if (joinedLobby.Data != null)
                {
                    if (joinedLobby.Data.ContainsKey("maxPlayers"))
                        int.TryParse(joinedLobby.Data["maxPlayers"].Value, out session.maxPlayers);

                    if (joinedLobby.Data.ContainsKey("roundTime"))
                        int.TryParse(joinedLobby.Data["roundTime"].Value, out session.roundTimeMinutes);
                }
            }

            // روحي لغرفة الانتظار
            SceneManager.LoadScene("WaitingRoom");
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError("JoinLobbyByCode failed: " + e);
            ShowError("الكود غير صحيح أو الغرفة مقفلة");
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
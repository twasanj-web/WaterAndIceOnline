using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using TMPro;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class WaitingRoomStartGame : MonoBehaviour // عدنا لـ MonoBehaviour ليعمل فوراً
{
    public TMP_Text statusText; 
    public GameObject startButton; 

    private Lobby _currentLobby;

    private void Start()
    {
        if (startButton != null) startButton.SetActive(false);

        UpdateStatusText();

        // الاشتراك في أحداث الشبكة
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientChanged;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientChanged;
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientChanged;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientChanged;
        }
    }

    private void OnClientChanged(ulong clientId)
    {
        UpdateStatusText();
    }

    private void UpdateStatusText()
    {
        if (NetworkManager.Singleton == null || statusText == null) return;

        int netcodeCount = NetworkManager.Singleton.ConnectedClients.Count;
        int maxCount = AppSession.Instance != null ? AppSession.Instance.maxPlayers : 3;

        if (netcodeCount == 0) netcodeCount = 1;

        statusText.text = $"({netcodeCount}/{maxCount})";

        // التحقق من اكتمال العدد
        if (netcodeCount >= maxCount)
        {
            Debug.Log($"<color=green>✅ العدد اكتمل ({netcodeCount}/{maxCount})!</color>");
            
            // الهوست فقط هو من يرى الزر
            if (NetworkManager.Singleton.IsServer && startButton != null)
            {
                startButton.SetActive(true);
                Debug.Log("<color=cyan>🚀 السهم تم تفعيله الآن للهوست.</color>");
            }
        }
        else
        {
            if (startButton != null) startButton.SetActive(false);
        }
    }

    public async void OnArrowPressed()
    {
        if (!NetworkManager.Singleton.IsServer) return;

        try
        {
            if (AppSession.Instance != null && !string.IsNullOrEmpty(AppSession.Instance.lobbyId))
            {
                _currentLobby = await LobbyService.Instance.GetLobbyAsync(AppSession.Instance.lobbyId);
                
                var players = _currentLobby.Players;
                int randomIndex = UnityEngine.Random.Range(0, players.Count);
                string icePlayerId = players[randomIndex].Id;

                UpdateLobbyOptions options = new UpdateLobbyOptions
                {
                    Data = new Dictionary<string, DataObject> {
                        { "iceIds", new DataObject(DataObject.VisibilityOptions.Public, icePlayerId) }
                    }
                };
                await LobbyService.Instance.UpdateLobbyAsync(_currentLobby.Id, options);
            }

            // الانتقال للماب
            NetworkManager.Singleton.SceneManager.LoadScene("GameMap", LoadSceneMode.Single);
        }
        catch (Exception e)
        {
            Debug.LogWarning("⚠️ Error, starting anyway: " + e.Message);
            NetworkManager.Singleton.SceneManager.LoadScene("GameMap", LoadSceneMode.Single);
        }
    }
}

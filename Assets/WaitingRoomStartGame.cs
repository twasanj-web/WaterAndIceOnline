using System;
using UnityEngine;
using TMPro;
using Unity.Netcode;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine.SceneManagement;

public class WaitingRoomStartGame : MonoBehaviour
{
    public TMP_Text statusText; 
    public GameObject startButton; 

    private void Awake()
    {
        // هذه الرسالة يجب أن تظهر فور تشغيل المشهد!
        Debug.Log("<color=orange>📢 سكريبت WaitingRoomStartGame بدأ العمل الآن في Awake!</color>");
    }

    private void Start()
    {
        if (startButton != null) startButton.SetActive(false);
        UpdateStatusText();
    }

    private void Update()
    {
        // تحديث مستمر وبسيط للرقم والزر
        UpdateStatusText();
    }

    private void UpdateStatusText()
    {
        if (NetworkManager.Singleton == null || statusText == null) return;

        // جلب العدد الفعلي للمتصلين بالشبكة
        int netcodeCount = NetworkManager.Singleton.ConnectedClients.Count;
        int maxCount = AppSession.Instance != null ? AppSession.Instance.maxPlayers : 3;

        // إذا كنت الهوست لوحدك، العدد يجب أن يكون 1
        if (netcodeCount == 0) netcodeCount = 1;

        statusText.text = $"({netcodeCount}/{maxCount})";

        // إظهار السهم للهوست فقط إذا اكتمل العدد
        if (netcodeCount >= maxCount)
        {
            if (NetworkManager.Singleton.IsServer && startButton != null && !startButton.activeSelf)
            {
                startButton.SetActive(true);
                Debug.Log("<color=green>✅ تم إظهار السهم بنجاح!</color>");
            }
        }
        else
        {
            if (startButton != null && startButton.activeSelf) startButton.SetActive(false);
        }
    }

    public async void OnArrowPressed()
    {
        if (!NetworkManager.Singleton.IsServer) return;

        try
        {
            // توزيع الأدوار عشوائياً بشكل سريع
            if (AppSession.Instance != null && !string.IsNullOrEmpty(AppSession.Instance.lobbyId))
            {
                var lobby = await LobbyService.Instance.GetLobbyAsync(AppSession.Instance.lobbyId);
                int randomIndex = UnityEngine.Random.Range(0, lobby.Players.Count);
                string icePlayerId = lobby.Players[randomIndex].Id;

                await LobbyService.Instance.UpdateLobbyAsync(lobby.Id, new UpdateLobbyOptions {
                    Data = new System.Collections.Generic.Dictionary<string, DataObject> {
                        { "iceIds", new DataObject(DataObject.VisibilityOptions.Public, icePlayerId) }
                    }
                });
            }
            
            // الانتقال للماب لجميع اللاعبين
            NetworkManager.Singleton.SceneManager.LoadScene("GameMap", LoadSceneMode.Single);
        }
        catch (Exception e)
        {
            Debug.LogWarning("Error starting game: " + e.Message);
            NetworkManager.Singleton.SceneManager.LoadScene("GameMap", LoadSceneMode.Single);
        }
    }
}

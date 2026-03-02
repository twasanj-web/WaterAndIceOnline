using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using TMPro;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class WaitingRoomStartGame : MonoBehaviour
{
    public TMP_Text statusText;
    public GameObject startButton; // السهم

    private Lobby currentLobby;
    private float updateTimer = 0f;
    private const float UPDATE_INTERVAL = 3f; // تحديث كل 3 ثوانٍ لتجنب حظر Unity

    private async void Start()
    {
        if (startButton != null) startButton.SetActive(false);
        await RefreshLobby();
    }

    private async void Update()
    {
        // تحديث دوري للوبي للتأكد من عدد اللاعبين
        updateTimer += Time.deltaTime;
        if (updateTimer >= UPDATE_INTERVAL)
        {
            updateTimer = 0f;
            await RefreshLobby();
        }
    }

    private async Task RefreshLobby()
    {
        try
        {
            if (AppSession.Instance == null || string.IsNullOrEmpty(AppSession.Instance.lobbyId)) return;

            currentLobby = await LobbyService.Instance.GetLobbyAsync(AppSession.Instance.lobbyId);
            int currentCount = currentLobby.Players.Count;
            int maxCount = AppSession.Instance.maxPlayers;

            if (statusText != null)
                statusText.text = $"ننتظر الباقين يدخلون... ({currentCount}/{maxCount})";

            // إظهار السهم للهوست فقط إذا اكتمل العدد
            if (AppSession.Instance.isHost && startButton != null)
            {
                startButton.SetActive(currentCount >= maxCount);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("Lobby Refresh Error: " + e.Message);
        }
    }

    public async void OnArrowPressed()
    {
        // 1. التحقق من أن الضغط من طرف الهوست
        if (!NetworkManager.Singleton.IsServer) return;

        Debug.Log("Arrow Clicked -> Starting Role Assignment and Scene Load");

        try
        {
            // 2. توزيع الأدوار عشوائياً (اختيار لاعب واحد ليكون الثلج)
            var players = currentLobby.Players;
            int randomIndex = UnityEngine.Random.Range(0, players.Count);
            string icePlayerId = players[randomIndex].Id;

            // 3. تحديث بيانات اللوبي بالأدوار ليعرفها الجميع عند الانتقال
            UpdateLobbyOptions options = new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject> {
                    { "iceIds", new DataObject(DataObject.VisibilityOptions.Public, icePlayerId) }
                }
            };
            await LobbyService.Instance.UpdateLobbyAsync(currentLobby.Id, options);

            Debug.Log($"Role Assigned: Player {icePlayerId} is ICE. Loading Scene...");

            // 4. الانتقال للماب عبر الشبكة (هذا هو السر! سينقل الجميع معاً)
            // تأكد أن اسم السين "GameMap" مضاف في الـ Build Settings
            NetworkManager.Singleton.SceneManager.LoadScene("GameMap", LoadSceneMode.Single);
        }
        catch (Exception e)
        {
            Debug.LogError("Error during game start: " + e.Message);
        }
    }
}

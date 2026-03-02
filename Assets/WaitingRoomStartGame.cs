using System;
using System.Collections.Generic;
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
    public GameObject startButton; 

    private Lobby currentLobby;
    private float updateTimer = 0f;
    private const float UPDATE_INTERVAL = 6.0f; // تحديث كل 6 ثوانٍ (آمن جداً)
    private bool isRefreshing = false;

    private async void Start()
    {
        if (startButton != null) startButton.SetActive(false);
        
        // تحديث أولي عند الدخول
        await RefreshLobby();
    }

    private async void Update()
    {
        // نحدث فقط إذا لم نكن في حالة "Refreshing" لتجنب تراكم الطلبات
        if (isRefreshing) return;

        updateTimer += Time.deltaTime;
        if (updateTimer >= UPDATE_INTERVAL)
        {
            updateTimer = 0f;
            await RefreshLobby();
        }
    }

    private async Task RefreshLobby()
    {
        if (isRefreshing) return;
        isRefreshing = true;

        try
        {
            if (AppSession.Instance == null || string.IsNullOrEmpty(AppSession.Instance.lobbyId)) return;

            // جلب بيانات اللوبي
            currentLobby = await LobbyService.Instance.GetLobbyAsync(AppSession.Instance.lobbyId);
            
            // تحديث الواجهة بناءً على عدد اللاعبين المتصلين فعلياً في Netcode
            // ملاحظة: NetworkManager.Singleton.ConnectedClients.Count يعطينا العدد الفعلي المتصل بالشبكة حالياً
            int netcodeCount = NetworkManager.Singleton.ConnectedClients.Count;
            int maxCount = AppSession.Instance.maxPlayers;

            if (statusText != null)
                statusText.text = $"ننتظر الباقين يدخلون... ({netcodeCount}/{maxCount})";

            // إظهار السهم للهوست فقط إذا اكتمل العدد في Netcode
            if (NetworkManager.Singleton.IsServer && startButton != null)
            {
                startButton.SetActive(netcodeCount >= maxCount);
            }
        }
        catch (LobbyServiceException e) when (e.Reason == LobbyExceptionReason.RateLimited)
        {
            // إذا حدث حظر، ننتظر وقتاً أطول
            updateTimer = -5f; 
            Debug.LogWarning("Rate limit hit, cooling down...");
        }
        catch (Exception e)
        {
            Debug.LogWarning("Lobby Refresh Error: " + e.Message);
        }
        finally
        {
            isRefreshing = false;
        }
    }

    public async void OnArrowPressed()
    {
        if (!NetworkManager.Singleton.IsServer) return;

        try
        {
            // اختيار الثلج عشوائياً من اللاعبين المتصلين حالياً
            var connectedIds = NetworkManager.Singleton.ConnectedClientsIds;
            int randomIndex = UnityEngine.Random.Range(0, connectedIds.Count);
            ulong iceNetworkId = connectedIds[randomIndex];
            
            // ملاحظة: هنا نحتاج الـ PlayerId الخاص بـ Unity Services وليس NetworkId
            // للتبسيط، سنعتمد على أن أول لاعب يدخل (الهوست) هو الثلج أو اختيار عشوائي بسيط
            string icePlayerId = currentLobby.Players[randomIndex].Id;

            UpdateLobbyOptions options = new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject> {
                    { "iceIds", new DataObject(DataObject.VisibilityOptions.Public, icePlayerId) }
                }
            };
            await LobbyService.Instance.UpdateLobbyAsync(currentLobby.Id, options);

            // الانتقال للماب عبر الشبكة
            NetworkManager.Singleton.SceneManager.LoadScene("GameMap", LoadSceneMode.Single);
        }
        catch (Exception e)
        {
            Debug.LogError("Error during game start: " + e.Message);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using TMPro;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class WaitingRoomStartGame : NetworkBehaviour
{
    public TMP_Text statusText; // الرقم الصغير (1/3)
    public GameObject startButton; // السهم

    private Lobby _currentLobby;

    private void Start()
    {
        // إخفاء السهم في البداية لضمان عدم ظهوره قبل اكتمال العدد
        if (startButton != null) startButton.SetActive(false);

        // تحديث أولي للرقم
        UpdateStatusText();

        // الاشتراك في أحداث دخول وخروج اللاعبين لتحديث الرقم لحظياً
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientChanged;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientChanged;
        }
    }

    public override void OnDestroy()
    {
        // إلغاء الاشتراك عند تدمير الكائن لمنع الأخطاء البرمجية
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientChanged;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientChanged;
        }
        base.OnDestroy();
    }

    private void OnClientChanged(ulong clientId)
    {
        // هذه الدالة ستعمل فوراً عند دخول أي لاعب جديد
        UpdateStatusText();
    }

    private void UpdateStatusText()
    {
        if (NetworkManager.Singleton == null || statusText == null) return;

        // جلب العدد الفعلي للمتصلين بالشبكة حالياً (بما في ذلك الهوست)
        int netcodeCount = NetworkManager.Singleton.ConnectedClients.Count;
        int maxCount = AppSession.Instance != null ? AppSession.Instance.maxPlayers : 3;

        // إذا كان الهوست لم يبدأ بعد أو في لحظة البداية، نعتبره 1
        if (netcodeCount == 0) netcodeCount = 1;

        // تحديث النص بالرقم فقط (3/3)
        statusText.text = $"({netcodeCount}/{maxCount})";

        // التحقق من اكتمال العدد وإظهار السهم للهوست
        if (netcodeCount >= maxCount)
        {
            Debug.Log($"<color=green>✅ العدد اكتمل ({netcodeCount}/{maxCount})!</color>");
            
            if (IsServer && startButton != null)
            {
                startButton.SetActive(true);
                Debug.Log("<color=cyan>🚀 السهم تم تفعيله الآن للهوست.</color>");
            }
            else if (startButton == null)
            {
                Debug.LogError("❌ خطأ: لم يتم ربط 'Start Button' في الـ Inspector!");
            }
        }
        else
        {
            // إخفاء السهم إذا غادر أحد اللاعبين ونقص العدد عن المطلوب
            if (startButton != null) startButton.SetActive(false);
        }
    }

    public async void OnArrowPressed()
    {
        if (!IsServer) return;

        try
        {
            Debug.Log("⌛ جاري توزيع الأدوار وبدء اللعبة...");

            // جلب بيانات اللوبي مرة واحدة فقط عند الضغط لتوزيع الأدوار
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
                Debug.Log($"✅ تم اختيار اللاعب {icePlayerId} ليكون الثلج.");
            }

            // الانتقال للماب لجميع اللاعبين عبر الشبكة
            Debug.Log("🌍 جاري نقل جميع اللاعبين إلى مشهد الماب...");
            NetworkManager.Singleton.SceneManager.LoadScene("GameMap", LoadSceneMode.Single);
        }
        catch (Exception e)
        {
            Debug.LogWarning("⚠️ حدث خطأ أثناء تحديث اللوبي، ولكن سنبدأ اللعبة على أي حال: " + e.Message);
            NetworkManager.Singleton.SceneManager.LoadScene("GameMap", LoadSceneMode.Single);
        }
    }
}

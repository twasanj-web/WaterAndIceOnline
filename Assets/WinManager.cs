using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WinManager : NetworkBehaviour
{
    [Header("Panels")]
    public GameObject winPanel;
    public GameObject iceWinPanel;
    public GameObject waterWinPanel;

    private NetworkVariable<bool> gameEnded = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private NetworkVariable<int> winnerRole = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    ); // 1 = ثلج فاز, 2 = ماء فاز

    public override void OnNetworkSpawn()
    {
        if (winPanel != null) winPanel.SetActive(false);
        if (iceWinPanel != null) iceWinPanel.SetActive(false);
        if (waterWinPanel != null) waterWinPanel.SetActive(false);

        // كل الأجهزة تستمع للتغيير
        gameEnded.OnValueChanged += OnGameEndedChanged;
    }

    private void Update()
    {
        if (!IsServer) return;
        if (gameEnded.Value) return;

        CheckAllWaterFrozen();
    }

    private void CheckAllWaterFrozen()
    {
        var allPlayers = FindObjectsOfType<NetworkPlayerMovement>();

        int waterCount = 0;
        int frozenWaterCount = 0;

        foreach (var player in allPlayers)
        {
            var visual = player.GetComponent<NetworkPlayerVisual>();
            if (visual == null) continue;

            if (visual.roleIndex.Value == 1) // ماء
            {
                waterCount++;
                if (player.isFrozen.Value)
                    frozenWaterCount++;
            }
        }

        if (waterCount > 0 && waterCount == frozenWaterCount)
        {
            winnerRole.Value = 1; // ثلج فاز
            gameEnded.Value = true;
        }
    }

    private void OnGameEndedChanged(bool oldVal, bool newVal)
    {
        if (!newVal) return;

        // إيقاف حركة جميع اللاعبين
        var allPlayers = FindObjectsOfType<NetworkPlayerMovement>();
        foreach (var player in allPlayers)
        {
            player.enabled = false;
        }

        // إخفاء واجهة اللعبة
        var gameUI = GameObject.Find("GameUI");
        if (gameUI != null) gameUI.SetActive(false);

        // إظهار البانل المناسب
        if (winPanel != null) winPanel.SetActive(true);

        if (winnerRole.Value == 1) // ثلج فاز
        {
            if (iceWinPanel != null) iceWinPanel.SetActive(true);
            if (waterWinPanel != null) waterWinPanel.SetActive(false);
        }
        else // ماء فاز
        {
            if (iceWinPanel != null) iceWinPanel.SetActive(false);
            if (waterWinPanel != null) waterWinPanel.SetActive(true);
        }
    }

    public void GoToMainMenu()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.Shutdown();
        SceneManager.LoadScene("MainMenu");
    }

    public void PlayAgain()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.Shutdown();
        SceneManager.LoadScene("MainMenu");
    }
}

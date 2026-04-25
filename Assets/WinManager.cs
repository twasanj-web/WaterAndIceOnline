using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WinManager : NetworkBehaviour
{
    [Header("Panels")]
    public GameObject winPanel;       // WinPanel (الأب)
    public GameObject iceWinPanel;    // IceWinPanel
    public GameObject waterWinPanel;  // WaterWinPanel

    private NetworkVariable<bool> gameEnded = new NetworkVariable<bool>(false);

    public override void OnNetworkSpawn()
    {
        // إخفاء البانلين في البداية
        if (winPanel != null) winPanel.SetActive(false);
        if (iceWinPanel != null) iceWinPanel.SetActive(false);
        if (waterWinPanel != null) waterWinPanel.SetActive(false);

        gameEnded.OnValueChanged += OnGameEnded;
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

        // لو كل لاعبي الماء مجمدين → الثلج يفوز
        if (waterCount > 0 && waterCount == frozenWaterCount)
            IceWinsClientRpc();
    }

    [ClientRpc]
    private void IceWinsClientRpc()
    {
        gameEnded.Value = true;

        if (winPanel != null) winPanel.SetActive(true);
        if (iceWinPanel != null) iceWinPanel.SetActive(true);
        if (waterWinPanel != null) waterWinPanel.SetActive(false);
    }

    private void OnGameEnded(bool oldVal, bool newVal) { }

    // زر الصفحة الرئيسية
    public void GoToMainMenu()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.Shutdown();
        SceneManager.LoadScene("MainMenu");
    }

    // زر العب مرة ثانية
    public void PlayAgain()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.Shutdown();
        SceneManager.LoadScene("MainMenu");
    }
}

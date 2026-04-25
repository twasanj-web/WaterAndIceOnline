using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WinManager : NetworkBehaviour
{
    [Header("Panels")]
    public GameObject winPanel;
    public GameObject iceWinPanel;
    public GameObject waterWinPanel;

    private NetworkVariable<bool> gameEnded = new NetworkVariable<bool>(false);

    public override void OnNetworkSpawn()
    {
        if (winPanel != null) winPanel.SetActive(false);
        if (iceWinPanel != null) iceWinPanel.SetActive(false);
        if (waterWinPanel != null) waterWinPanel.SetActive(false);
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
            gameEnded.Value = true; // ✅ يتم من الـ Server فقط
            IceWinsClientRpc();
        }
    }

    [ClientRpc]
    private void IceWinsClientRpc()
    {
        // إيقاف حركة جميع اللاعبين
        var allPlayers = FindObjectsOfType<NetworkPlayerMovement>();
        foreach (var player in allPlayers)
        {
            player.enabled = false; // يوقف الحركة
        }

        // إخفاء واجهة اللعبة (الجويستيك والأزرار)
        var gameUI = GameObject.Find("GameUI");
        if (gameUI != null) gameUI.SetActive(false);

        // إظهار بانل الفوز
        if (winPanel != null) winPanel.SetActive(true);
        if (iceWinPanel != null) iceWinPanel.SetActive(true);
        if (waterWinPanel != null) waterWinPanel.SetActive(false);
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

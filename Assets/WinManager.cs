using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WinManager : NetworkBehaviour
{
    [Header("Panels")]
    public GameObject winPanel;
    public GameObject iceWinPanel;
    public GameObject waterWinPanel;

    private bool gameEnded = false;

    public override void OnNetworkSpawn()
    {
        if (winPanel != null) winPanel.SetActive(false);
        if (iceWinPanel != null) iceWinPanel.SetActive(false);
        if (waterWinPanel != null) waterWinPanel.SetActive(false);
    }

    private void Update()
    {
        if (!IsServer) return;
        if (gameEnded) return;

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

            if (visual.roleIndex.Value == 1)
            {
                waterCount++;
                if (player.isFrozen.Value)
                    frozenWaterCount++;
            }
        }

        if (waterCount > 0 && waterCount == frozenWaterCount)
        {
            gameEnded = true;
            ShowIceWinClientRpc();
        }
    }

    [ClientRpc]
    private void ShowIceWinClientRpc()
    {
        Debug.Log("ShowIceWinClientRpc وصل! الجهاز: " + (IsHost ? "Host" : "Client"));

        if (winPanel == null) Debug.LogError("winPanel = null!");
        if (iceWinPanel == null) Debug.LogError("iceWinPanel = null!");

        var allPlayers = FindObjectsOfType<NetworkPlayerMovement>();
        foreach (var player in allPlayers)
            player.enabled = false;

        var gameUI = GameObject.Find("GameUI");
        if (gameUI != null) gameUI.SetActive(false);

        if (winPanel != null) winPanel.SetActive(true);
        if (iceWinPanel != null) iceWinPanel.SetActive(true);
        if (waterWinPanel != null) waterWinPanel.SetActive(false);
    }

    [ClientRpc]
    public void ShowWaterWinClientRpc()
    {
        Debug.Log("ShowWaterWinClientRpc وصل! الجهاز: " + (IsHost ? "Host" : "Client"));

        var allPlayers = FindObjectsOfType<NetworkPlayerMovement>();
        foreach (var player in allPlayers)
            player.enabled = false;

        var gameUI = GameObject.Find("GameUI");
        if (gameUI != null) gameUI.SetActive(false);

        if (winPanel != null) winPanel.SetActive(true);
        if (iceWinPanel != null) iceWinPanel.SetActive(false);
        if (waterWinPanel != null) waterWinPanel.SetActive(true);
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

using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WinManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject winPanel;
    public GameObject iceWinPanel;
    public GameObject waterWinPanel;

    private bool gameEnded = false;

    private void Start()
    {
        if (winPanel != null) winPanel.SetActive(false);
        if (iceWinPanel != null) iceWinPanel.SetActive(false);
        if (waterWinPanel != null) waterWinPanel.SetActive(false);
    }

    private void Update()
    {
        if (gameEnded) return;

        // كل الأجهزة تفحص بنفسها
        CheckAllWaterFrozen();
    }

    private void CheckAllWaterFrozen()
    {
        var allPlayers = FindObjectsOfType<NetworkPlayerMovement>();
        if (allPlayers.Length == 0) return;

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
            ShowIceWin();
        }
    }

    private void ShowIceWin()
    {
        Debug.Log("الثلج فاز على: " + (NetworkManager.Singleton.IsHost ? "Host" : "Client"));

        var allPlayers = FindObjectsOfType<NetworkPlayerMovement>();
        foreach (var player in allPlayers)
            player.enabled = false;

        var gameUI = GameObject.Find("GameUI");
        if (gameUI != null) gameUI.SetActive(false);

        if (winPanel != null) winPanel.SetActive(true);
        if (iceWinPanel != null) iceWinPanel.SetActive(true);
        if (waterWinPanel != null) waterWinPanel.SetActive(false);
    }

    public void ShowWaterWin()
    {
        gameEnded = true;

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

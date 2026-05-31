using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using System.Collections.Generic;

public class WinManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject winPanel;
    public GameObject iceWinPanel;
    public GameObject waterWinPanel;

    [Header("Timer UI")]
    public TextMeshProUGUI timerText;

    private bool gameEnded = false;
    private float timeRemaining;
    private bool isTimerRunning = false;

    private void Start()
    {
        if (winPanel != null) winPanel.SetActive(false);
        if (iceWinPanel != null) iceWinPanel.SetActive(false);
        if (waterWinPanel != null) waterWinPanel.SetActive(false);

        if (AppSession.Instance != null)
            timeRemaining = AppSession.Instance.roundTimeMinutes * 60f;
        else
            timeRemaining = 300f;

        isTimerRunning = true;
        UpdateTimerDisplay();
    }

    private void Update()
    {
        if (gameEnded) return;

        if (isTimerRunning)
        {
            UpdateSyncedTimer();

            if (timeRemaining <= 0)
            {
                timeRemaining = 0;
                isTimerRunning = false;
                UpdateTimerDisplay();

                gameEnded = true;
                ShowWaterWin();
                return;
            }
        }

        CheckAllWaterFrozen();
    }

    private void UpdateSyncedTimer()
    {
        var session = AppSession.Instance;

        if (session != null && session.gameStartUnixMs > 0)
        {
            long now = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            float totalTime = session.roundTimeMinutes * 60f;
            float elapsed = (now - session.gameStartUnixMs) / 1000f;

            timeRemaining = Mathf.Max(0f, totalTime - elapsed);
        }
        else
        {
            timeRemaining -= Time.deltaTime;
        }

        UpdateTimerDisplay();
    }

    private void UpdateTimerDisplay()
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(timeRemaining / 60);
            int seconds = Mathf.FloorToInt(timeRemaining % 60);
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }
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
            isTimerRunning = false;
            ShowIceWin();
        }
    }

    private void ShowIceWin()
    {
        Debug.Log("الثلج فاز!");

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
        Debug.Log("الماء فاز!");

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

    public async void PlayAgain()
    {
        var session = AppSession.Instance;

        if (session != null)
        {
            session.role = PlayerRole.None;
            session.relayJoinCode = "";
            session.hostAllocation = null;
            session.gameStartUnixMs = 0;
            session.returningToWaitingRoom = true;

            if (session.isHost && !string.IsNullOrWhiteSpace(session.lobbyId))
            {
                try
                {
                    await LobbyService.Instance.UpdateLobbyAsync(
                        session.lobbyId,
                        new UpdateLobbyOptions
                        {
                            Data = new Dictionary<string, DataObject>
                            {
                                { "state", new DataObject(DataObject.VisibilityOptions.Public, "waiting") },
                                { "iceIds", new DataObject(DataObject.VisibilityOptions.Public, "") },
                                { "relayCode", new DataObject(DataObject.VisibilityOptions.Public, "") },
                                { "startAt", new DataObject(DataObject.VisibilityOptions.Public, "0") }
                            }
                        }
                    );
                }
                catch (System.Exception e)
                {
                    Debug.LogError("Failed to reset lobby for play again: " + e);
                }
            }
        }

        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.Shutdown();

        SceneManager.LoadScene("WaitingRoom");
    }
}
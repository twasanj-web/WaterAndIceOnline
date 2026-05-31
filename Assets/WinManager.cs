using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class WinManager : NetworkBehaviour
{
    [Header("Panels")]
    public GameObject winPanel;
    public GameObject iceWinPanel;
    public GameObject waterWinPanel;

    [Header("Timer UI")]
    public TextMeshProUGUI timerText;

    private bool localGameEnded = false;

    private NetworkVariable<float> networkTimeRemaining = new NetworkVariable<float>(
        300f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private NetworkVariable<bool> networkTimerRunning = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private NetworkVariable<int> winnerTeam = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private void Awake()
    {
        if (winPanel != null) winPanel.SetActive(false);
        if (iceWinPanel != null) iceWinPanel.SetActive(false);
        if (waterWinPanel != null) waterWinPanel.SetActive(false);
    }

    public override void OnNetworkSpawn()
    {
        networkTimeRemaining.OnValueChanged += OnTimeChanged;
        winnerTeam.OnValueChanged += OnWinnerChanged;

        if (IsServer)
        {
            float startTime = 300f;

            if (AppSession.Instance != null)
                startTime = AppSession.Instance.roundTimeMinutes * 60f;

            networkTimeRemaining.Value = startTime;
            networkTimerRunning.Value = false;
            winnerTeam.Value = 0;

            Invoke(nameof(StartTimerServer), 3f);
        }

        UpdateTimerDisplay(networkTimeRemaining.Value);
    }

    private void Update()
    {
        if (!IsServer) return;
        if (localGameEnded) return;

        if (networkTimerRunning.Value)
        {
            if (networkTimeRemaining.Value > 0)
            {
                networkTimeRemaining.Value -= Time.deltaTime;

                if (networkTimeRemaining.Value <= 0)
                {
                    networkTimeRemaining.Value = 0;
                    networkTimerRunning.Value = false;
                    EndGameServer(1); // 1 = Water Win
                    return;
                }
            }
        }

        CheckAllWaterFrozenServer();
    }

    private void StartTimerServer()
    {
        if (!IsServer) return;
        if (localGameEnded) return;

        networkTimerRunning.Value = true;
    }

    private void CheckAllWaterFrozenServer()
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
            EndGameServer(2); // 2 = Ice Win
        }
    }

    private void EndGameServer(int winner)
    {
        if (!IsServer) return;
        if (localGameEnded) return;

        localGameEnded = true;
        networkTimerRunning.Value = false;
        winnerTeam.Value = winner;
    }

    private void OnTimeChanged(float oldValue, float newValue)
    {
        UpdateTimerDisplay(newValue);
    }

    private void OnWinnerChanged(int oldValue, int newValue)
    {
        if (newValue == 1)
        {
            ShowWaterWin();
        }
        else if (newValue == 2)
        {
            ShowIceWin();
        }
    }

    private void UpdateTimerDisplay(float time)
    {
        if (timerText == null) return;

        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);

        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    private void ShowIceWin()
    {
        Debug.Log("الثلج فاز!");

        StopPlayers();

        var gameUI = GameObject.Find("GameUI");
        if (gameUI != null) gameUI.SetActive(false);

        if (winPanel != null) winPanel.SetActive(true);
        if (iceWinPanel != null) iceWinPanel.SetActive(true);
        if (waterWinPanel != null) waterWinPanel.SetActive(false);
    }

    public void ShowWaterWin()
    {
        Debug.Log("الماء فاز!");

        StopPlayers();

        var gameUI = GameObject.Find("GameUI");
        if (gameUI != null) gameUI.SetActive(false);

        if (winPanel != null) winPanel.SetActive(true);
        if (iceWinPanel != null) iceWinPanel.SetActive(false);
        if (waterWinPanel != null) waterWinPanel.SetActive(true);
    }

    private void StopPlayers()
    {
        var allPlayers = FindObjectsOfType<NetworkPlayerMovement>();

        foreach (var player in allPlayers)
            player.enabled = false;
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

    public override void OnNetworkDespawn()
    {
        networkTimeRemaining.OnValueChanged -= OnTimeChanged;
        winnerTeam.OnValueChanged -= OnWinnerChanged;
    }
}
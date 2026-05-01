using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro; // ضروري للتعامل مع النصوص

public class WinManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject winPanel;
    public GameObject iceWinPanel;
    public GameObject waterWinPanel;

    [Header("Timer UI")]
    public TextMeshProUGUI timerText; // اسحبي نص التايمر هنا من الـ Inspector

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

        // انتظر 3 ثواني قبل بدء التايمر (حتى يتصل الجميع)
        Invoke(nameof(StartTimer), 3f);
        UpdateTimerDisplay();
    }

    private void StartTimer()
    {
        isTimerRunning = true;
    }


    private void Update()
    {
        if (gameEnded) return;

        // تحديث التايمر
        if (isTimerRunning)
        {
            if (timeRemaining > 0)
            {
                timeRemaining -= Time.deltaTime;
                UpdateTimerDisplay();
            }
            else
            {
                // انتهى الوقت!
                timeRemaining = 0;
                isTimerRunning = false;
                UpdateTimerDisplay();
                
                // فوز الماء
                gameEnded = true;
                ShowWaterWin();
                return;
            }
        }

        // فحص فوز الثلج
        CheckAllWaterFrozen();
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

            if (visual.roleIndex.Value == 1) // ماء
            {
                waterCount++;
                if (player.isFrozen.Value)
                    frozenWaterCount++;
            }
        }

        // إذا كان هناك لاعبو ماء وكلهم مجمدين -> فوز الثلج
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
        Debug.Log("الماء فاز (انتهى الوقت)!");

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

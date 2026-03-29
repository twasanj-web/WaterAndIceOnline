using Unity.Netcode;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class GameManager : NetworkBehaviour
{
    [Header("UI - نتيجة اللعبة")]
    public GameObject winPanel;
    public TMP_Text winText;

    [Header("Timing")]
    public float returnToMenuDelay = 5f;

    private bool gameEnded = false;

    private void Update()
    {
        if (!IsServer) return;
        if (gameEnded) return;

        CheckAllWaterFrozen();
    }

    void CheckAllWaterFrozen()
    {
        FreezeAbility[] allPlayers = FindObjectsByType<FreezeAbility>(FindObjectsSortMode.None);

        int waterCount = 0;
        int frozenWaterCount = 0;

        foreach (var player in allPlayers)
        {
            NetworkPlayerVisual vis = player.GetComponent<NetworkPlayerVisual>();
            if (vis == null) continue;

            if (vis.roleIndex.Value == 1)
            {
                waterCount++;
                if (player.isFrozen.Value)
                    frozenWaterCount++;
            }
        }

        if (waterCount == 0) return;

        if (frozenWaterCount >= waterCount)
        {
            EndGame(iceWins: true);
        }
    }

    public void OnTimeUp()
    {
        if (gameEnded) return;

        FreezeAbility[] allPlayers = FindObjectsByType<FreezeAbility>(FindObjectsSortMode.None);
        int waterCount = 0;
        int frozenWaterCount = 0;

        foreach (var player in allPlayers)
        {
            NetworkPlayerVisual vis = player.GetComponent<NetworkPlayerVisual>();
            if (vis == null) continue;

            if (vis.roleIndex.Value == 1)
            {
                waterCount++;
                if (player.isFrozen.Value)
                    frozenWaterCount++;
            }
        }

        bool waterWins = (waterCount - frozenWaterCount) > 0;
        EndGame(iceWins: !waterWins);
    }

    void EndGame(bool iceWins)
    {
        if (gameEnded) return;
        gameEnded = true;

        GameTimer timer = FindFirstObjectByType<GameTimer>();
        if (timer != null) timer.StopTimer();

        ShowResultClientRpc(iceWins);
    }

    [ClientRpc]
    void ShowResultClientRpc(bool iceWins)
    {
        string winner = iceWins ? "فاز فريق الثلج ❄️" : "فاز فريق الماء 💧";

        if (winPanel != null) winPanel.SetActive(true);
        if (winText != null) winText.text = winner;

        Debug.Log($"[GameManager] Showing result: {winner}");

        Invoke(nameof(ReturnToMenu), returnToMenuDelay);
    }

    void ReturnToMenu()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.Shutdown();

        SceneManager.LoadScene("MainMenu");
    }
}

using Unity.Netcode;
using UnityEngine;
using TMPro;

public class GameTimer : NetworkBehaviour
{
    [Header("UI")]
    public TMP_Text timerText;

    public NetworkVariable<float> timeRemaining = new NetworkVariable<float>(
        300f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public NetworkVariable<bool> isRunning = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private bool gameEnded = false;

    public override void OnNetworkSpawn()
    {
        timeRemaining.OnValueChanged += OnTimeChanged;
        isRunning.OnValueChanged += OnRunningChanged;

        if (IsServer)
        {
            int minutes = 5;
            if (AppSession.Instance != null)
                minutes = AppSession.Instance.roundTimeMinutes;

            timeRemaining.Value = minutes * 60f;
            isRunning.Value = true;
            Debug.Log($"[GameTimer] Started: {minutes} minutes");
        }

        UpdateTimerUI(timeRemaining.Value);
    }

    private void Update()
    {
        if (!IsServer) return;
        if (!isRunning.Value) return;
        if (gameEnded) return;

        timeRemaining.Value -= Time.deltaTime;

        if (timeRemaining.Value <= 0f)
        {
            timeRemaining.Value = 0f;
            isRunning.Value = false;
            GameManager gm = FindFirstObjectByType<GameManager>();
            if (gm != null) gm.OnTimeUp();
        }
    }

    void OnTimeChanged(float oldVal, float newVal)
    {
        UpdateTimerUI(newVal);
    }

    void OnRunningChanged(bool oldVal, bool newVal) { }

    void UpdateTimerUI(float seconds)
    {
        if (timerText == null) return;
        int m = Mathf.FloorToInt(seconds / 60f);
        int s = Mathf.FloorToInt(seconds % 60f);
        timerText.text = $"{m:00}:{s:00}";
    }

    public void StopTimer()
    {
        if (IsServer) isRunning.Value = false;
    }

    public override void OnNetworkDespawn()
    {
        timeRemaining.OnValueChanged -= OnTimeChanged;
        isRunning.OnValueChanged -= OnRunningChanged;
    }
}

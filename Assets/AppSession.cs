using UnityEngine;

public enum PlayerRole
{
    None,
    Water,
    Ice
}

public class AppSession : MonoBehaviour
{
    public static AppSession Instance { get; private set; }

    [Header("Player")]
    public string playerName = "Player";
    public string playerId = "";        // Authentication PlayerId
    public PlayerRole role = PlayerRole.None;

    [Header("Room Settings")]
    public int maxPlayers = 3;
    public int roundTimeMinutes = 5;

    [Header("Lobby Info")]
    public string lobbyCode = "";
    public string lobbyId = "";
    public bool isHost = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetPlayerName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            playerName = "Player";
        else
            playerName = name.Trim();
    }
}
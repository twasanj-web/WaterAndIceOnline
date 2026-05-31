using UnityEngine;
using Unity.Services.Relay.Models;

public enum PlayerRole
{
    None = 0,
    Water = 1,
    Ice = 2
}

public class AppSession : MonoBehaviour
{
    public static AppSession Instance { get; private set; }
  

    [Header("Player")]
    public string playerName = "Player";
    public string playerId = "";
    public PlayerRole role = PlayerRole.None;

    [Header("Room Settings")]
    public int maxPlayers = 3;
    public int roundTimeMinutes = 5;

    [Header("Lobby Info")]
    public string lobbyCode = "";
    public string lobbyId = "";
    public bool isHost = false;

    [Header("Relay")]
    public string relayJoinCode = "";
    public Allocation hostAllocation = null;

    [Header("Runtime")]
    public int currentPlayerCount = 0;
    public bool returningToWaitingRoom = false;

    [Header("Game Time")]
    public long gameStartUnixMs = 0;

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
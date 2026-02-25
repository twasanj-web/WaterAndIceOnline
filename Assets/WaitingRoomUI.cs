using UnityEngine;
using TMPro;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class WaitingRoomUI : MonoBehaviour
{
    private bool rolesAssigned = false;
    private bool sceneLoaded = false;
    
    [Header("UI")]
    public TMP_Text statusText;        // (1/3)
    public TMP_Text[] nameSlots;       // Name(1) ... Name(9)

    [Header("Refresh")]
    public float refreshSeconds = 1.0f;

    private Coroutine pollRoutine;

    private async void Start()
    {
        // صفّر UI أولاً
        ClearSlots();
        if (statusText != null) statusText.text = "(0/0)";

        await InitServices();

        var session = AppSession.Instance;
        if (session == null || string.IsNullOrWhiteSpace(session.lobbyId))
        {
            Debug.LogError("WaitingRoomUI: no AppSession or lobbyId is empty!");
            return;
        }

        pollRoutine = StartCoroutine(PollLobby(session.lobbyId));
    }

    private void OnDestroy()
    {
        if (pollRoutine != null)
            StopCoroutine(pollRoutine);
    }

    private void ClearSlots()
    {
        if (nameSlots == null) return;
        for (int i = 0; i < nameSlots.Length; i++)
            if (nameSlots[i] != null) nameSlots[i].text = "";
    }

    private async Task InitServices()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            var options = new InitializationOptions().SetEnvironmentName("development");
            await UnityServices.InitializeAsync(options);
        }

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
    }

    private IEnumerator PollLobby(string lobbyId)
    {
        while (true)
        {
            var task = RefreshLobbyUI(lobbyId);
            while (!task.IsCompleted) yield return null;

            yield return new WaitForSeconds(refreshSeconds);
        }
    }

    private async Task RefreshLobbyUI(string lobbyId)
    {
        try
        {
            Lobby lobby = await LobbyService.Instance.GetLobbyAsync(lobbyId);

            int current = lobby.Players != null ? lobby.Players.Count : 0;
            int max = lobby.MaxPlayers;

            if (statusText != null)
                statusText.text = $"({current}/{max})";

            ClearSlots();

            if (lobby.Players == null || nameSlots == null) return;

            int slotsCount = Mathf.Min(nameSlots.Length, lobby.Players.Count);
            for (int i = 0; i < slotsCount; i++)
            {
                string name = GetPlayerName(lobby.Players[i], i);
                if (nameSlots[i] != null) nameSlots[i].text = name;
            }

            // ✅ THIS PART MUST BE INSIDE THIS METHOD
            if (current == max)
            {
                var session = AppSession.Instance;

                if (session != null && session.isHost && !rolesAssigned)
                {
                    rolesAssigned = true;
                    await AssignRoles(lobby);
                }

                if (!sceneLoaded)
                {
                    sceneLoaded = true;
                    LoadMyRoleScene(lobby);   // 👈 lobby exists here
                }
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError("GetLobbyAsync failed: " + e);
        }
    }

    private string GetPlayerName(Player p, int index)
    {
        if (p != null && p.Data != null && p.Data.ContainsKey("name"))
        {
            string v = p.Data["name"].Value;
            if (!string.IsNullOrWhiteSpace(v)) return v.Trim();
        }
        return $"Player {index + 1}";
    }
    
    private async Task AssignRoles(Lobby lobby)
    {
        int totalPlayers = lobby.Players.Count;

        int iceCount = totalPlayers / 3;     // 3→1, 6→2, 9→3
        int waterCount = totalPlayers - iceCount;

        Debug.Log($"Assigning Roles: Water={waterCount}, Ice={iceCount}");

        // Create role list
        List<string> roles = new List<string>();

        for (int i = 0; i < waterCount; i++)
            roles.Add("Water");

        for (int i = 0; i < iceCount; i++)
            roles.Add("Ice");

        // Shuffle roles
        for (int i = 0; i < roles.Count; i++)
        {
            int randomIndex = Random.Range(i, roles.Count);
            string temp = roles[i];
            roles[i] = roles[randomIndex];
            roles[randomIndex] = temp;
        }

        // Assign to players
        for (int i = 0; i < lobby.Players.Count; i++)
        {
            string role = roles[i];

            var updateOptions = new UpdatePlayerOptions
            {
                Data = new Dictionary<string, PlayerDataObject>
                {
                    { "role", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, role) }
                }
            };

            await LobbyService.Instance.UpdatePlayerAsync(
                lobby.Id,
                lobby.Players[i].Id,
                updateOptions
            );
        }

        Debug.Log("Roles Assigned Successfully");
    }
    
    private void LoadMyRoleScene(Lobby lobby)
    {
        var session = AppSession.Instance;
        if (session == null) return;

        string myPlayerId =
            Unity.Services.Authentication.AuthenticationService.Instance.PlayerId;

        foreach (Player p in lobby.Players)
        {
            if (p.Id == myPlayerId && p.Data.ContainsKey("role"))
            {
                string role = p.Data["role"].Value;
                session.myRole = role;

                if (role == "Ice")
                    SceneManager.LoadScene("IceScene");
                else
                    SceneManager.LoadScene("WaterScene");

                return;
            }
        }
    }
}
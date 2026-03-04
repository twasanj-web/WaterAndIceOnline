using UnityEngine;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using TMPro;
using System.Collections.Generic;
using System.Threading.Tasks;

public class WaitingRoomFixedSlots : MonoBehaviour
{
    public List<GameObject> playerSlots;   // الصور كاملة
    public List<TMP_Text> nameTexts;       // النصوص

    async void Start()
    {
        await LoadPlayers();
    }

    async Task LoadPlayers()
    {
        var session = AppSession.Instance;
        Lobby lobby = await LobbyService.Instance.GetLobbyAsync(session.lobbyId);

        List<Player> players = lobby.Players;
        int playerCount = players.Count;

        for (int i = 0; i < playerSlots.Count; i++)
        {
            if (i < playerCount)
            {
                playerSlots[i].SetActive(true);

                if (players[i].Data != null && players[i].Data.ContainsKey("name"))
                    nameTexts[i].text = players[i].Data["name"].Value;
                else
                    nameTexts[i].text = "Player";
            }
            else
            {
                playerSlots[i].SetActive(false);
            }
        }
    }
}
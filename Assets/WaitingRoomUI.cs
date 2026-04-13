using UnityEngine;
using TMPro;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class WaitingRoomUI : MonoBehaviour
{
    [Header("UI Elements")]
    public TMP_Text statusText;
    public GameObject[] imageSlots;
    public TMP_Text[] nameSlots;
    public GameObject startButton;

    [Header("Settings")]
    public float refreshSeconds = 5f;

    private bool hasMovedToGame = false;

    private void Start()
    {
        if (imageSlots != null) foreach (var slot in imageSlots) if (slot != null) slot.SetActive(false);
        if (nameSlots != null) foreach (var n in nameSlots) if (n != null) n.text = "";
        
        if (startButton != null)
        {
            bool isHost = AppSession.Instance != null && AppSession.Instance.isHost;
            startButton.SetActive(isHost);
        }

        if (AppSession.Instance != null && !string.IsNullOrEmpty(AppSession.Instance.lobbyId))
        {
            StartCoroutine(RefreshLobbyLoop());
        }
    }

    private IEnumerator RefreshLobbyLoop()
    {
        while (!hasMovedToGame)
        {
            RefreshLobbyData();
            yield return new WaitForSeconds(refreshSeconds);
        }
    }

    private async void RefreshLobbyData()
    {
        if (AppSession.Instance == null || string.IsNullOrEmpty(AppSession.Instance.lobbyId) || hasMovedToGame) return;

        try
        {
            Lobby lobby = await LobbyService.Instance.GetLobbyAsync(AppSession.Instance.lobbyId);
            if (lobby == null || lobby.Players == null) return;

            if (statusText != null) statusText.text = $"({lobby.Players.Count}/{lobby.MaxPlayers})";


            int playersCount = lobby.Players.Count;

            for (int i = 0; i < imageSlots.Length; i++)
            {
                if (imageSlots[i] == null) continue;

                if (i < playersCount)
                {
                    imageSlots[i].SetActive(true);
                    if (i < nameSlots.Length && nameSlots[i] != null)
                    {
                        var p = lobby.Players[i];
                        string pName = "Player";
                        if (p.Data != null && p.Data.ContainsKey("name")) pName = p.Data["name"].Value;
                        nameSlots[i].text = pName;
                    }
                }
                else
                {
                    imageSlots[i].SetActive(false);
                }
            }

            if (!AppSession.Instance.isHost && lobby.Data != null && lobby.Data.ContainsKey("state"))
            {
                if (lobby.Data["state"].Value == "started")
                {
                    hasMovedToGame = true;
                    SceneManager.LoadScene("GameMap");
                }
            }
        }
        catch (System.Exception e) { Debug.LogWarning(e.Message); }
    }
}

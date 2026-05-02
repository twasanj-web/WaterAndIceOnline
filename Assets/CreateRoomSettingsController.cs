using UnityEngine;
using UnityEngine.SceneManagement;

public class CreateRoomSettingsController : MonoBehaviour
{
    public int selectedTime = 5;
    public int selectedPlayers = 6;

    public void SelectTime(int time)
    {
        selectedTime = time;
        PlayerPrefs.SetInt("room_time", selectedTime);
        Debug.Log("Selected Time: " + selectedTime);
    }

    public void SelectPlayers(int players)
    {
        selectedPlayers = players;
        PlayerPrefs.SetInt("room_players", selectedPlayers);
        Debug.Log("Selected Players: " + selectedPlayers);
    }

    public void StartGame()
    {
        if (AppSession.Instance != null)
        {
            AppSession.Instance.maxPlayers = PlayerPrefs.GetInt("room_players", selectedPlayers);
            AppSession.Instance.roundTimeMinutes = PlayerPrefs.GetInt("room_time", selectedTime);
        }
        PlayerPrefs.Save();
        SceneManager.LoadScene("ShareCode");
    }

    public void GoBack()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
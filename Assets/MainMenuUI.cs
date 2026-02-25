using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuUI : MonoBehaviour
{
    public void GoToCreateRoom()
    {
        SceneManager.LoadScene("CreateRoomSettings");
    }

    public void GoToJoinRoom()
    {
        SceneManager.LoadScene("Enter Code");
    }
}
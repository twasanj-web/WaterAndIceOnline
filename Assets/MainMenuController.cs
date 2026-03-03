using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenuController : MonoBehaviour
{
    // اختياري: عشان الاسم من البوب اب
    public TMP_Text nameButtonText;

    public void GoToCreateRoomSettings()
    {
        SceneManager.LoadScene("CreateRoomSettings");
    }

    public void GoToJoinRoom()
    {
        SceneManager.LoadScene("Enter Code"); // ← غيّر من JoinRoom لـ Enter Code
    }
    }
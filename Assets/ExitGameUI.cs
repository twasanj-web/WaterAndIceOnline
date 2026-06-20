using UnityEngine;
using UnityEngine.SceneManagement;

public class ExitGameUI : MonoBehaviour
{
    public GameObject exitPopup;

    private void Start()
    {
        exitPopup.SetActive(false);
    }

    public void OpenExitPopup()
    {
        exitPopup.SetActive(true);
    }

    public void CloseExitPopup()
    {
        exitPopup.SetActive(false);
    }

    public void ExitGame()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
using TMPro;
using UnityEngine;

public class NamePopupController : MonoBehaviour
{
    public GameObject namePopup;
    public TMP_InputField nameInput;
    public TMP_Text nameButtonText;

    private const string KEY = "player_name";

    void Start()
    {
        if (namePopup != null)
            namePopup.SetActive(false);

        string saved = PlayerPrefs.GetString(KEY, "Player");

        if (nameButtonText != null)
            nameButtonText.text = saved;

        if (nameInput != null)
            nameInput.text = saved;

        if (AppSession.Instance != null)
            AppSession.Instance.playerName = saved;
    }

    public void OpenPopup()
    {
        if (namePopup == null) return;

        namePopup.SetActive(true);

        if (nameInput != null)
        {
            nameInput.text = nameButtonText != null ? nameButtonText.text : nameInput.text;
            nameInput.ActivateInputField();
        }
    }

    public void ClosePopup()
    {
        if (namePopup == null) return;

        namePopup.SetActive(false);
    }

    public void Confirm()
    {
        if (nameInput == null) return;

        string playerName = nameInput.text.Trim();

        if (string.IsNullOrEmpty(playerName))
            playerName = "Player";

        PlayerPrefs.SetString(KEY, playerName);
        PlayerPrefs.Save();

        if (nameButtonText != null)
            nameButtonText.text = playerName;

        if (AppSession.Instance != null)
            AppSession.Instance.playerName = playerName;

        Debug.Log("Player name saved: " + playerName);

        ClosePopup();
    }
}
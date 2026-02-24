using TMPro;
using UnityEngine;

public class NamePopupController : MonoBehaviour
{
    public GameObject namePopup;      // NamePopup
    public TMP_InputField nameInput;  // NameInput
    public TMP_Text nameButtonText;   // النص داخل زر الاسم (Text TMP)

    const string KEY = "player_name";

    void Start()
    {
        if (namePopup != null) namePopup.SetActive(false);

        string saved = PlayerPrefs.GetString(KEY, "Player");
        if (nameButtonText != null) nameButtonText.text = saved;
        if (nameInput != null) nameInput.text = saved;
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

        string n = nameInput.text.Trim();
        if (string.IsNullOrEmpty(n)) return;

        PlayerPrefs.SetString(KEY, n);
        PlayerPrefs.Save();

        if (nameButtonText != null) nameButtonText.text = n;

        ClosePopup();
    }
}
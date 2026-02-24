using UnityEngine;
using TMPro;

public class PlayerNameBinder : MonoBehaviour
{
    public TMP_InputField nameInput;

    void Start()
    {
        // اعرض الاسم الحالي من AppSession داخل الحقل
        if (AppSession.Instance != null && nameInput != null)
            nameInput.text = AppSession.Instance.playerName;
    }

    public void OnNameChanged(string value)
    {
        if (AppSession.Instance != null)
            AppSession.Instance.SetPlayerName(value);
    }
}

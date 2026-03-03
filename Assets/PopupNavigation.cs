using UnityEngine;
using UnityEngine.SceneManagement;

public class PopupNavigation : MonoBehaviour
{
    public GameObject popup;   // نحط فيه ConfirmPopup
    public string sceneToLoad; // اسم الصفحة اللي نبي نروح لها

    // يفتح البوب اب
    public void ShowPopup()
    {
        popup.SetActive(true);
    }

    // يقفل البوب اب
    public void HidePopup()
    {
        popup.SetActive(false);
    }

    // يغير المشهد
    public void ConfirmAndGo()
    {
        SceneManager.LoadScene(sceneToLoad);
    }
}
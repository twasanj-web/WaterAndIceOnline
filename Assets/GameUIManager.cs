using UnityEngine;
using UnityEngine.UI;

public class GameUIManager : MonoBehaviour
{
    [Header("Buttons")]
    public Button freezeButton;
    public Button unfreezeButton;

    public Joystick joystick;

    private void Start()
    {
        if (freezeButton != null) freezeButton.gameObject.SetActive(false);
        if (unfreezeButton != null) unfreezeButton.gameObject.SetActive(false);

        var session = AppSession.Instance;
        if (session != null)
            SetupUIBasedOnRole(session.role);
        else
            SetupUIBasedOnRole(PlayerRole.Water);
    }

    private void SetupUIBasedOnRole(PlayerRole role)
    {
        switch (role)
        {
            case PlayerRole.Ice:
                if (freezeButton != null) freezeButton.gameObject.SetActive(true);
                break;
            case PlayerRole.Water:
                if (unfreezeButton != null) unfreezeButton.gameObject.SetActive(true);
                break;
        }
    }
}
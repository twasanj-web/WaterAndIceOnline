using UnityEngine;
using UnityEngine.UI;

public class GameUIManager : MonoBehaviour
{
    [Header("Buttons")]
    public Button freezeButton;
    public Button unfreezeButton;

    public Joystick joystick;

    [Header("Audio (Local Only)")]
    public AudioSource audioSource;
    public AudioClip freezeSound;
    public AudioClip unfreezeSound;

    private void Start()
    {
        if (freezeButton != null)
        {
            freezeButton.gameObject.SetActive(false);
            // تشغيل الصوت فور الضغط على زر التجميد
            freezeButton.onClick.AddListener(PlayFreezeSoundLocal);
        }

        if (unfreezeButton != null)
        {
            unfreezeButton.gameObject.SetActive(false);
            // تشغيل الصوت فور الضغط على زر فك التجميد
            unfreezeButton.onClick.AddListener(PlayUnfreezeSoundLocal);
        }

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

    private void PlayFreezeSoundLocal()
    {
        if (audioSource != null && freezeSound != null)
            audioSource.PlayOneShot(freezeSound);
    }

    private void PlayUnfreezeSoundLocal()
    {
        if (audioSource != null && unfreezeSound != null)
            audioSource.PlayOneShot(unfreezeSound);
    }
}
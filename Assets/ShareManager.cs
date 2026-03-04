using UnityEngine;
using TMPro;

public class ShareManager : MonoBehaviour
{
    public TextMeshProUGUI LobbyCodeText;

    public void ShareGame()
    {
        string code = LobbyCodeText.text;

        if (string.IsNullOrEmpty(code))
        {
            Debug.Log("Lobby code is empty!");
            return;
        }

        string message = "🎮 ادخل العب معي في Water & Ice!\nكود الغرفة: " + code;

#if UNITY_ANDROID || UNITY_IOS

        new NativeShare()
            .SetText(message)
            .Share();

#elif UNITY_STANDALONE

        GUIUtility.systemCopyBuffer = message;
        Debug.Log("Copied to clipboard: " + message);

#endif
    }
}
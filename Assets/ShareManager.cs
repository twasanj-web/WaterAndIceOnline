using UnityEngine;
using TMPro;
using Sych.ShareAssets;

public class ShareManager : MonoBehaviour
{
    public TextMeshProUGUI LobbyCodeText;

    public void ShareGame()
    {
        string code = LobbyCodeText.text;

        if (string.IsNullOrEmpty(code))
            return;

        string message = "🎮 ادخل العب معي في Water & Ice!\nكود الغرفة: " + code;

#if UNITY_ANDROID || UNITY_IOS
        
        Share.ShareAsync(message);

#elif UNITY_STANDALONE
        
        GUIUtility.systemCopyBuffer = message;
        Debug.Log("Copied to clipboard");

#endif
    }
}
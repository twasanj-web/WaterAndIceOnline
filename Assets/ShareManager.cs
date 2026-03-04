using UnityEngine;
using TMPro;
using Sych.ShareAssets.Runtime; 

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
        // استخدمي ItemAsync لو تحبين تنتظرين النتيجة أو Item مع callback
        Share.Item(message, success => 
        {
            if (success) Debug.Log("تم فتح نافذة المشاركة بنجاح!");
            else Debug.LogWarning("حدث خطأ عند المشاركة.");
        });

#elif UNITY_STANDALONE
        GUIUtility.systemCopyBuffer = message;
        Debug.Log("Copied to clipboard");
#endif
    }
}
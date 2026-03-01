using UnityEngine;

public class EnsureAppSession : MonoBehaviour
{
    private void Awake()
    {
        // إذا فيه AppSession أصلاً (من DontDestroyOnLoad) خلاص
        if (AppSession.Instance != null) return;

        // إذا ما فيه، أنشئ واحد تلقائي
        Debug.Log("⚠️ AppSession not found -> creating one automatically");
        var go = new GameObject("AppSession (Auto)");
        go.AddComponent<AppSession>();
        // AppSession.Awake يسوي DontDestroyOnLoad
    }
}
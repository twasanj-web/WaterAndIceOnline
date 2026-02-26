using UnityEngine;

public class EnsureAppSession : MonoBehaviour
{
    public AppSession appSessionPrefab; // اسحبي prefab حق AppSession هنا

    void Awake()
    {
        if (AppSession.Instance == null && appSessionPrefab != null)
            Instantiate(appSessionPrefab);
    }
}
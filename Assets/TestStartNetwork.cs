using Unity.Netcode;
using UnityEngine;

public class TestStartNetwork : MonoBehaviour
{
    void Start()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("❌ No NetworkManager in scene!");
            return;
        }

        Debug.Log("✅ Starting Host...");
        bool ok = NetworkManager.Singleton.StartHost();
        Debug.Log("StartHost result: " + ok);
    }
}
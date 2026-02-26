using Unity.Netcode;
using UnityEngine;

public class AutoStartHost : MonoBehaviour
{
    void Start()
    {
        Debug.Log("Starting Host...");
        NetworkManager.Singleton.StartHost();
    }
}
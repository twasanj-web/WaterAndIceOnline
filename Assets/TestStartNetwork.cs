using Unity.Netcode;
using UnityEngine;

public class TestStartNetwork : MonoBehaviour
{
    void Start()
    {
        NetworkManager.Singleton.StartHost();
    }
}
using UnityEngine;

public class WaitingRoomStartButton : MonoBehaviour
{
    // اربطيها في زر السهم OnClick
    public void OnStartButtonClicked()
    {
        if (RelayNetworkManager.Instance == null)
        {
            Debug.LogError("❌ RelayNetworkManager.Instance is NULL (تأكدي انه موجود في DontDestroyOnLoad).");
            return;
        }

        RelayNetworkManager.Instance.StartGameAsHost();
    }
}
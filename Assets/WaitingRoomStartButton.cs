using UnityEngine;

public class WaitingRoomStartButton : MonoBehaviour
{
    public void OnStartButtonClicked()
    {
        Debug.Log("🎮 START button clicked");
        
        WaitingRoomStartGame waitingRoomStartGame = FindObjectOfType<WaitingRoomStartGame>();
        if (waitingRoomStartGame != null)
        {
            waitingRoomStartGame.OnArrowPressed();
        }
        else
        {
            Debug.LogError("❌ WaitingRoomStartGame not found in scene!");
        }
    }
}
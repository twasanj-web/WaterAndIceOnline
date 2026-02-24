using UnityEngine;
using TMPro;
using System.Collections;

public class WaitingRoomUI : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text statusText;        // (1/3)
    public TMP_Text[] nameSlots;       // 9 خانات: Name(1) ... Name(9)

    IEnumerator Start()
    {
        // انتظري فريم واحد عشان تتأكدين إن AppSession جاهز
        yield return null;

        var session = AppSession.Instance;
        if (session == null)
        {
            Debug.LogError("WaitingRoomUI: AppSession.Instance is NULL (هل فيه AppSession في MainMenu ويتعمل DontDestroyOnLoad؟)");
            yield break;
        }

        Debug.Log($"WaitingRoomUI: playerName='{session.playerName}', maxPlayers={session.maxPlayers}");

        // صفّر الخانات
        for (int i = 0; i < nameSlots.Length; i++)
            if (nameSlots[i] != null) nameSlots[i].text = "";

        // حط اسم الهوست في أول مكان
        if (nameSlots != null && nameSlots.Length > 0 && nameSlots[0] != null)
            nameSlots[0].text = string.IsNullOrWhiteSpace(session.playerName) ? "Player" : session.playerName;

        // حدّث الستاتس
        if (statusText != null)
            statusText.text = $"(1/{session.maxPlayers})";
    }
}
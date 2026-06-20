using System.Linq;
using UnityEngine;

public class PlayerStatusUI : MonoBehaviour
{
    [Header("Slots")]
    public PlayerStatusSlot[] slots;

    [Header("Sprites")]
    public Sprite waterSprite;
    public Sprite frozenWaterSprite;

    private void Start()
    {
        RefreshSlots();
        InvokeRepeating(nameof(RefreshSlots), 0.5f, 0.5f);
    }

    private void RefreshSlots()
    {
        var waterPlayers = FindObjectsOfType<NetworkPlayerInfo>()
            .Where(p =>
            {
                var visual = p.GetComponent<NetworkPlayerVisual>();
                return visual != null && visual.roleIndex.Value == 1;
            })
            .OrderBy(p => p.OwnerClientId)
            .ToList();

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null) continue;

            if (i >= waterPlayers.Count)
            {
                slots[i].Hide();
                continue;
            }

            var info = waterPlayers[i];
            var visual = info.GetComponent<NetworkPlayerVisual>();

            string playerName = info.playerName.Value.ToString();

            Sprite selectedSprite = visual.isFrozenVisual.Value
                ? frozenWaterSprite
                : waterSprite;

            slots[i].SetData(playerName, selectedSprite);
        }
    }
}
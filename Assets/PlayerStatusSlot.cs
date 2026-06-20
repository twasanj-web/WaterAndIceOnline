using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStatusSlot : MonoBehaviour
{
    public Image roleImage;
    public TMP_Text nameText;

    public void SetData(string playerName, Sprite sprite)
    {
        gameObject.SetActive(true);

        if (nameText != null)
            nameText.text = playerName;

        if (roleImage != null)
            roleImage.sprite = sprite;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
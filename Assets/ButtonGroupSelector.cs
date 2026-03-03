using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ButtonGroupSelector : MonoBehaviour
{
    public Button[] buttons;

    // لونك #B3E6B1
    private Color selectedColor = new Color32(179, 230, 177, 255);
    private Color normalColor = Color.white;

    private Button currentSelected;

    // نخزن الرقم المختار
    public int selectedValue;

    public void SelectButton(Button clickedButton)
    {
        // رجع الزر القديم للون الطبيعي
        if (currentSelected != null)
        {
            ColorBlock oldColors = currentSelected.colors;
            oldColors.normalColor = normalColor;
            oldColors.selectedColor = normalColor;
            currentSelected.colors = oldColors;
        }

        // غير لون الزر الجديد
        ColorBlock newColors = clickedButton.colors;
        newColors.normalColor = selectedColor;
        newColors.selectedColor = selectedColor;
        clickedButton.colors = newColors;

        currentSelected = clickedButton;

        // نخزن الرقم من النص داخل الزر
//selectedValue = int.Parse(clickedButton.GetComponentInChildren<TMP_Text>().text);
       // Debug.Log("Selected Value: " + selectedValue);
    }
}
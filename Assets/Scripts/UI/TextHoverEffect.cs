using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class TextHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public TextMeshProUGUI textMesh;
    public Color hoverColor = Color.yellow;
    private Color32 defaultColor;

    void Start()
    {
        if (textMesh == null)
        {
            textMesh = GetComponent<TextMeshProUGUI>();
        }
        defaultColor = textMesh.faceColor; // Store original color
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        textMesh.faceColor = hoverColor; // Change text color on hover
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        textMesh.faceColor = defaultColor; // Restore original color
    }
}

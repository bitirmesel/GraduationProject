using UnityEngine;
using UnityEngine.EventSystems;

public class ContentDrag : MonoBehaviour, IDragHandler
{
    private RectTransform rect;

    [Header("Dikey Sınırlar")]
    public float minY = -3000f; // En aşağı
    public float maxY = 0f;     // En yukarı

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Ekrandaki sürükleme miktarı (pixel cinsinden)
        float deltaY = eventData.delta.y;

        // Mevcut konuma ekleyelim
        float newY = rect.anchoredPosition.y + deltaY;

        // Limitler
        newY = Mathf.Clamp(newY, minY, maxY);

        rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, newY);

        // Debug istersen:
        // Debug.Log($"Dragging... deltaY={deltaY}, newY={newY}");
    }
}

using UnityEngine;
using UnityEngine.EventSystems;

public class SuperSimpleDrag : MonoBehaviour, IDragHandler
{
    private RectTransform rect;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        if (rect == null)
        {
            Debug.LogError("[SuperSimpleDrag] RectTransform bulunamadı!");
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (rect == null) return;

        // Ekrandaki sürükleme miktarı
        float deltaY = eventData.delta.y;

        // DIREKT pozisyona ekle
        rect.anchoredPosition += new Vector2(0f, deltaY);

        // Debug için:
        // Debug.Log($"Drag deltaY={deltaY}, newPos={rect.anchoredPosition}");
    }
}

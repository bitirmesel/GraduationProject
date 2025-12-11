using UnityEngine;
using UnityEngine.EventSystems;

namespace GraduationProject.Controllers
{
    // BUNU VIEWPORT ÜZERİNE KOYUYORUZ
    public class MapDragController : MonoBehaviour, IBeginDragHandler, IDragHandler
    {
        [Header("Referanslar")]
        [SerializeField] private RectTransform content;   // Uzun map
        [SerializeField] private RectTransform viewport;  // Görünen pencere (Viewport)

        private Vector2 startPointerPos;
        private Vector2 startContentPos;

        private float minY;
        private float maxY;

        private void Start()
        {
            if (viewport == null)
                viewport = GetComponent<RectTransform>();

            if (viewport == null || content == null)
            {
                Debug.LogError("[MapDragController] viewport veya content atanmadı!");
                enabled = false;
                return;
            }

            // ŞİMDİLİK OTOMATİK HESABI SİLİYORUZ
            // float viewportHeight = viewport.rect.height;
            // float contentHeight  = content.rect.height;
            // maxY = 0f;
            // minY = Mathf.Min(0f, viewportHeight - contentHeight);

            maxY = 0f;        // en yukarı
            minY = -3000f;    // en aşağı (test için, sonra ayarlarsın)

            Debug.Log($"[MapDragController] Başladı. minY={minY}, maxY={maxY}");
        }


        public void OnBeginDrag(PointerEventData eventData)
        {
            if (viewport == null || content == null) return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                viewport, eventData.position, eventData.pressEventCamera, out startPointerPos);

            startContentPos = content.anchoredPosition;

            Debug.Log($"[MapDragController] BeginDrag startContentPos={startContentPos}");
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (viewport == null || content == null) return;

            Vector2 currentPointerPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                viewport, eventData.position, eventData.pressEventCamera, out currentPointerPos);

            Vector2 delta = currentPointerPos - startPointerPos;

            // Parmağın nereye giderse map de o yöne gitsin
            float newY = startContentPos.y + delta.y;
            newY = Mathf.Clamp(newY, minY, maxY);

            content.anchoredPosition = new Vector2(startContentPos.x, newY);
        }
    }
}

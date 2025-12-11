using UnityEngine;
using UnityEngine.EventSystems;

public class MapScrollDrag : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler
{
    private RectTransform rect;

    [Header("Scroll Limitleri")]
    public float minY = -3000f;   // En aşağı gidebileceği nokta
    public float maxY = 0f;       // En yukarı gidebileceği nokta

    [Header("Inertia / Yaylanma Ayarları")]
    public float overshoot = 200f;     // Kenarların biraz dışına çıkmasına izin verilen mesafe
    public float deceleration = 5f;    // Sürtünme (ne kadar hızlı dursun)
    public float elasticity = 50f;     // Kenardan çekme kuvveti (yaylanma sertliği)

    private bool isDragging;
    private float velocityY;           // piksel/saniye

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
    }

    // Parmağı koyduğumuz an
    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
        velocityY = 0f; // Eski hızı sıfırla
    }

    // Parmağı sürüklerken
    public void OnDrag(PointerEventData eventData)
    {
        if (rect == null) return;

        // Parmağın gittiği yönde map de gitsin:
        float deltaY = eventData.delta.y;

        // Anlık pozisyonu güncelle
        Vector2 pos = rect.anchoredPosition;
        pos.y += deltaY;

        // Biraz sınır dışına çıkmasına izin veriyoruz (overshoot)
        pos.y = Mathf.Clamp(pos.y, minY - overshoot, maxY + overshoot);

        rect.anchoredPosition = pos;

        // Inertia için hız hesapla (piksel / saniye)
        float dt = Time.unscaledDeltaTime;
        if (dt > 0f)
            velocityY = deltaY / dt;
    }

    // Parmağı bıraktığımız an
    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
    }

    private void Update()
    {
        if (rect == null) return;
        if (isDragging) return;

        // Hız çok küçükse artık durdu say
        if (Mathf.Abs(velocityY) < 1f)
            return;

        float dt = Time.unscaledDeltaTime;

        // Inertia: hıza göre kaydır
        Vector2 pos = rect.anchoredPosition;
        pos.y += velocityY * dt;

        // Sürtünme: zamanla hızı sıfıra doğru çek
        velocityY = Mathf.Lerp(velocityY, 0f, deceleration * dt);

        // Kenarlarda yaylanma (elastic)
        if (pos.y > maxY)
        {
            float over = pos.y - maxY;                // ne kadar yukarı taşmış
            velocityY -= over * elasticity * dt;      // geri çek
        }
        else if (pos.y < minY)
        {
            float over = minY - pos.y;               // ne kadar aşağı taşmış
            velocityY += over * elasticity * dt;     // yukarı çek
        }

        rect.anchoredPosition = pos;
    }
}

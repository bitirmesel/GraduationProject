using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// Hece egzersizi kartı. MemoryCard'dan tamamen bağımsız.
/// Ekranda büyük yazıyla heceyi gösterir, tıklanınca callback tetikler.
/// Tüm UI elemanları runtime'da otomatik oluşturulur.
/// </summary>
public class SyllableCard : MonoBehaviour
{
    public string SyllableText { get; private set; }
    public string AudioUrl { get; private set; }
    public bool IsCompleted { get; private set; } = false;

    private TMP_Text _label;
    private Image _background;
    private Button _button;
    private Image _border; // Seçili vurgusu için

    private Action<SyllableCard> _onClickAction;

    // Renk Paleti
    private static readonly Color COLOR_DEFAULT = new Color(0.95f, 0.95f, 1f, 1f);    // Açık lavanta
    private static readonly Color COLOR_SELECTED = new Color(0.4f, 0.7f, 1f, 1f);     // Mavi vurgu
    private static readonly Color COLOR_COMPLETED = new Color(0.4f, 0.9f, 0.5f, 1f);  // Yeşil (başarılı)
    private static readonly Color COLOR_TEXT = new Color(0.15f, 0.15f, 0.2f, 1f);      // Koyu metin

    /// <summary>
    /// Kartı oluşturur ve UI elemanlarını runtime'da yaratır.
    /// </summary>
    public void Setup(string syllable, string audioUrl, Action<SyllableCard> onClick)
    {
        SyllableText = syllable;
        AudioUrl = audioUrl;
        _onClickAction = onClick;
        IsCompleted = false;

        BuildUI();
        SetVisualState(false);
    }

    private void BuildUI()
    {
        // --- 1. Arka Plan (Bu GameObject'un kendi Image'ı) ---
        _background = GetComponent<Image>();
        if (_background == null)
            _background = gameObject.AddComponent<Image>();

        _background.color = COLOR_DEFAULT;

        // Köşeleri yumuşatmak için sprite yoksa düz renk kullanılır
        // (Projenizde RoundedRect sprite varsa buraya atanabilir)

        // --- 2. Button Bileşeni ---
        _button = GetComponent<Button>();
        if (_button == null)
            _button = gameObject.AddComponent<Button>();

        _button.onClick.RemoveAllListeners();
        _button.onClick.AddListener(OnClicked);

        // Button transition: renk değişimi kapalı (biz kendimiz yönetiyoruz)
        _button.transition = Selectable.Transition.None;

        // --- 3. Yazı (TMP_Text) ---
        GameObject textObj = new GameObject("SyllableLabel");
        textObj.transform.SetParent(this.transform, false);

        RectTransform textRt = textObj.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(5, 5);
        textRt.offsetMax = new Vector2(-5, -5);

        _label = textObj.AddComponent<TextMeshProUGUI>();
        _label.text = SyllableText;
        _label.enableAutoSizing = true;
        _label.fontSizeMin = 24;
        _label.fontSizeMax = 120;
        _label.fontStyle = FontStyles.Bold;
        _label.alignment = TextAlignmentOptions.Center;
        _label.color = COLOR_TEXT;
    }

    private void OnClicked()
    {
        if (IsCompleted) return;
        _onClickAction?.Invoke(this);
    }

    /// <summary>
    /// Kartın seçili/normal görünümünü ayarlar.
    /// </summary>
    public void SetVisualState(bool selected)
    {
        if (IsCompleted) return;

        if (_background != null)
            _background.color = selected ? COLOR_SELECTED : COLOR_DEFAULT;

        if (_label != null)
            _label.color = selected ? Color.white : COLOR_TEXT;
    }

    /// <summary>
    /// Hece başarılı okunduğunda çağrılır. Yıldız/yeşil renk.
    /// </summary>
    public void MarkCompleted()
    {
        IsCompleted = true;
        if (_background != null)
            _background.color = COLOR_COMPLETED;
        if (_label != null)
            _label.color = Color.white;
    }
}

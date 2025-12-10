using UnityEngine;
using UnityEngine.UI;
using System;

public class MemoryCard : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image _cardImage; // Kartın üzerindeki Image bileşeni
    [SerializeField] private Button _btnComponent; // Tıklama için Button bileşeni

    [Header("Settings")]
    [SerializeField] private Sprite _cardBackSprite; // Kartın arkası (Kapalı hali)

    public int CardID { get; private set; } // Eşleşme kontrolü için kimlik
    public bool IsMatched { get; private set; } = false; // Eşleşti mi?

    private Sprite _cardFaceSprite; // Kartın ön yüzü (Açık hali)
    private bool _isFlipped = false; // Şu an açık mı?
    private Action<MemoryCard> _onClickAction; // Tıklanınca Manager'a haber verecek

    public void Setup(int id, Sprite faceSprite, Sprite backSprite, Action<MemoryCard> onClick)
    {
        CardID = id;
        _cardFaceSprite = faceSprite;
        _cardBackSprite = backSprite;
        _onClickAction = onClick;

        // Başlangıçta kartı kapat
        FlipBack(); 
        
        _btnComponent.onClick.RemoveAllListeners();
        _btnComponent.onClick.AddListener(OnCardClicked);
    }

    private void OnCardClicked()
    {
        // Eğer zaten açıksa, eşleşmişse veya oyun durmuşsa tıklama yapma
        if (_isFlipped || IsMatched) return;

        _onClickAction?.Invoke(this);
    }

    public void FlipOpen()
    {
        _isFlipped = true;
        _cardImage.sprite = _cardFaceSprite;
        // İstersen burada animasyon başlatabilirsin
    }

    public void FlipBack()
    {
        _isFlipped = false;
        _cardImage.sprite = _cardBackSprite;
    }

    public void SetMatched()
    {
        IsMatched = true;
        _btnComponent.interactable = false; // Artık tıklanamaz
        // Eşleşme efekti (Renk değişimi, yok olma vs.) buraya eklenebilir
        _cardImage.color = Color.gray; 
    }
}
using UnityEngine;
using UnityEngine.UI;
using System;

public class MemoryCard : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image _cardImage; 
    [SerializeField] private Button _btnComponent; 

    public int CardID { get; private set; }
    public bool IsRevealed { get; private set; } = false;
    public bool IsMatched { get; private set; } = false;

    private Sprite _faceSprite; // Ön yüz (Kedi)
    private Sprite _backSprite; // Arka yüz (Soru işareti)
    private Action<MemoryCard> _onClickAction;

    public void Setup(int id, Sprite face, Sprite back, Action<MemoryCard> onClick)
    {
        CardID = id;
        _faceSprite = face;
        _backSprite = back;
        _onClickAction = onClick;

        // Başlangıç: Arkası dönük ve tıklanabilir
        _cardImage.sprite = _backSprite;
        IsRevealed = false;
        IsMatched = false;
        _btnComponent.interactable = true;
        
        // Temizlik (Önceki tıklama olaylarını sil)
        _btnComponent.onClick.RemoveAllListeners();
        _btnComponent.onClick.AddListener(OnClicked);
    }

    private void OnClicked()
    {
        // Kilitliyse veya zaten açıksa tepki verme
        if (IsRevealed || IsMatched) return;

        _onClickAction?.Invoke(this);
    }

    // --- BASİT EYLEMLER ---

    public void FlipOpen()
    {
        IsRevealed = true;
        _cardImage.sprite = _faceSprite; // Direkt resmi değiştir
        _btnComponent.interactable = false;
    }

    public void FlipBack()
    {
        IsRevealed = false;
        _cardImage.sprite = _backSprite; // Direkt resmi değiştir
        _btnComponent.interactable = true;
    }

    public void SetMatched()
    {
        IsMatched = true;
        IsRevealed = true;
        _btnComponent.interactable = false;
        // İstersen burada kartı yarı şeffaf yapabilirsin:
        // _cardImage.color = new Color(1, 1, 1, 0.5f);
    }
}
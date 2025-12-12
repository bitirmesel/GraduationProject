using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using GraduationProject.Managers;
using GraduationProject.Models;

public class MemoryGameManager : BaseGameManager
{
    [Header("References")]
    [SerializeField] private Transform _gridContainer; 
    [SerializeField] private MemoryCard _cardPrefab;   

    [Header("Config")]
    [SerializeField] private long _fixedGameId = 4; 

    // Veriler
    private Sprite _cardBackSprite; 
    private List<Sprite> _faceSprites = new List<Sprite>(); 

    // Mantık
    private MemoryCard _firstCard;  
    private MemoryCard _secondCard; 
    private bool _inputLocked = false; // Tıklama kilidi
    private int _matchesFound = 0;  
    private int _totalPairs = 0;    

    // --- 1. CLOUD YÜKLEME (AYNEN KALIYOR) ---
    public override async Task InitializeGame(long letterId)
    {
        // Temizlik
        foreach (Transform child in _gridContainer) Destroy(child.gameObject);
        _matchesFound = 0;
        _faceSprites.Clear();
        _inputLocked = false; // Kilidi açmayı unutma

        Debug.Log($"[MemoryGameManager] Varlıklar indiriliyor... LetterID: {letterId}");

        var config = await APIManager.Instance.GetGameConfigAsync(_fixedGameId, letterId);
        
        if (config == null) { Debug.LogError("Config hatası!"); return; }

        foreach (var item in config.Items)
        {
            string fullUrl = config.BaseUrl + item.File;
            Sprite sprite = await AssetLoader.Instance.GetSpriteAsync(fullUrl, item.File);

            if (sprite != null)
            {
                if (item.Key == "background") _cardBackSprite = sprite;
                else { sprite.name = item.Key; _faceSprites.Add(sprite); }
            }
        }

        SetupGrid();
    }

    // --- 2. OYUN KURULUMU ---
    private void SetupGrid()
    {
        if (_faceSprites.Count == 0) return;

        // Çiftle
        List<Sprite> deck = new List<Sprite>();
        foreach (Sprite s in _faceSprites)
        {
            deck.Add(s);
            deck.Add(s);
        }
        _totalPairs = _faceSprites.Count;

        // Karıştır
        for (int i = 0; i < deck.Count; i++)
        {
            Sprite temp = deck[i];
            int rand = Random.Range(i, deck.Count);
            deck[i] = deck[rand];
            deck[rand] = temp;
        }

        // Diz
        foreach (Sprite s in deck)
        {
            MemoryCard card = Instantiate(_cardPrefab, _gridContainer);
            int cardId = s.name.GetHashCode();
            card.Setup(cardId, s, _cardBackSprite, OnCardSelected);
        }
    }

    // --- 3. OYUN MANTIĞI (STANDART) ---
    private void OnCardSelected(MemoryCard clickedCard)
    {
        if (_inputLocked) return;

        // Ses çal (SoundManager varsa)
        // SoundManager.Instance?.PlayClick();

        clickedCard.FlipOpen();

        if (_firstCard == null)
        {
            _firstCard = clickedCard;
        }
        else
        {
            _secondCard = clickedCard;
            _inputLocked = true; // Diğer tıklamaları engelle
            StartCoroutine(CheckMatch());
        }
    }

    private IEnumerator CheckMatch()
    {
        // Kartlar görünsün diye 1 saniye bekle
        yield return new WaitForSeconds(1.0f);

        if (_firstCard.CardID == _secondCard.CardID)
        {
            // Eşleşme!
            _matchesFound++;
            _firstCard.SetMatched();
            _secondCard.SetMatched();
            
            // SoundManager.Instance?.PlayMatch();

            if (_matchesFound >= _totalPairs)
            {
                Debug.Log("OYUN BİTTİ!");
                // SoundManager.Instance?.PlayVictory();
                
                // 2 saniye sonra sistemi bitir
                yield return new WaitForSeconds(2.0f);
                GameCompleted(); 
            }
        }
        else
        {
            // Hata!
            _firstCard.FlipBack();
            _secondCard.FlipBack();
            // SoundManager.Instance?.PlayMismatch();
        }

        // Sıfırla
        _firstCard = null;
        _secondCard = null;
        _inputLocked = false;
    }
}
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using GraduationProject.Managers;
using GraduationProject.Models;
using GraduationProject.Utilities;

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
    _inputLocked = false;

    long gameId = GameContext.SelectedGameId; // ✅ 4

    Debug.Log($"[MemoryGameManager] gameId={gameId} letterId={letterId} config çekiliyor...");

    var config = await APIManager.Instance.GetGameConfigAsync(gameId, letterId);
    if (config == null)
    {
        Debug.LogError("[MemoryGameManager] GameConfig gelmedi!");
        return;
    }

    // BACK + FACES
    _cardBackSprite = null;
    _faceSprites.Clear();

    foreach (var item in config.Items)
    {
        string fullUrl = config.BaseUrl + item.File;
        Sprite sprite = await AssetLoader.Instance.GetSpriteAsync(fullUrl, item.File);

        if (sprite != null)
        {
            if (item.Key == "background") _cardBackSprite = sprite;
            else _faceSprites.Add(sprite);
        }
    }

    if (_cardBackSprite == null || _faceSprites.Count == 0)
    {
        Debug.LogError("[MemoryGameManager] Sprite’lar eksik!");
        return;
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

    protected override async Task ApplyAssetSet(AssetSetDto assetSet)
{
    // Temizlik
    foreach (Transform child in _gridContainer) Destroy(child.gameObject);
    _matchesFound = 0;
    _faceSprites.Clear();
    _inputLocked = false;

    if (assetSet == null)
    {
        Debug.LogError("[MemoryGameManager] AssetSet null!");
        return;
    }

    // AssetLoader garanti
    if (AssetLoader.Instance == null)
    {
        Debug.LogError("[MemoryGameManager] AssetLoader.Instance yok! Scene'e AssetLoader objesi koy.");
        return;
    }

    // BACK
    _cardBackSprite = null;
    if (!string.IsNullOrEmpty(assetSet.cardBackUrl))
    {
        _cardBackSprite = await AssetLoader.Instance.GetSpriteAsync(assetSet.cardBackUrl, "card_back.png");
    }

    // FACES
    if (assetSet.items != null)
    {
        for (int i = 0; i < assetSet.items.Count; i++)
        {
            var url = assetSet.items[i].imageUrl;
            if (string.IsNullOrEmpty(url)) continue;

            var sp = await AssetLoader.Instance.GetSpriteAsync(url, $"face_{i}.png");
            if (sp != null)
            {
                sp.name = $"face_{i}";
                _faceSprites.Add(sp);
            }
        }
    }

    if (_cardBackSprite == null)
    {
        Debug.LogError("[MemoryGameManager] CardBack yüklenmedi!");
        return;
    }

    if (_faceSprites.Count == 0)
    {
        Debug.LogError("[MemoryGameManager] Face sprite yok!");
        return;
    }

    SetupGrid();
}

}
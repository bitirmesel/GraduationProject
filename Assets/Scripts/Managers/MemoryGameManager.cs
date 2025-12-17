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
    private bool _inputLocked = false; 
    private int _matchesFound = 0;  
    private int _totalPairs = 0;    

    // --- 1. CLOUD YÜKLEME ---
    public override async Task InitializeGame(long letterId)
    {
        // Temizlik
        foreach (Transform child in _gridContainer) Destroy(child.gameObject);
        _matchesFound = 0;
        _faceSprites.Clear();
        _inputLocked = false;

        // EMNİYET 1: Eğer Login'den gelmediysen Fixed ID kullan
        long gameId = GameContext.SelectedGameId > 0 ? GameContext.SelectedGameId : _fixedGameId;

        Debug.Log($"[MemoryGameManager] gameId={gameId} letterId={letterId} config çekiliyor...");

        var config = await APIManager.Instance.GetGameConfigAsync(gameId, letterId);
        
        // EMNİYET 2: Config gelmediyse dur
        if (config == null)
        {
            Debug.LogError("[MemoryGameManager] GameConfig gelmedi! (API 404 veya Bağlantı Hatası)");
            return;
        }

        // BACK + FACES
        _cardBackSprite = null;
        _faceSprites.Clear();

        if (config.Items != null)
        {
            for (int i = 0; i < config.Items.Count; i++)
            {
                var item = config.Items[i];
                string fullUrl = config.BaseUrl + item.File;
                
                Sprite sprite = await AssetLoader.Instance.GetSpriteAsync(fullUrl, item.File);

                if (sprite != null)
                {
                    // Sprite ismini garantiye al (Hash code hatası için)
                    sprite.name = $"Card_{item.Key}_{i}"; 

                    if (item.Key == "background") _cardBackSprite = sprite;
                    else _faceSprites.Add(sprite);
                }
            }
        }

        if (_cardBackSprite == null || _faceSprites.Count == 0)
        {
            Debug.LogError("[MemoryGameManager] Sprite’lar eksik! Grid oluşturulamıyor.");
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
            // Emniyet: Liste oluşturulurken sprite silinmiş mi?
            if (s != null) 
            {
                deck.Add(s);
                deck.Add(s);
            }
        }
        _totalPairs = deck.Count / 2; // Face count değil, deck count yarısı daha güvenli

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
            // EMNİYET 3: MissingReferenceException Engelleyici
            if (s == null) 
            {
                Debug.LogWarning("Bir sprite kayıp, kart atlanıyor.");
                continue;
            }

            MemoryCard card = Instantiate(_cardPrefab, _gridContainer);
            
            // İsme erişmeden önce null check yaptık, güvenli.
            int cardId = s.name.GetHashCode(); 
            
            card.Setup(cardId, s, _cardBackSprite, OnCardSelected);
        }
    }

    // --- 3. OYUN MANTIĞI ---
    private void OnCardSelected(MemoryCard clickedCard)
    {
        if (_inputLocked) return;
        if (clickedCard == _firstCard) return; // Kendine tıklamayı önle

        clickedCard.FlipOpen();

        if (_firstCard == null)
        {
            _firstCard = clickedCard;
        }
        else
        {
            _secondCard = clickedCard;
            _inputLocked = true; 
            StartCoroutine(CheckMatch());
        }
    }

    private IEnumerator CheckMatch()
    {
        yield return new WaitForSeconds(1.0f);

        // Kartlar yok olduysa (sahne değişimi vb) hata vermesin
        if (_firstCard == null || _secondCard == null) 
        {
             _inputLocked = false;
             yield break;
        }

        if (_firstCard.CardID == _secondCard.CardID)
        {
            // Eşleşme!
            _matchesFound++;
            _firstCard.SetMatched();
            _secondCard.SetMatched();

            if (_matchesFound >= _totalPairs)
            {
                Debug.Log("OYUN BİTTİ! Harika!");
                yield return new WaitForSeconds(2.0f);
                
                // BaseGameManager'daki bitiş fonksiyonu
                GameCompleted(); 
            }
        }
        else
        {
            // Hata
            _firstCard.FlipBack();
            _secondCard.FlipBack();
        }

        _firstCard = null;
        _secondCard = null;
        _inputLocked = false;
    }

    // --- 4. ASSET SET (ESKİ SİSTEM DESTEĞİ) ---
    protected override async Task ApplyAssetSet(AssetSetDto assetSet)
    {
        foreach (Transform child in _gridContainer) Destroy(child.gameObject);
        _matchesFound = 0;
        _faceSprites.Clear();
        _inputLocked = false;

        if (assetSet == null || AssetLoader.Instance == null) return;

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
                    sp.name = $"face_{i}"; // İsim atama önemli
                    _faceSprites.Add(sp);
                }
            }
        }

        if (_cardBackSprite != null && _faceSprites.Count > 0)
        {
            SetupGrid();
        }
    }
}
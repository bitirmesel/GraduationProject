using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks; // Async işlemler için
using UnityEngine;
using UnityEngine.UI;
using GraduationProject.Managers; // APIManager ve AssetLoader
using GraduationProject.Models;  // Modeller

// ARTIK 'MonoBehaviour' DEĞİL 'BaseGameManager'DAN MİRAS ALIYOR
public class MemoryGameManager : BaseGameManager
{
    [Header("References")]
    [SerializeField] private Transform _gridContainer; // Kartların dizileceği Grid
    [SerializeField] private MemoryCard _cardPrefab;   // Kart Prefab'ı

    [Header("Game Configuration")]
    // Bu prefab SADECE Hafıza Oyunu (ID:4) içindir.
    [SerializeField] private long _fixedGameId = 4; 

    // Dinamik Görseller
    private Sprite _cardBackSprite; // API'den "background" olarak gelecek
    private List<Sprite> _faceSprites = new List<Sprite>(); // Kart ön yüzleri

    // Oyun Mantığı Değişkenleri
    private MemoryCard _firstCard;  
    private MemoryCard _secondCard; 
    private bool _canClick = true;  
    private int _matchesFound = 0;  
    private int _totalPairs = 0;    

    // ----------------------------------------------------------------
    // 1. BAŞLATMA (GameLoader Tarafından Çağrılır)
    // ----------------------------------------------------------------
    public override async Task InitializeGame(long letterId)
    {
        Debug.Log($"[MemoryGameManager] Oyun Başlatılıyor... LetterID: {letterId}");

        // Temizlik (Eski kartlar varsa sil)
        foreach (Transform child in _gridContainer) Destroy(child.gameObject);

        // Buluttan verileri çek
        await LoadAssetsFromCloud(letterId);
    }

    private async Task LoadAssetsFromCloud(long letterId)
    {
        // A. Konfigürasyonu İste
        var config = await APIManager.Instance.GetGameConfigAsync(_fixedGameId, letterId);
        
        if (config == null) 
        {
            Debug.LogError("[MemoryGameManager] Config alınamadı! İnternet hatası.");
            return;
        }

        Debug.Log($"[MemoryGameManager] {config.Items.Count} adet varlık indirilecek.");

        // B. Resimleri İndir
        _faceSprites.Clear();
        _cardBackSprite = null;

        foreach (var item in config.Items)
        {
            string fullUrl = config.BaseUrl + item.File;
            
            // AssetLoader Singleton Çağrısı
            Sprite downloadedSprite = await AssetLoader.Instance.GetSpriteAsync(fullUrl, item.File);

            if (downloadedSprite != null)
            {
                // 'background' kontrolü
                if (item.Key == "background")
                {
                    _cardBackSprite = downloadedSprite;
                    _cardBackSprite.name = "CardBack";
                }
                else
                {
                    downloadedSprite.name = item.Key; 
                    _faceSprites.Add(downloadedSprite);
                }
            }
        }

        // C. Güvenlik Kontrolü
        if (_cardBackSprite == null)
            Debug.LogWarning("API'de 'background' görseli yok. Kart arkaları boş kalabilir.");

        // D. Oyunu Kur
        SetupGrid();
    }

    // ----------------------------------------------------------------
    // 2. SAHNE KURULUMU (Kartları Dizme)
    // ----------------------------------------------------------------
    private void SetupGrid()
    {
        if (_faceSprites.Count == 0)
        {
            Debug.LogError("HATA: Hiç kart görseli yüklenemedi!");
            return;
        }

        // Deste Oluşturma: Her resimden 2 tane
        List<Sprite> deck = new List<Sprite>();
        foreach (Sprite s in _faceSprites)
        {
            deck.Add(s);
            deck.Add(s); 
        }

        _totalPairs = _faceSprites.Count;
        _matchesFound = 0;

        // Karıştırma (Fisher-Yates)
        for (int i = 0; i < deck.Count; i++)
        {
            Sprite temp = deck[i];
            int randomIndex = Random.Range(i, deck.Count);
            deck[i] = deck[randomIndex];
            deck[randomIndex] = temp;
        }

        // Sahneye Yerleştirme
        foreach (Sprite sprite in deck)
        {
            MemoryCard cardObj = Instantiate(_cardPrefab, _gridContainer);
            int cardId = sprite.name.GetHashCode(); 
            
            cardObj.Setup(cardId, sprite, _cardBackSprite, OnCardSelected);
        }
    }

    // ----------------------------------------------------------------
    // 3. OYUN MANTIĞI
    // ----------------------------------------------------------------
    private void OnCardSelected(MemoryCard clickedCard)
    {
        if (!_canClick) return;

        clickedCard.FlipOpen();

        if (_firstCard == null)
        {
            _firstCard = clickedCard;
        }
        else
        {
            _secondCard = clickedCard;
            _canClick = false;
            StartCoroutine(CheckMatch());
        }
    }

    private IEnumerator CheckMatch()
    {
        yield return new WaitForSeconds(1.0f);

        if (_firstCard.CardID == _secondCard.CardID)
        {
            // Eşleşme
            _firstCard.SetMatched();
            _secondCard.SetMatched();
            _matchesFound++;
            CheckGameOver();
        }
        else
        {
            // Hata
            _firstCard.FlipBack();
            _secondCard.FlipBack();
        }

        _firstCard = null;
        _secondCard = null;
        _canClick = true;
    }

    private void CheckGameOver()
    {
        if (_matchesFound >= _totalPairs)
        {
            // Base Class'taki bitiş fonksiyonunu çağırıyoruz
            // Bu sayede GameLoader veya LevelManager oyunun bittiğini anlar
            GameCompleted(); 
        }
    }
}
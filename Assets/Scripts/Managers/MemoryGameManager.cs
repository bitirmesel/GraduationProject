using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MemoryGameManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform _gridContainer; // Kartların dizileceği Grid Layout Group
    [SerializeField] private MemoryCard _cardPrefab;   // Kart prefabı

    [Header("Test Data (Backend Yokken)")]
    [SerializeField] private Sprite _cardBackSprite;    // Test modunda kart arkası
    [SerializeField] private List<Sprite> _faceSprites; // Test için meyve/hayvan resimleri

    private MemoryCard _firstCard;   // Açılan ilk kart
    private MemoryCard _secondCard;  // Açılan ikinci kart

    private bool _canClick = true;   // Oyuncu tıklayabilir mi?
    private int _matchesFound = 0;   // Bulunan eşleşme sayısı
    private int _totalPairs = 0;     // Toplam çift sayısı

    // Dışarıdan veri geldi mi? (GameSceneController vs.)
    private bool _startedWithExternalAssets = false;

    private void Start()
    {
        // Eğer dışarıdan StartGameWithAssets çağrılmadıysa
        // ve test datası doluysa, test modunda oyunu başlat.
        if (!_startedWithExternalAssets)
        {
            if (_faceSprites != null && _faceSprites.Count > 0 && _cardBackSprite != null)
            {
                Debug.Log("[MemoryGameManager] Test modunda başlatılıyor (_faceSprites kullanılıyor).");
                BuildDeckAndSpawn();
            }
            else
            {
                Debug.LogWarning("[MemoryGameManager] Başlatmak için dışarıdan asset bekleniyor veya test datası boş.");
            }
        }
    }

    /// <summary>
    /// GameSceneController gibi başka bir script, backend’den/AssetLoader’dan yüklediği
    /// sprite’ları buraya verir. Böylece runtime verisiyle oyun başlar.
    /// </summary>
    public void StartGameWithAssets(List<Sprite> faceSprites, Sprite backSprite)
    {
        if (faceSprites == null || faceSprites.Count == 0)
        {
            Debug.LogError("[MemoryGameManager] StartGameWithAssets: faceSprites boş!");
            return;
        }

        _startedWithExternalAssets = true;
        _faceSprites = faceSprites;
        if (backSprite != null)
            _cardBackSprite = backSprite;

        Debug.Log($"[MemoryGameManager] {faceSprites.Count} adet kart yüzü ile oyun başlatılıyor.");
        BuildDeckAndSpawn();
    }

    /// <summary>
    /// Eski StartGame gövdesi buraya taşındı.
    /// Hem test modunda hem de dışarıdan gelen asset’lerle aynı mantığı kullanıyoruz.
    /// </summary>
    private void BuildDeckAndSpawn()
    {
        // 1. Önceki oyundan kalan kartları temizle
        foreach (Transform child in _gridContainer)
        {
            Destroy(child.gameObject);
        }

        if (_faceSprites == null || _faceSprites.Count == 0)
        {
            Debug.LogError("[MemoryGameManager] Kart yüzü yok, oyun başlatılamaz.");
            return;
        }

        // 2. Kart çiftlerini oluştur (Örn: 4 resim varsa 8 kart olur)
        List<Sprite> deck = new List<Sprite>();

        // Her resimden 2 tane ekle
        foreach (Sprite s in _faceSprites)
        {
            deck.Add(s);
            deck.Add(s);
        }

        _totalPairs = _faceSprites.Count;
        _matchesFound = 0;

        // 3. Desteyi Karıştır (Fisher-Yates Shuffle)
        for (int i = 0; i < deck.Count; i++)
        {
            Sprite temp = deck[i];
            int randomIndex = Random.Range(i, deck.Count);
            deck[i] = deck[randomIndex];
            deck[randomIndex] = temp;
        }

        // 4. Kartları Sahneye Koy
        foreach (Sprite sprite in deck)
        {
            MemoryCard cardObj = Instantiate(_cardPrefab, _gridContainer);

            // Kartın ID'si olarak Sprite'ın adını veya hash kodunu kullanabiliriz
            // Aynı resme sahip kartlar aynı ID'ye sahip olur.
            int cardId = sprite.name.GetHashCode();

            cardObj.Setup(cardId, sprite, _cardBackSprite, OnCardSelected);
        }

        _canClick = true;
    }

    // Eğer istersen dışarıdan da çağırabil (UI butonu vs.)
    public void RestartGame()
    {
        BuildDeckAndSpawn();
    }

    private void OnCardSelected(MemoryCard clickedCard)
    {
        if (!_canClick) return;
        if (clickedCard == null) return;

        // Kartı aç
        clickedCard.FlipOpen();

        // İlk kart mı?
        if (_firstCard == null)
        {
            _firstCard = clickedCard;
        }
        else
        {
            // İkinci kart seçildi
            _secondCard = clickedCard;
            _canClick = false; // Kontrol bitene kadar tıklamayı engelle

            StartCoroutine(CheckMatch());
        }
    }

    private IEnumerator CheckMatch()
    {
        // Kartlar görünsün diye az bekle
        yield return new WaitForSeconds(1.0f);

        if (_firstCard != null && _secondCard != null &&
            _firstCard.CardID == _secondCard.CardID)
        {
            // EŞLEŞME OLDU!
            Debug.Log("Eşleşme Başarılı!");
            _firstCard.SetMatched();
            _secondCard.SetMatched();

            _matchesFound++;
            CheckGameOver();
        }
        else
        {
            // EŞLEŞME OLMADI, KAPAT
            Debug.Log("Eşleşmedi...");
            if (_firstCard != null) _firstCard.FlipBack();
            if (_secondCard != null) _secondCard.FlipBack();
        }

        // Seçimleri sıfırla
        _firstCard = null;
        _secondCard = null;
        _canClick = true;
    }

    private void CheckGameOver()
    {
        if (_matchesFound >= _totalPairs)
        {
            Debug.Log("OYUN BİTTİ! TEBRİKLER! 🎉");
            // Buraya "Level Completed" paneli açma kodu gelecek
        }
    }
}

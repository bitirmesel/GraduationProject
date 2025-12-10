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
    [SerializeField] private Sprite _cardBackSprite;   // Tüm kartların arkası
    [SerializeField] private List<Sprite> _faceSprites; // Test için meyve/hayvan resimleri

    private MemoryCard _firstCard;  // Açılan ilk kart
    private MemoryCard _secondCard; // Açılan ikinci kart
    
    private bool _canClick = true;  // Oyuncu tıklayabilir mi?
    private int _matchesFound = 0;  // Bulunan eşleşme sayısı
    private int _totalPairs = 0;    // Toplam çift sayısı

    private void Start()
    {
        StartGame();
    }

    public void StartGame()
    {
        // 1. Önceki oyundan kalan kartları temizle
        foreach (Transform child in _gridContainer)
        {
            Destroy(child.gameObject);
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
    }

    private void OnCardSelected(MemoryCard clickedCard)
    {
        if (!_canClick) return;

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

        if (_firstCard.CardID == _secondCard.CardID)
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
            _firstCard.FlipBack();
            _secondCard.FlipBack();
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
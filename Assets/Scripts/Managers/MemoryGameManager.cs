using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using GraduationProject.Managers; // PronunciationManager burada
using GraduationProject.Models;
using GraduationProject.Utilities;

public class MemoryGameManager : BaseGameManager
{
    public static MemoryGameManager Instance; // Singleton erişimi

    [Header("References")]
    [SerializeField] private Transform _gridContainer;
    [SerializeField] private MemoryCard _cardPrefab;

    [Header("Config")]
    [SerializeField] private long _fixedGameId = 4;

    // --- YENİ EKLENEN KISIM: KELİME DATASI ---
    // Sadece resimleri değil, kelime metinlerini de burada saklayacağız.
    private List<AssetItem> _levelAssetData = new List<AssetItem>(); 
    
    // Veriler
    private Sprite _cardBackSprite;
    private List<Sprite> _faceSprites = new List<Sprite>();

    // Mantık
    private MemoryCard _firstCard;
    private MemoryCard _secondCard;
    private bool _inputLocked = false;
    private int _matchesFound = 0;
    private int _totalPairs = 0;

    private void Awake()
    {
        Instance = this;
    }

    // --- 1. CLOUD YÜKLEME ---
    public override async Task InitializeGame(long letterId)
    {
        // Temizlik
        foreach (Transform child in _gridContainer) Destroy(child.gameObject);
        _matchesFound = 0;
        _faceSprites.Clear();
        _levelAssetData.Clear(); // Listeyi temizle
        _inputLocked = false;

        long gameId = GameContext.SelectedGameId > 0 ? GameContext.SelectedGameId : _fixedGameId;
        Debug.Log($"[MemoryGameManager] gameId={gameId} letterId={letterId} config çekiliyor...");

        var config = await APIManager.Instance.GetGameConfigAsync(gameId, letterId);

        if (config == null)
        {
            Debug.LogError("[MemoryGameManager] GameConfig gelmedi!");
            return;
        }

        _cardBackSprite = null;
        
        // --- DATA YÜKLEME ---
        if (config.Items != null)
        {
            for (int i = 0; i < config.Items.Count; i++)
            {
                var item = config.Items[i];
                string fullUrl = config.BaseUrl + item.File;

                Sprite sprite = await AssetLoader.Instance.GetSpriteAsync(fullUrl, item.File);

                if (sprite != null)
                {
                    if (item.Key == "background") 
                    {
                        _cardBackSprite = sprite;
                    }
                    else 
                    {
                        // 1. Resmi listeye ekle (Oyun için)
                        _faceSprites.Add(sprite);
                        
                        // 2. Data'yı listeye ekle (Telaffuz testi için "timsah", "kedi" vb.)
                        _levelAssetData.Add(item);
                    }
                }
            }
        }

        if (_cardBackSprite == null || _faceSprites.Count == 0) return;

        SetupGrid();
    }

    // --- 2. OYUN KURULUMU (DEĞİŞMEDİ) ---
    private void SetupGrid()
    {
        if (_faceSprites.Count == 0) return;

        List<Sprite> deck = new List<Sprite>();
        foreach (Sprite s in _faceSprites)
        {
            if (s != null) { deck.Add(s); deck.Add(s); }
        }
        _totalPairs = deck.Count / 2;

        // Shuffle
        for (int i = 0; i < deck.Count; i++)
        {
            Sprite temp = deck[i];
            int rand = Random.Range(i, deck.Count);
            deck[i] = deck[rand];
            deck[rand] = temp;
        }

        foreach (Sprite s in deck)
        {
            if (s == null) continue;
            MemoryCard card = Instantiate(_cardPrefab, _gridContainer);
            int cardId = s.name.GetHashCode();
            card.Setup(cardId, s, _cardBackSprite, OnCardSelected);
        }
    }

    // --- 3. OYUN MANTIĞI (DEĞİŞMEDİ) ---
    private void OnCardSelected(MemoryCard clickedCard)
    {
        if (_inputLocked || clickedCard == _firstCard) return;

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

        if (_firstCard == null || _secondCard == null)
        {
            _inputLocked = false;
            yield break;
        }

        if (_firstCard.CardID == _secondCard.CardID)
        {
            _matchesFound++;
            _firstCard.SetMatched();
            _secondCard.SetMatched();

            if (_matchesFound >= _totalPairs)
            {
                Debug.Log("HAFIZA OYUNU BİTTİ! Telaffuz aşamasına geçiliyor...");
                yield return new WaitForSeconds(2.0f);
                OnGameComplete();
            }
        }
        else
        {
            _firstCard.FlipBack();
            _secondCard.FlipBack();
        }

        _firstCard = null;
        _secondCard = null;
        _inputLocked = false;
    }

    // --- 4. OYUN BİTİŞİ VE TELAFFUZ BAŞLANGICI ---
    public async void OnGameComplete()
    {
        // Ses efektleri vs. (Senin kodların)
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayEffect("CongratsEffect");

        // --- ENTEGRASYON NOKTASI ---
        // Oyun bittiğinde Telaffuz paneline geçiyoruz.
        // Ama önce bu bölümdeki kelimeleri (timsah, kedi vs) panele bildirmeliyiz.
        
        // Örnek: UIPanelManager'da bir fonksiyonun varsa veriyi oraya at.
        // UIPanelManager.Instance.SetupPronunciationWords(_levelAssetData);
        
        if (UIPanelManager.Instance != null)
            UIPanelManager.Instance.ShowPronunciationPanel(true);
        else
            Debug.LogWarning("UIPanelManager yok! Test için otomatik başlatıyorum...");

        // TEST İÇİN: Eğer UI hazır değilse ilk kelimeyi ("timsah") test etmek için log atalım
        if (_levelAssetData.Count > 0)
        {
            Debug.Log($"[Test] Telaffuz sırası: {_levelAssetData[0].Key} ({_levelAssetData[0].File})");
        }
    }

    // --- 5. TELAFFUZ KONTROLÜ (UI BUTONLARININ ÇAĞIRACAĞI YER) ---
    
    // UI'daki "Kaydı Bitir" butonu burayı çağıracak.
    // 'assetIndex': Hangi kelimeyi okuduğunu UI bilecek (0: Timsah, 1: Kedi...)
    public void SubmitPronunciation(int assetIndex)
    {
        if (assetIndex < 0 || assetIndex >= _levelAssetData.Count) return;

        // JSON'dan gelen "Key" (örn: "timsah") bizim API Referans Metnimizdir!
        string targetWord = _levelAssetData[assetIndex].Key;

        Debug.Log($"'{targetWord}' kelimesi için ses gönderiliyor...");

        // PRONUNCIATION MANAGER'I ÇAĞIRIYORUZ
        // Sonuç gelince (callback) ne yapacağımızı da süslü parantez içine yazıyoruz.
        PronunciationManager.Instance.StopRecording(targetWord, (jsonResult) => 
        {
            // Backend'den cevap geldi!
            if (!string.IsNullOrEmpty(jsonResult))
            {
                // Burada JSON parse edip skora bakacaksın
                Debug.Log($"Backend Cevabı ({targetWord}): {jsonResult}");
                
                // Örneğin basit bir kontrol:
                // bool isSuccess = ParseJsonAndCheckScore(jsonResult);
                // if (isSuccess) ShowNextWord();
            }
            else
            {
                Debug.Log("Ses analizi başarısız oldu.");
            }
        });
    }

    // --- Eski Kod Desteği ---
    protected override async Task ApplyAssetSet(AssetSetDto assetSet) { /* ... Eski kodların ... */ }
    public void HandlePronunciationResult(string jsonResult) { /* ... Eski kodların ... */ }
}
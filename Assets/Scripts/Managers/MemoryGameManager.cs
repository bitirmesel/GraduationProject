using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using GraduationProject.Managers;
using GraduationProject.Models;
using GraduationProject.Utilities;

public class MemoryGameManager : BaseGameManager
{
    public static MemoryGameManager Instance;

    [Header("References")]
    [SerializeField] private Transform _gridContainer;
    [SerializeField] private MemoryCard _cardPrefab;

    [Header("Config")]
    [SerializeField] private long _fixedGameId = 4;

    private List<AssetItem> _levelAssetData = new List<AssetItem>();
<<<<<<< Updated upstream
    private string _imageBaseUrl; // Görseller için temel URL (base_url)
    private string _audioBaseUrl; // Ses dosyaları için temel URL (audio_base_url)

    // Veriler
=======
>>>>>>> Stashed changes
    private Sprite _cardBackSprite;
    private List<Sprite> _faceSprites = new List<Sprite>();

    private MemoryCard _firstCard;
    private MemoryCard _secondCard;
    private bool _inputLocked = false;
    private int _matchesFound = 0;
    private int _totalPairs = 0;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public override async Task InitializeGame(long letterId)
    {
        // 1. TEMİZLİK
        foreach (Transform child in _gridContainer) Destroy(child.gameObject);
        _matchesFound = 0;
        _faceSprites.Clear();
        _levelAssetData.Clear();
        _inputLocked = false;
        _cardBackSprite = null;

        long gameId = GameContext.SelectedGameId > 0 ? GameContext.SelectedGameId : _fixedGameId;
        Debug.Log($"🚀 [MemoryGameManager] Oyun Başlatılıyor... ID: {gameId}");

        // 2. API İSTEĞİ
        var config = await APIManager.Instance.GetGameConfigAsync(gameId, letterId);

        if (config == null)
        {
            Debug.LogError("❌ API Cevap Vermedi veya Config Null!");
            return;
        }

<<<<<<< Updated upstream
        _cardBackSprite = null;
        _imageBaseUrl = config.BaseUrl;      // Görsel URL'leri için temel yol
        _audioBaseUrl = config.AudioBaseUrl; // Ses URL'ini sakla

        // --- DATA YÜKLEME ---
        if (config.Items != null)
        {
            foreach (var item in config.Items)
            {
                string fullUrl = _imageBaseUrl + item.File;
                Sprite sprite = await AssetLoader.Instance.GetSpriteAsync(fullUrl, item.File);
=======
        // MODELDEKİ 'Items' LİSTESİNİ KULLANIYORUZ
        if (config.Items == null || config.Items.Count == 0)
        {
            Debug.LogError("❌ API'den gelen 'Items' listesi BOŞ!");
            return;
        }
>>>>>>> Stashed changes

        Debug.Log($"📦 Veri Alındı. Öge Sayısı: {config.Items.Count}");

        foreach (var item in config.Items)
        {
            // URL OLUŞTURMA (BaseUrl varsa ekle)
            string fullUrl = item.File;
            
            // Modeldeki 'BaseUrl' alanını kullanıyoruz
            if (!string.IsNullOrEmpty(config.BaseUrl) && !fullUrl.StartsWith("http"))
            {
                fullUrl = config.BaseUrl + item.File;
            }

            Debug.Log($"📥 İndiriliyor: {fullUrl}");

            Sprite sprite = await AssetLoader.Instance.GetSpriteAsync(fullUrl, item.File);

            if (sprite != null)
            {
                string cleanKey = string.IsNullOrEmpty(item.Key) ? "" : item.Key.Trim().ToLower();

                // Background kontrolü
                if (cleanKey.Contains("background"))
                {
<<<<<<< Updated upstream
                    if (item.Key == "background")
                    {
                        _cardBackSprite = sprite;
                    }
                    else
                    {
                        _faceSprites.Add(sprite);
                        // Telaffuz aşamasında kullanılacak datayı sakla
                        _levelAssetData.Add(item);

                        // Ses dosyasını önceden cache'e al (pronunciation anında hazır olsun)
                        if (!string.IsNullOrEmpty(item.Audio) && !string.IsNullOrEmpty(_audioBaseUrl))
                        {
                            string fullAudioUrl = _audioBaseUrl + item.Audio;
                            await AssetLoader.Instance.GetAudioAsync(fullAudioUrl, item.Audio);
                        }
                    }
=======
                    _cardBackSprite = sprite;
                    Debug.Log("✅ Background Ayarlandı.");
>>>>>>> Stashed changes
                }
                else
                {
                    _faceSprites.Add(sprite);
                    _levelAssetData.Add(item);
                    Debug.Log($"✅ Kart Eklendi: {item.Key}");
                }
            }
            else
            {
                Debug.LogError($"❌ İndirme Başarısız: {fullUrl}");
            }
        }

        // --- SONUÇ ---
        if (_faceSprites.Count < 2)
        {
            Debug.LogError($"❌ Yetersiz Kart Sayısı: {_faceSprites.Count}. Oyun başlatılamaz.");
            return;
        }

        SetupGrid();
    }

    private void SetupGrid()
    {
        List<Sprite> deck = new List<Sprite>();
        foreach (Sprite s in _faceSprites)
        {
            if (s != null) { deck.Add(s); deck.Add(s); }
        }
        _totalPairs = deck.Count / 2;

        // Karıştır
        for (int i = 0; i < deck.Count; i++)
        {
            Sprite temp = deck[i];
            int rand = Random.Range(i, deck.Count);
            deck[i] = deck[rand];
            deck[rand] = temp;
        }

        foreach (Sprite s in deck)
        {
            MemoryCard card = Instantiate(_cardPrefab, _gridContainer);
            int cardId = s.GetInstanceID();
            card.Setup(cardId, s, _cardBackSprite, OnCardSelected);
        }
    }

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
        yield return new WaitForSeconds(0.8f);

        if (_firstCard.CardID == _secondCard.CardID)
        {
            _matchesFound++;
            _firstCard.SetMatched();
            _secondCard.SetMatched();

            if (_matchesFound >= _totalPairs)
            {
                Debug.Log("🏆 OYUN BİTTİ!");
                yield return new WaitForSeconds(1.0f);
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

    public async void OnGameComplete()
    {
        if (UIPanelManager.Instance != null) UIPanelManager.Instance.ShowPronunciationPanel(true);
        PronunciationManager pm = PronunciationManager.Instance;
        
        if (pm == null)
        {
             var found = Resources.FindObjectsOfTypeAll<PronunciationManager>();
             if(found.Length > 0) { pm = found[0]; pm.gameObject.SetActive(true); PronunciationManager.Instance = pm; }
        }
        
        if (pm != null) pm.StartPronunciationSession(_levelAssetData);
    }

<<<<<<< Updated upstream
    if (pm != null)
    {
        pm.StartPronunciationSession(_levelAssetData, _imageBaseUrl, _audioBaseUrl);
    }
    else
    {
        Debug.LogError("KRİTİK HATA: PronunciationManager hiçbir yerde bulunamadı!");
    }
}
    // --- 5. TELAFFUZ KONTROLÜ (UI'dan çağrılır) ---
=======
>>>>>>> Stashed changes
    public void SubmitPronunciation(int assetIndex)
    {
        if (PronunciationManager.Instance != null) PronunciationManager.Instance.StopRecording();
    }

    protected override async Task ApplyAssetSet(AssetSetDto assetSet) { await Task.Yield(); }
}
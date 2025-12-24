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
    public static MemoryGameManager Instance;

    [Header("References")]
    [SerializeField] private Transform _gridContainer;
    [SerializeField] private MemoryCard _cardPrefab;

    [Header("Config")]
    [SerializeField] private long _fixedGameId = 4;

    // --- KELİME DATASI ---
    // API'den gelen orijinal item listesi (timsah, kedi vb.) burada saklanır.
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
        // Singleton kurulumu
        if (Instance == null) Instance = this;
    }

    // --- 1. CLOUD YÜKLEME ---
    public override async Task InitializeGame(long letterId)
    {
        // Temizlik işlemleri
        foreach (Transform child in _gridContainer) Destroy(child.gameObject);
        _matchesFound = 0;
        _faceSprites.Clear();
        _levelAssetData.Clear();
        _inputLocked = false;

        long gameId = GameContext.SelectedGameId > 0 ? GameContext.SelectedGameId : _fixedGameId;
        Debug.Log($"[MemoryGameManager] Config çekiliyor: gameId={gameId}, letterId={letterId}");

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
            foreach (var item in config.Items)
            {
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
                        _faceSprites.Add(sprite);
                        // Telaffuz aşamasında kullanılacak datayı sakla
                        _levelAssetData.Add(item);
                    }
                }
            }
        }

        if (_cardBackSprite == null || _faceSprites.Count == 0)
        {
            Debug.LogError("[MemoryGameManager] Gerekli görseller yüklenemedi!");
            return;
        }

        SetupGrid();
    }

    // --- 2. OYUN KURULUMU ---
    private void SetupGrid()
    {
        List<Sprite> deck = new List<Sprite>();
        foreach (Sprite s in _faceSprites)
        {
            if (s != null) { deck.Add(s); deck.Add(s); }
        }
        _totalPairs = deck.Count / 2;

        // Shuffle (Karıştırma)
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
            // Sprite ismini ID olarak kullanarak eşleşme kontrolü sağla
            int cardId = s.name.GetHashCode();
            card.Setup(cardId, s, _cardBackSprite, OnCardSelected);
        }
    }

    // --- 3. OYUN MANTIĞI ---
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
                Debug.Log("HAFIZA OYUNU BİTTİ! Telaffuz aşamasına geçiliyor...");
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

    // --- 4. OYUN BİTİŞİ VE TELAFFUZ BAŞLANGICI ---
    // --- 4. OYUN BİTİŞİ VE TELAFFUZ BAŞLANGICI ---
    public async void OnGameComplete()
    {
        Debug.Log("[Game] Oyun tamamlandı. Telaffuz süreci başlıyor...");

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayEffect("CongratsEffect");

        if (AudioManager.Instance != null)
        {
            await AudioManager.Instance.PlayVoiceOverAsync("Tebrik_Talimat");
        }

        if (UIPanelManager.Instance != null)
        {
            UIPanelManager.Instance.ShowPronunciationPanel(true);

            if (PronunciationManager.Instance != null)
            {
                // HATA DÜZELTME: _levelAssetData List<AssetItem> tipindedir.
                // PronunciationManager artık bu tipi bekleyecek şekilde güncellendi.
                PronunciationManager.Instance.StartPronunciationSession(_levelAssetData);
            }
        }
        else
        {
            Debug.LogError("UIPanelManager bulunamadı!");
        }
    }

    // --- 5. TELAFFUZ KONTROLÜ (UI'dan çağrılır) ---
    public void SubmitPronunciation(int assetIndex)
    {
        if (assetIndex < 0 || assetIndex >= _levelAssetData.Count) return;

        string targetWord = _levelAssetData[assetIndex].Key;
        Debug.Log($"'{targetWord}' için analiz başlatılıyor...");

        PronunciationManager.Instance.StopRecording(targetWord, (jsonResult) =>
        {
            if (!string.IsNullOrEmpty(jsonResult))
            {
                Debug.Log($"Backend Cevabı ({targetWord}): {jsonResult}");
                // Burada skora göre işlem yapılabilir
            }
            else
            {
                Debug.LogWarning("Ses analizi boş döndü.");
            }
        });
    }

    protected override async Task ApplyAssetSet(AssetSetDto assetSet) { await Task.Yield(); }
}
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GraduationProject.Managers;
using GraduationProject.Models;
using GraduationProject.Utilities;

/// <summary>
/// Hece egzersizi oyun yöneticisi. BaseGameManager'dan türer.
/// Hafıza oyunundan tamamen bağımsızdır.
/// 
/// Akış:
/// 1. Backend'den hece listesi çekilir (AssetSetDto, gameType=SYLLABLE)
/// 2. Ekrana Grid Layout ile hece kartları dizilir
/// 3. Çocuk bir karta tıklar → kart seçilir, ses çalar (varsa)
/// 4. İstediği kadar dinleyebilir (karta tekrar tıklar)
/// 5. Hazır olduğunda mikrofon butonuna basar → ses kaydedilir → backend'e gönderilir
/// 6. Puan ≥ 70 → kart yeşile döner (tamamlandı)
/// 7. Tüm kartlar yeşil → Egzersiz bitti
/// </summary>
public class SyllableExerciseManager : BaseGameManager
{
    // --- RUNTIME'DA OLUŞTURULACAK UI ELEMANLARI ---
    private Transform _gridContainer;
    private Button _micButton;
    private Button _listenButton;
    private TMP_Text _statusText;
    private AudioSource _audioSource;

    // --- OYUN VERİLERİ ---
    private List<SyllableCard> _allCards = new List<SyllableCard>();
    private SyllableCard _selectedCard;
    private int _completedCount = 0;
    private bool _isRecording = false;

    // --- SES KAYDI ---
    private AudioClip _recordingClip;
    private string _microphoneDevice;

    [Header("Backend")]
    [SerializeField] private string backendUrl = "https://backendapi-8nfn.onrender.com/api/pronunciation/check";

    // ========================================================
    // 1. BAŞLATMA
    // ========================================================

    public override async Task InitializeGame(long letterId)
    {
        Debug.Log($"[SyllableExercise] Başlatılıyor... letterId={letterId}");

        // Mikrofon hazırlığı
        if (Microphone.devices.Length > 0)
            _microphoneDevice = Microphone.devices[0];

#if UNITY_ANDROID
        if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Microphone))
            UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Microphone);
#endif

        // UI oluştur
        BuildUI();

        // Backend'den veri çek
        string gameType = "Hece";
        int difficulty = GameContext.SelectedDifficulty > 0 ? GameContext.SelectedDifficulty : 1;

        var assetSet = await APIManager.Instance.GetAssetSetAsync(letterId, gameType, difficulty);
        if (assetSet != null)
        {
            await ApplyAssetSet(assetSet);
        }
        else
        {
            Debug.LogError("[SyllableExercise] AssetSet backend'den gelmedi!");
            UpdateStatus("Veri yüklenemedi. Lütfen tekrar deneyin.");
        }
    }

    protected override async Task ApplyAssetSet(AssetSetDto assetSet)
    {
        if (assetSet.items == null || assetSet.items.Count == 0)
        {
            Debug.LogError("[SyllableExercise] Hece verisi boş!");
            UpdateStatus("Hece verisi bulunamadı.");
            return;
        }

        Debug.Log($"[SyllableExercise] {assetSet.items.Count} hece yükleniyor...");

        // Kartları oluştur
        foreach (var item in assetSet.items)
        {
            string syllable = item.syllableText;
            if (string.IsNullOrEmpty(syllable)) continue;

            // Kart objesi oluştur
            GameObject cardObj = new GameObject($"Card_{syllable}");
            cardObj.transform.SetParent(_gridContainer, false);

            // RectTransform ayarla (GridLayout yönetecek boyutu)
            RectTransform rt = cardObj.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(150, 150);

            // SyllableCard bileşeni ekle ve başlat
            SyllableCard card = cardObj.AddComponent<SyllableCard>();
            card.Setup(syllable, item.audioUrl ?? "", OnCardClicked);

            _allCards.Add(card);
        }

        // Ses dosyalarını ön-yüklemeye al (varsa)
        foreach (var item in assetSet.items)
        {
            if (!string.IsNullOrEmpty(item.audioUrl))
            {
                string fileName = System.IO.Path.GetFileName(item.audioUrl);
                await AssetLoader.Instance.GetAudioAsync(item.audioUrl, fileName);
            }
        }

        _completedCount = 0;
        UpdateStatus("Bir heceye dokunarak başla!");

        await Task.CompletedTask;
    }

    // ========================================================
    // 2. KART ETKİLEŞİMİ
    // ========================================================

    private void OnCardClicked(SyllableCard card)
    {
        if (_isRecording) return; // Kayıt sırasında kart değiştirilmesin

        // Önceki seçimi temizle
        if (_selectedCard != null && _selectedCard != card)
            _selectedCard.SetVisualState(false);

        // Yeni kartı seç
        _selectedCard = card;
        _selectedCard.SetVisualState(true);

        UpdateStatus($"Seçili hece: {card.SyllableText}");

        // Mikrofon butonunu aktif et
        if (_micButton != null) _micButton.interactable = true;
        if (_listenButton != null) _listenButton.interactable = true;

        // Otomatik olarak sesi çal (varsa)
        PlaySelectedSyllableAudio();
    }

    private async void PlaySelectedSyllableAudio()
    {
        if (_selectedCard == null) return;

        // 1. Cloudinary URL varsa oradan çek (AssetLoader önbellek kullanır)
        if (!string.IsNullOrEmpty(_selectedCard.AudioUrl))
        {
            string fileName = System.IO.Path.GetFileName(_selectedCard.AudioUrl);
            AudioClip clip = await AssetLoader.Instance.GetAudioAsync(_selectedCard.AudioUrl, fileName);
            if (clip != null)
            {
                _audioSource.clip = clip;
                _audioSource.Play();
                return;
            }
        }

        // 2. Cloudinary yoksa Resources'tan dene (yedek)
        AudioClip localClip = Resources.Load<AudioClip>("Audio/" + _selectedCard.SyllableText);
        if (localClip != null)
        {
            _audioSource.clip = localClip;
            _audioSource.Play();
            return;
        }

        Debug.Log($"[SyllableExercise] '{_selectedCard.SyllableText}' için ses dosyası bulunamadı.");
    }

    // ========================================================
    // 3. SES KAYDI VE PUANLAMA
    // ========================================================

    private void OnMicButtonPressed()
    {
        if (_selectedCard == null)
        {
            UpdateStatus("Önce bir hece seç!");
            return;
        }

        if (!_isRecording)
        {
            StartRecording();
        }
        else
        {
            StopRecordingAndAnalyze();
        }
    }

    private void OnListenButtonPressed()
    {
        if (_selectedCard == null)
        {
            UpdateStatus("Önce bir hece seç!");
            return;
        }
        PlaySelectedSyllableAudio();
    }

    private void StartRecording()
    {
        if (string.IsNullOrEmpty(_microphoneDevice))
        {
            if (Microphone.devices.Length > 0)
                _microphoneDevice = Microphone.devices[0];
            else
            {
                UpdateStatus("Mikrofon bulunamadı!");
                return;
            }
        }

        if (Microphone.IsRecording(_microphoneDevice))
            Microphone.End(_microphoneDevice);

        _recordingClip = Microphone.Start(_microphoneDevice, false, 10, 16000);
        _isRecording = true;

        UpdateStatus($"🎤 Kayıt yapılıyor... \"{_selectedCard.SyllableText}\" de!");

        // Mikrofon buton rengini değiştir (kayıt aktif)
        SetMicButtonRecording(true);
    }

    private void StopRecordingAndAnalyze()
    {
        if (!_isRecording) return;

        int samplePos = Microphone.GetPosition(_microphoneDevice);
        Microphone.End(_microphoneDevice);
        _isRecording = false;

        SetMicButtonRecording(false);

        if (samplePos <= 0)
        {
            UpdateStatus("Ses alınamadı! Tekrar dene.");
            return;
        }

        try
        {
            byte[] wavData = WavEncoder.FromAudioClip(_recordingClip, samplePos);
            UpdateStatus("Analiz ediliyor...");
            StartCoroutine(SendToBackendAndScore(wavData, _selectedCard.SyllableText));
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[SyllableExercise] WAV encode hatası: " + ex.Message);
            UpdateStatus("Bir hata oluştu. Tekrar dene!");
        }
    }

    private IEnumerator SendToBackendAndScore(byte[] wavData, string targetText)
    {
        var formData = new List<UnityEngine.Networking.IMultipartFormSection>();
        formData.Add(new UnityEngine.Networking.MultipartFormFileSection("audioFile", wavData, "recording.wav", "audio/wav"));
        formData.Add(new UnityEngine.Networking.MultipartFormDataSection("text", targetText));

        using (var www = UnityEngine.Networking.UnityWebRequest.Post(backendUrl, formData))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                ProcessScore(www.downloadHandler.text);
            }
            else
            {
                Debug.LogWarning($"[Backend Hata] {www.responseCode}: {www.error}");
                UpdateStatus("Sunucuya ulaşılamadı. Tekrar dene!");
            }
        }
    }

    private void ProcessScore(string jsonResponse)
    {
        try
        {
            var responseList = Newtonsoft.Json.JsonConvert.DeserializeObject<List<PronunciationResponseDto>>(jsonResponse);

            if (responseList == null || responseList.Count <= 1 ||
                responseList[1].OverallResult == null || responseList[1].OverallResult.Count == 0)
            {
                UpdateStatus("Puan alınamadı. Tekrar dene!");
                return;
            }

            double puan = responseList[1].OverallResult[0].overall_points;
            Debug.Log($"[SyllableExercise] Skor: {puan} | Hece: {_selectedCard.SyllableText}");

            if (puan >= 70)
            {
                // Başarılı!
                UpdateStatus($"Harika! Puanın: {puan:F0} ⭐");
                _selectedCard.MarkCompleted();
                _completedCount++;

                if (AudioManager.Instance != null)
                    AudioManager.Instance.PlayEffect("CorrectSound");

                // Tüm heceler tamamlandı mı?
                if (_completedCount >= _allCards.Count)
                {
                    UpdateStatus("Tebrikler! Tüm heceler tamamlandı! 🎉");
                    if (_micButton != null) _micButton.interactable = false;
                    if (_listenButton != null) _listenButton.interactable = false;

                    // Bitiş panelini göster
                    if (UIPanelManager.Instance != null)
                        UIPanelManager.Instance.ShowVictoryPanel(true);
                }
                else
                {
                    // Seçimi temizle, bir sonraki heceyi seçmesini bekle
                    _selectedCard.SetVisualState(false);
                    _selectedCard = null;
                    if (_micButton != null) _micButton.interactable = false;
                }
            }
            else
            {
                UpdateStatus($"Puanın: {puan:F0}. Tekrar dene!");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[SyllableExercise] JSON parse hatası: " + ex.Message);
            UpdateStatus("Bir hata oluştu. Tekrar dene!");
        }
    }

    // ========================================================
    // 4. UI OLUŞTURMA (RUNTIME)
    // ========================================================

    private void BuildUI()
    {
        // AudioSource ekle
        _audioSource = gameObject.GetComponent<AudioSource>();
        if (_audioSource == null)
            _audioSource = gameObject.AddComponent<AudioSource>();

        // --- Ana Layout ---
        // Bu objenin kendisi zaten Canvas içinde. Dikey düzen kuralım.
        VerticalLayoutGroup mainLayout = gameObject.AddComponent<VerticalLayoutGroup>();
        mainLayout.spacing = 15;
        mainLayout.padding = new RectOffset(20, 20, 20, 20);
        mainLayout.childAlignment = TextAnchor.UpperCenter;
        mainLayout.childControlWidth = true;
        mainLayout.childControlHeight = false;
        mainLayout.childForceExpandWidth = true;
        mainLayout.childForceExpandHeight = false;

        // RectTransform (Stretch)
        RectTransform selfRt = GetComponent<RectTransform>();
        if (selfRt != null)
        {
            selfRt.anchorMin = Vector2.zero;
            selfRt.anchorMax = Vector2.one;
            selfRt.offsetMin = Vector2.zero;
            selfRt.offsetMax = Vector2.zero;
        }

        // --- 1. Başlık ---
        GameObject titleObj = CreateTextElement("TitleText", "Hece Egzersizi", 36, FontStyles.Bold);
        LayoutElement titleLe = titleObj.AddComponent<LayoutElement>();
        titleLe.preferredHeight = 50;
        titleLe.flexibleWidth = 1;

        // --- 2. Durum Metni ---
        GameObject statusObj = CreateTextElement("StatusLabel", "Yükleniyor...", 24, FontStyles.Normal);
        _statusText = statusObj.GetComponent<TMP_Text>();
        LayoutElement statusLe = statusObj.AddComponent<LayoutElement>();
        statusLe.preferredHeight = 40;
        statusLe.flexibleWidth = 1;

        // --- 3. Kart Grid Alanı ---
        GameObject gridObj = new GameObject("SyllableGrid");
        gridObj.transform.SetParent(this.transform, false);

        RectTransform gridRt = gridObj.AddComponent<RectTransform>();
        LayoutElement gridLe = gridObj.AddComponent<LayoutElement>();
        gridLe.flexibleHeight = 1;
        gridLe.flexibleWidth = 1;

        GridLayoutGroup grid = gridObj.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(140, 100);
        grid.spacing = new Vector2(12, 12);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 4;
        grid.childAlignment = TextAnchor.MiddleCenter;
        grid.padding = new RectOffset(10, 10, 10, 10);

        _gridContainer = gridObj.transform;

        // --- 4. Alt Buton Barı ---
        GameObject buttonBar = new GameObject("ButtonBar");
        buttonBar.transform.SetParent(this.transform, false);
        buttonBar.AddComponent<RectTransform>();
        LayoutElement barLe = buttonBar.AddComponent<LayoutElement>();
        barLe.preferredHeight = 80;
        barLe.flexibleWidth = 1;

        HorizontalLayoutGroup barLayout = buttonBar.AddComponent<HorizontalLayoutGroup>();
        barLayout.spacing = 30;
        barLayout.childAlignment = TextAnchor.MiddleCenter;
        barLayout.childControlWidth = false;
        barLayout.childControlHeight = false;
        barLayout.childForceExpandWidth = false;
        barLayout.childForceExpandHeight = false;

        // Dinle Butonu
        _listenButton = CreateButton(buttonBar.transform, "ListenBtn", "🔊 Dinle",
            new Color(0.3f, 0.6f, 0.9f), OnListenButtonPressed);
        _listenButton.interactable = false;

        // Mikrofon Butonu
        _micButton = CreateButton(buttonBar.transform, "MicBtn", "🎤 Kaydet",
            new Color(0.9f, 0.35f, 0.35f), OnMicButtonPressed);
        _micButton.interactable = false;
    }

    private GameObject CreateTextElement(string name, string text, int fontSize, FontStyles style)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(this.transform, false);
        obj.AddComponent<RectTransform>();

        TMP_Text tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        return obj;
    }

    private Button CreateButton(Transform parent, string name, string label, Color bgColor, UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);

        RectTransform rt = btnObj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(180, 60);

        Image bg = btnObj.AddComponent<Image>();
        bg.color = bgColor;

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = bg;
        btn.onClick.AddListener(onClick);

        // Buton yazısı
        GameObject textObj = new GameObject("Label");
        textObj.transform.SetParent(btnObj.transform, false);

        RectTransform textRt = textObj.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        TMP_Text tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 22;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        return btn;
    }

    private void SetMicButtonRecording(bool recording)
    {
        if (_micButton == null) return;

        Image bg = _micButton.GetComponent<Image>();
        TMP_Text label = _micButton.GetComponentInChildren<TMP_Text>();

        if (recording)
        {
            if (bg != null) bg.color = new Color(1f, 0.2f, 0.2f); // Koyu kırmızı
            if (label != null) label.text = "⏹ Durdur";
        }
        else
        {
            if (bg != null) bg.color = new Color(0.9f, 0.35f, 0.35f); // Normal kırmızı
            if (label != null) label.text = "🎤 Kaydet";
        }
    }

    // ========================================================
    // 5. YARDIMCI
    // ========================================================

    private void UpdateStatus(string message)
    {
        if (_statusText != null)
            _statusText.text = message;
    }
}

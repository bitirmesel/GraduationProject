using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Threading.Tasks;
using GraduationProject.Managers;
using GraduationProject.Models;
using GraduationProject.Utilities;

public class LevelMapSceneController : MonoBehaviour
{


    [Header("Paneller")]
    // !!! İŞTE EKSİK OLAN KISIM BURASIYDI !!!
    public GameObject levelSelectionPanel; // Inspector'da buraya panelini sürüklemelisin!

    [Header("Tab Butonları")]
    public Button btnTabSyllable;
    public Button btnTabWord;
    public Button btnTabSentence;

    [Header("Zorluk Butonları")]
    public Button btnEasy;
    public Button btnMedium;
    public Button btnHard;

    private string _currentGameType = "Kelime";

    private void Start()
    {
        GameObject backBtnObj = GameObject.Find("BackButton");
        if (backBtnObj != null)
        {
            Button btn = backBtnObj.GetComponent<Button>();
            btn.onClick.RemoveAllListeners(); // Eski hatalı bağlantıları sil
            // DontDestroyOnLoad ile gelen navigator instance'ına bağla
            btn.onClick.AddListener(() => SceneNavigator.Instance.GoToSelection());
        }
        // 2. DEĞİŞİKLİK: Butonların gönderdiği isimleri Türkçe yapıyoruz
        // Backend'deki 'game_types' tablosunda isimler muhtemelen "Hece", "Kelime", "Cümle" olarak kayıtlı.

        if (btnTabSyllable != null)
            btnTabSyllable.onClick.AddListener(() => SelectTab("Hece")); // "Syllable" yerine "Hece"

        if (btnTabWord != null)
            btnTabWord.onClick.AddListener(() => SelectTab("Kelime")); // "Word" yerine "Kelime"

        if (btnTabSentence != null)
            btnTabSentence.onClick.AddListener(() => SelectTab("Cümle")); // "Sentence" yerine "Cümle"

        // Zorluk butonları (1, 2, 3) aynı kalabilir, onlar sayısal değer.
        if (btnEasy != null) btnEasy.onClick.AddListener(() => _ = OnLevelSelected(1));
        if (btnMedium != null) btnMedium.onClick.AddListener(() => _ = OnLevelSelected(2));
        if (btnHard != null) btnHard.onClick.AddListener(() => _ = OnLevelSelected(3));
    }

    // Tab (Hece, Kelime, Cümle) seçimi
    private void SelectTab(string gameType)
    {
        _currentGameType = gameType;
        Debug.Log($"[MAP] Tab seçildi: {_currentGameType}");
    }

    // Zorluk seviyesi seçildiğinde Backend'e istek atıp oyunu başlatan fonksiyon
    private async Task OnLevelSelected(int difficulty)
    {
        if (APIManager.Instance == null)
        {
            Debug.LogError("[MAP] APIManager.Instance yok!");
            return;
        }

        // Seçili harf ID'sini GameContext'ten alıyoruz (OnLetterClicked sayesinde güncellendi)
        long letterId = GameContext.SelectedLetterId;

        if (letterId <= 0)
        {
            Debug.LogError("[MAP] SelectedLetterId set edilmemiş! Lütfen önce bir harfe tıkladığınızdan emin olun.");
            return;
        }

        Debug.Log($"[MAP] İstek Gönderiliyor => Harf ID: {letterId}, Tip: {_currentGameType}, Zorluk: {difficulty}");

        // API İsteği
        AssetSetDto assetSet = await APIManager.Instance.GetAssetSetAsync(letterId, _currentGameType, difficulty);

        if (assetSet == null)
        {
            Debug.LogError("[MAP] AssetSet gelmedi (null). Bu harf ve zorluk seviyesi için backend verisi yok.");
            return;
        }

        // Gelen verileri GameContext'e kaydet
        GameContext.SelectedGameType = assetSet.gameType;
        GameContext.SelectedDifficulty = assetSet.difficulty;
        GameContext.SelectedAssetSetId = assetSet.assetSetId;
        GameContext.SelectedLetterId = assetSet.letterId;
        GameContext.SelectedLetterCode = assetSet.letterCode;
        GameContext.CardBackUrl = assetSet.cardBackUrl;

        GameContext.ImageUrls = new System.Collections.Generic.List<string>();
        GameContext.AudioUrls = new System.Collections.Generic.List<string>();

        if (assetSet.items != null)
        {
            foreach (var item in assetSet.items)
            {
                if (!string.IsNullOrEmpty(item.imageUrl)) GameContext.ImageUrls.Add(item.imageUrl);
                if (!string.IsNullOrEmpty(item.audioUrl)) GameContext.AudioUrls.Add(item.audioUrl);
            }
        }

        Debug.Log($"[MAP] Veri Alındı => Oyun Sahnesi Yükleniyor... (Görsel Sayısı: {GameContext.ImageUrls.Count})");

        SceneManager.LoadScene("GameScene");
    }

    // HARF BUTONLARINA ATANACAK FONKSİYON
    // Bu fonksiyon harfe tıklandığında hem ID'yi günceller hem de paneli açar.
    public void OnLetterClicked(int letterId, string letterCode)
    {
        // 1. GameContext'i güncelle (Backend'e doğru ID gitmesi için kritik nokta)
        GameContext.SelectedLetterId = letterId;
        GameContext.SelectedLetterCode = letterCode;

        Debug.Log($"[Selection] Harf Seçildi! ID: {letterId}, Kod: {letterCode}");

        // 2. Paneli Aç
        if (levelSelectionPanel != null)
        {
            levelSelectionPanel.SetActive(true);
        }
        else
        {
            Debug.LogError("HATA: 'levelSelectionPanel' Inspector'da atanmamış! Script'e paneli sürüklemeyi unuttunuz.");
        }
    }
}
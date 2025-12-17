using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Threading.Tasks;
using GraduationProject.Managers;
using GraduationProject.Models;
using GraduationProject.Utilities;

public class LevelMapSceneController : MonoBehaviour
{
    private string _currentGameType = "Syllable"; // Default

    [Header("Tab Butonları")]
    public Button btnTabSyllable;
    public Button btnTabWord;
    public Button btnTabSentence;

    [Header("Zorluk Butonları")]
    public Button btnEasy;
    public Button btnMedium;
    public Button btnHard;

    private void Start()
    {
        if (btnTabSyllable != null) btnTabSyllable.onClick.AddListener(() => SelectTab("Syllable"));
        if (btnTabWord != null) btnTabWord.onClick.AddListener(() => SelectTab("Word"));
        if (btnTabSentence != null) btnTabSentence.onClick.AddListener(() => SelectTab("Sentence"));

        if (btnEasy != null) btnEasy.onClick.AddListener(() => _ = OnLevelSelected(1));
        if (btnMedium != null) btnMedium.onClick.AddListener(() => _ = OnLevelSelected(2));
        if (btnHard != null) btnHard.onClick.AddListener(() => _ = OnLevelSelected(3));
    }

    private void SelectTab(string gameType)
    {
        _currentGameType = gameType;
        Debug.Log($"[MAP] Tab seçildi: {_currentGameType}");
    }

    private async Task OnLevelSelected(int difficulty)
    {
        if (APIManager.Instance == null)
        {
            Debug.LogError("[MAP] APIManager.Instance yok!");
            return;
        }

        long letterId = GameContext.SelectedLetterId;
        if (letterId <= 0)
        {
            Debug.LogError("[MAP] SelectedLetterId set edilmemiş! SelectionScene’den gelmiyor.");
            return;
        }

        Debug.Log($"[MAP] İstek => letterId={letterId}, type={_currentGameType}, diff={difficulty}");

        AssetSetDto assetSet = await APIManager.Instance.GetAssetSetAsync(letterId, _currentGameType, difficulty);
        if (assetSet == null)
        {
            Debug.LogError("[MAP] AssetSet gelmedi (null). Backend veya parametreler uyuşmuyor.");
            return;
        }

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

        Debug.Log($"[MAP] OK => letter={GameContext.SelectedLetterCode} type={GameContext.SelectedGameType} diff={GameContext.SelectedDifficulty} images={GameContext.ImageUrls.Count}");

        SceneManager.LoadScene(GameConstants.SCENE_GAME);
    }
}

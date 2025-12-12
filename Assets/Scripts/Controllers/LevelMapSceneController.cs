using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Threading.Tasks;
using GraduationProject.Managers;
using GraduationProject.Models;
using GraduationProject.Utilities;

public class LevelMapSceneController : MonoBehaviour
{
    private string _currentGameType = "Syllable"; // Default Hece

    public Button btnTabSyllable;
    public Button btnTabWord;
    public Button btnTabSentence;

    public Button btnEasy;
    public Button btnMedium;
    public Button btnHard;

    private void Start()
    {
        // Tab butonları
        btnTabSyllable.onClick.AddListener(() => _currentGameType = "Syllable");
        btnTabWord.onClick.AddListener(() => _currentGameType = "Word");
        btnTabSentence.onClick.AddListener(() => _currentGameType = "Sentence");

        // Zorluk butonları
        btnEasy.onClick.AddListener(() => { _ = OnLevelSelected(1); });
        btnMedium.onClick.AddListener(() => { _ = OnLevelSelected(2); });
        btnHard.onClick.AddListener(() => { _ = OnLevelSelected(3); });
    }

    private async Task OnLevelSelected(int difficulty)
    {
        if (APIManager.Instance == null)
        {
            Debug.LogError("APIManager yok!");
            return;
        }

        long letterId = GameContext.SelectedLetterId;
        if (letterId <= 0)
        {
            Debug.LogError("SelectedLetterId set edilmemiş!");
            return;
        }

        // 1) Backend’ten asset setini çek
        AssetSetDto assetSet = await APIManager.Instance
            .GetAssetSetAsync(letterId, _currentGameType, difficulty);

        if (assetSet == null)
        {
            Debug.LogError("Asset set alınamadı.");
            return;
        }

        // 2) GameContext’e doldur
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
                if (!string.IsNullOrEmpty(item.imageUrl))
                    GameContext.ImageUrls.Add(item.imageUrl);

                if (!string.IsNullOrEmpty(item.audioUrl))
                    GameContext.AudioUrls.Add(item.audioUrl);
            }
        }

        // 3) GameScene’e geç
        SceneManager.LoadScene(GameConstants.SCENE_GAME);
    }
}

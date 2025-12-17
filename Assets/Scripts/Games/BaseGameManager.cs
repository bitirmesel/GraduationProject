using UnityEngine;
using System.Threading.Tasks;
using GraduationProject.Utilities;
using GraduationProject.Managers;

public abstract class BaseGameManager : MonoBehaviour
{
    // Her oyunun kendi "Başlatma" komutu olacak
    // letterId: Hangi harfin içeriği yüklenecek?
    public abstract Task InitializeGame(long letterId);
    
    // Her oyunun bittiğinde çağıracağı ortak fonksiyon
    protected void GameCompleted()
    {
        Debug.Log("Oyun Tamamlandı! Base Manager sinyali aldı.");
        // İleride buraya "Level End Panel" açma kodu gelecek
    }

    public async Task InitializeFromContext()
    {
        long letterId = GameContext.SelectedLetterId;
        string gameType = GameContext.SelectedGameType;
        int difficulty = GameContext.SelectedDifficulty;

        Debug.Log($"[BaseGameManager] Init => LetterId={letterId}, GameType={gameType}, Diff={difficulty}");

        var assetSet = await APIManager.Instance.GetAssetSetAsync(letterId, gameType, difficulty);
        if (assetSet == null)
        {
            Debug.LogError("[BaseGameManager] AssetSet gelmedi!");
            return;
        }

        await ApplyAssetSet(assetSet);
    }

    protected abstract Task ApplyAssetSet(GraduationProject.Models.AssetSetDto assetSet);
}
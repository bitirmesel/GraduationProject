using UnityEngine;
using System.Collections.Generic;
using GraduationProject.Utilities; // GameContext için
using GraduationProject.Models;

public class GameLoader : MonoBehaviour
{
    [Header("Oyun Modülleri")]
    // Hangi GameID için hangi Prefab yüklenecek?
    public List<GameModuleDef> GameModules; 

    [Header("Spawn Point")]
    public Transform GameContainer; // Prefabın oluşacağı yer (Genelde Canvas'ın ortası)

    private async void Start()
    {
        // 1. GameContext'ten hangi oyunun seçildiğini öğren
        long gameIdToLoad = GameContext.SelectedGameType != null ? 
                            ParseGameType(GameContext.SelectedGameType) : 
                            4; // Varsayılan (Test için)
        
        long letterIdToLoad = GameContext.SelectedLetterId != 0 ? 
                              GameContext.SelectedLetterId : 
                              9; // Varsayılan (Test için)

        Debug.Log($"[GameLoader] Yükleniyor... GameID: {gameIdToLoad}, LetterID: {letterIdToLoad}");

        // 2. Doğru Prefab'ı Bul
        BaseGameManager selectedPrefab = null;
        
        foreach (var module in GameModules)
        {
            if (module.GameId == gameIdToLoad)
            {
                selectedPrefab = module.GamePrefab;
                break;
            }
        }

        if (selectedPrefab == null)
        {
            Debug.LogError($"[GameLoader] ID: {gameIdToLoad} için bir Prefab bulunamadı!");
            return;
        }

        // 3. Prefab'ı Yarat (Instantiate)
        BaseGameManager gameInstance = Instantiate(selectedPrefab, GameContainer);

        // 4. Oyunu Başlat
        // RectTransform ayarlarını sıfırla ki ekrana tam otursun
        RectTransform rt = gameInstance.GetComponent<RectTransform>();
        if(rt != null)
        {
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
        }

        await gameInstance.InitializeGame(letterIdToLoad);
    }

    // Backend'de "Memory", "Matching" gibi string tutuyorsan bunu ID'ye çevirmen gerekebilir
    // Veya direkt ID tutuyorsan buna gerek yok.
    private long ParseGameType(string type)
    {
        // Şimdilik basit bir switch, veritabanına göre değişir
        if (type == "Memory") return 4;
        if (type == "Matching") return 5;
        return 4; // Varsayılan
    }
}

[System.Serializable]
public struct GameModuleDef
{
    public string Name;       // Editörde kolay okumak için (Örn: "Hafıza Oyunu")
    public long GameId;       // Database ID'si (Örn: 4)
    public BaseGameManager GamePrefab; // Sürüklenecek Prefab
}
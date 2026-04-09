using UnityEngine;
using UnityEngine.UI;
using GraduationProject.Managers;
using System.Collections.Generic;

public class TestGame : MonoBehaviour
{
    public Image targetImage; 

    private async void Start()
    {
        long gameId = 4; 
        long letterId = 2; 

        Debug.Log($"Konfigürasyon çekiliyor... GameID: {gameId}, LetterID: {letterId}");
        
        var config = await APIManager.Instance.GetGameConfigAsync(gameId, letterId);

        if (config != null)
        {
            // DÜZELTME: config.Assets YERİNE config.Items
            int assetCount = (config.Items != null) ? config.Items.Count : 0;
            Debug.Log($"Config Başarıyla Geldi! Toplam Resim Sayısı: {assetCount}");

            if (assetCount > 0)
            {
                // DÜZELTME: config.Assets YERİNE config.Items
                var item = config.Items[0]; 

                string fullUrl = item.File; 
                
                Debug.Log($"İlk Resim İndiriliyor: {item.Key} -> {fullUrl}");

                if (targetImage != null)
                {
                    AssetLoader.Instance.LoadImageIntoUI(fullUrl, item.File, targetImage);
                }
                else
                {
                    Debug.LogWarning("TestGame scriptinde 'Target Image' boş! Inspector'dan atama yapmalısın.");
                }
            }
            else
            {
                Debug.LogWarning("Config geldi ama içinde hiç Asset (resim) yok!");
            }
        }
        else
        {
            Debug.LogError("Config alınamadı! (Null döndü)");
        }
    }
}
using UnityEngine;
using UnityEngine.UI;
using GraduationProject.Managers;

public class TestGame : MonoBehaviour
{
    public Image targetImage; // Unity Inspector'dan ata

    private async void Start()
    {
        // 1. K Harfi (ID: 9) ve Hafıza Oyunu (ID: 4) konfigürasyonunu çek
        // NOT: ID'ler senin veritabanına göre değişebilir, kontrol et!
        long gameId = 4;
        long letterId = 9;

        Debug.Log("Konfigürasyon çekiliyor...");
        var config = await APIManager.Instance.GetGameConfigAsync(gameId, letterId);

        if (config != null)
        {
            Debug.Log($"Config Geldi! BaseURL: {config.BaseUrl}");

            // 2. Listeden ilk asseti bul (Örn: Kedi)
            if (config.Items.Count > 0)
            {
                var item = config.Items[0]; // İlk elemanı al
                string fullUrl = config.BaseUrl + item.File;
                
                Debug.Log($"İlk Resim Yükleniyor: {item.Key} ({fullUrl})");

                // 3. AssetLoader ile resmi indir ve ekrana bas
                AssetLoader.Instance.LoadImageIntoUI(fullUrl, item.File, targetImage);
            }
        }
        else
        {
            Debug.LogError("Config alınamadı!");
        }
    }
}
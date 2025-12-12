using UnityEngine;
using System.Threading.Tasks;
using GraduationProject.Managers; // AssetLoader ve APIManager için şart!
using GraduationProject.Utilities; // GameContext için şart!

public class GameSceneController : MonoBehaviour
{
    [Header("Oyun Referansı")]
    // Buraya sahnedeki MemoryGameManager (veya Prefab) atanacak
    public BaseGameManager CurrentGameManager; 

    private async void Start()
    {
        // 1. Hangi harfin oynanacağını belirle
        long letterIdToPlay = 9; // Varsayılan (Test - K Harfi)

        // Eğer menüden seçim yapıldıysa onu kullan
        if (GameContext.SelectedLetterId != 0)
        {
            letterIdToPlay = GameContext.SelectedLetterId;
        }

        Debug.Log($"[GameSceneController] Oyun Başlatılıyor. LetterID: {letterIdToPlay}");

        // 2. Oyunu Başlat
        // ESKİSİ: StartGameWithAssets(...) -> ARTIK YOK
        // YENİSİ: InitializeGame(...)
        if (CurrentGameManager != null)
        {
            await CurrentGameManager.InitializeGame(letterIdToPlay);
        }
        else
        {
            Debug.LogError("[GameSceneController] CurrentGameManager atanmamış! Inspector'dan atayın.");
        }
    }
}
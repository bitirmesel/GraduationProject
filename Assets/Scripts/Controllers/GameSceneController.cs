// GameSceneController.cs (örnek)
using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using GraduationProject.Utilities;   // AssetLoader, GameContext

public class GameSceneController : MonoBehaviour
{
    public MemoryGameManager memoryManager;

    private async void Start()
    {
        // 1) Kart arkası
        Sprite backSprite = null;
        if (!string.IsNullOrEmpty(GameContext.CardBackUrl))
            backSprite = await AssetLoader.LoadSpriteAsync(GameContext.CardBackUrl);

        // 2) Kart yüzleri
        var faces = new List<Sprite>();
        foreach (var url in GameContext.ImageUrls)
        {
            var sp = await AssetLoader.LoadSpriteAsync(url);
            if (sp != null) faces.Add(sp);
        }

        // 3) MemoryGameManager’a ver
        memoryManager.StartGameWithAssets(faces, backSprite);
    }
}

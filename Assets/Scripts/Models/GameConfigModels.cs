using System.Collections.Generic;
using Newtonsoft.Json;

namespace GraduationProject.Models
{
    [System.Serializable]
public class GameAssetConfig
{
    [JsonProperty("configId")]
    public string ConfigId { get; set; }

    [JsonProperty("baseUrl")]
    public string BaseUrl { get; set; }

<<<<<<< Updated upstream
    [JsonProperty("audioBaseUrl")]
    public string AudioBaseUrl { get; set; }

    [JsonProperty("items")]
    public List<AssetItem> Items { get; set; }

    [JsonProperty("assetJson")]
    public string AssetJson { get; set; }
}
=======
        [JsonProperty("items")]
        public List<AssetItem> Items { get; set; }

        // --- EKLENMESİ GEREKEN KISIM ---
        // Backend'den gelen string veriyi karşılamak için bunu buraya ekliyoruz.
        [JsonProperty("assetJson")] 
        public string AssetJson { get; set; }
    }
>>>>>>> Stashed changes

    [System.Serializable]
    public class AssetItem
    {
        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("file")]
<<<<<<< Updated upstream
        public string File { get; set; }  // Örn: "kedi.png"

        [JsonProperty("audio")]
        public string Audio { get; set; } // Örn: "kedi.mp3" (background için null)
=======
        public string File { get; set; }
>>>>>>> Stashed changes
    }
}
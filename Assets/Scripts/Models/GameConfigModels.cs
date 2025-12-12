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

        [JsonProperty("items")]
        public List<AssetItem> Items { get; set; }
    }

    [System.Serializable]
    public class AssetItem
    {
        [JsonProperty("key")]
        public string Key { get; set; }   // Örn: "kedi"

        [JsonProperty("file")]
        public string File { get; set; }  // Örn: "kedi.png"
    }
}
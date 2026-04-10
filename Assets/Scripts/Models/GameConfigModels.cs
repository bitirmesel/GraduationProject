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
        public List<GameAssetItem> Items { get; set; }
    }
    
    public class GameAssetItem
    {
        [JsonProperty("key")]
        public string Key { get; set; }   // Örn: "kedi"

        [JsonProperty("file")]
        public string File { get; set; }  // Örn: "kedi.png"
    }
}
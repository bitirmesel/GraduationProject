using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace GraduationProject.Models
{
    [Serializable]
    public class AssetConfig
    {
        [JsonProperty("letter_id")] public string LetterId { get; set; }
        [JsonProperty("assets")] public List<AssetItem> Assets { get; set; }
    }

    [Serializable]
    public class AssetItem
    {
        [JsonProperty("asset_id")] public string AssetId { get; set; }
        [JsonProperty("image_url")] public string ImageUrl { get; set; }
        [JsonProperty("audio_url")] public string AudioUrl { get; set; }
        [JsonProperty("is_correct_answer")] public bool IsCorrectAnswer { get; set; }
    }
}
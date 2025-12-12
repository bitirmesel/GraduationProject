// Models/AssetSetDto.cs
using System.Collections.Generic;

namespace GraduationProject.Models
{
    [System.Serializable]
    public class AssetItemDto
    {
        public string imageUrl;
        public string audioUrl;
    }

    [System.Serializable]
    public class AssetSetDto
    {
        public long assetSetId;
        public long letterId;
        public string letterCode;
        public string gameType;
        public int difficulty;
        public string cardBackUrl;
        public List<AssetItemDto> items;
    }
}

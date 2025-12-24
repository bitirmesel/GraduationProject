using Newtonsoft.Json;

namespace GraduationProject.Models
{
    [System.Serializable]
    public class PlayerLoginResponseDto
    {
        // Response RAW içinde "playerId" yazdığı için burayı böyle güncelliyoruz
        [JsonProperty("playerId")] 
        public long PlayerId { get; set; }

        // Response RAW içinde "nickname" yazdığı için burayı böyle güncelliyoruz
        [JsonProperty("nickname")] 
        public string Nickname { get; set; }

        [JsonProperty("totalScore")]
        public int TotalScore { get; set; }
    }
}
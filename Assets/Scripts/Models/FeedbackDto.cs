using Newtonsoft.Json;
using System; // [Serializable] için bu ŞART

namespace GraduationProject.Models
{
    [Serializable]
    public class FeedbackDto
    {
        [JsonProperty("comment")]
        public string comment;

        [JsonProperty("targetWord")]
        public string targetWord;

        [JsonProperty("score")]
        public int score;

        [JsonProperty("createdAt")]
        public string createdAt;

        [JsonProperty("therapistName")]
        public string therapistName;
    }
}
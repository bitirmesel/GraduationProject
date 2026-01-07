using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace GraduationProject.Models
{
    [Serializable]
    public class PronunciationResponseDto
    {
        // JSON dizisindeki her bir objenin içindeki listeleri temsil eder
        [JsonProperty("overall_result_data")]
        public List<OverallResultData> OverallResult { get; set; }

        [JsonProperty("word_result_data")]
        public List<WordResultData> WordResults { get; set; }
    }

    [Serializable]
    public class OverallResultData
    {
        public double overall_points { get; set; }
        public string user_recording_transcript { get; set; }
    }

    [Serializable]
    public class WordResultData
    {
        public string word { get; set; }
        public string points { get; set; }
        public string speed { get; set; }
    }
}
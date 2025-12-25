using System;

namespace GraduationProject.Models
{
    [Serializable]
    public class PronunciationResponseDto
    {
        public string Word;
        public PronunciationScore Score;
    }

    [Serializable]
    public class PronunciationScore
    {
        public double OverallPoints; // 0 ile 100 arası puan
    }
}
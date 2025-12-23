using System.Collections.Generic;

namespace GraduationProject.Models
{
    public class PronunciationResult
    {
        // Doğru telaffuz edilen kelimelerin listesi
        public List<string> CorrectWords { get; set; } = new List<string>();
        
        // Yanlış telaffuz edilen kelimelerin listesi
        public List<string> IncorrectWords { get; set; } = new List<string>();
        
        // API'den gelen genel başarı skoru (isteğe bağlı)
        public float AccuracyScore { get; set; } 
    }
}
namespace GraduationProject.Models
{
    [System.Serializable]
    public class PlayerLoginResponseDto
    {
        public long PlayerId { get; set; }
        public string Nickname { get; set; }
        public int TotalScore { get; set; }   // backend null dönerse 0'a mapleriz
    }
}
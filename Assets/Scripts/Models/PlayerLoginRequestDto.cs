namespace GraduationProject.Models
{
    [System.Serializable]
    public class PlayerLoginRequestDto
    {
        public string Nickname { get; set; }
        public string Password { get; set; }
    }
}
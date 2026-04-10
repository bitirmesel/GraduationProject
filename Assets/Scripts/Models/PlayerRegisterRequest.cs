namespace GraduationProject.Models
{
    public class PlayerRegisterRequest
{
    public string FullName { get; set; }
    public string Email { get; set; } // Yeni
    public string Nickname { get; set; } // E-posta'yı nickname olarak da kullanabiliriz
    public string Password { get; set; }
    public string PasswordAgain { get; set; }
    public bool IsGoingToClinic { get; set; } // Checkbox verisi
}


}
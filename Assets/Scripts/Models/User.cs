using System;
using Newtonsoft.Json;

namespace GraduationProject.Models
{
    [Serializable]
    public class User
    {
        [JsonProperty("token")] public string Token { get; set; }
        [JsonProperty("user_id")] public int UserId { get; set; }
        [JsonProperty("username")] public string Username { get; set; }
    }

    [Serializable]
    public class LoginRequestDTO
    {
        [JsonProperty("username")] public string Username;
        [JsonProperty("password")] public string Password;
    }
}
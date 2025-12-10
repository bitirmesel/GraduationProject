using System;
using Newtonsoft.Json;

namespace GraduationProject.Models
{
    [Serializable]
    public class TaskItem
    {
        [JsonProperty("task_id")] public int TaskId { get; set; }
        [JsonProperty("letter_code")] public string LetterCode; 
        [JsonProperty("status")] public string Status; // "Assigned", "Completed"
        [JsonProperty("game_type")] public int GameType;
    }
}
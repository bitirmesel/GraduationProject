namespace GraduationProject.Models
{
    [System.Serializable]
    public class PlayerTaskDto
    {
        public long taskId;
        public long gameId;
        public string gameName;
        public long letterId;
        public string letterCode;
        public string note;
        public string status;
        public string assignedAt;
        public string dueAt;
    }
}

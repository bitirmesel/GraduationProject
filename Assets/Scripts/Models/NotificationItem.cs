namespace GraduationProject.Models
{
    public class NotificationItem
    {
        public long id { get; set; }
        public string message { get; set; }
        public long therapistId { get; set; }
        public System.DateTime createdAt { get; set; }
        public bool isRead { get; set; }
    }
}
using System;

namespace GraduationProject.Models
{
    [System.Serializable]
    public class TaskItem
    {
        // Log'dan gördüğümüz gerçek JSON anahtarları:
        // {"taskId":5, "gameName":"...", "letterCode":"D", "status":"ASSIGNED", ...}

        public int taskId;
        public string status;      // Örn: "ASSIGNED"
        public string letterCode;  // Örn: "D"
        public string gameName;    // Örn: "Syllable L1 - Matching"
        public string note;        // Örn: "Haftaya görüşürüz"

        // Eski kodların patlamaması için yardımcı köprüler (Opsiyonel ama güvenli)
        public int TaskId => taskId;
        public string Status => status;

            public long id;
    public string title; // Backend'deki isimlendirmeye dikkat!
    public string description; 
    public bool isCompleted;
    }
}



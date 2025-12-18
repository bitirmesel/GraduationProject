using System.Collections.Generic;

namespace GraduationProject.Models
{
    public static class GameContext
    {
        // --- TEMEL BİLGİLER ---
        public static int PlayerId { get; set; }
        
        // --- SELECTION EKRANINDAN GELENLER ---
        public static long SelectedLetterId { get; set; } 
        public static string SelectedLetterCode { get; set; } // "K", "B"
        public static bool IsFocusMode { get; set; } = false;

        // --- LEVEL MAP EKRANINDAN GELENLER ---
        public static int SelectedDifficulty { get; set; } // 1, 2, 3
        public static string SelectedGameType { get; set; } // "Syllable", "Word"
        public static long SelectedAssetSetId { get; set; } 
        
        // !!! --- İŞTE HATA VEREN DEĞİŞKEN BURADA --- !!!
        // Bazı eski scriptlerin bunu arıyor, o yüzden ekledik.
        public static int SelectedGameId { get; set; } 

        // --- OYUN İÇİN HAZIRLANAN VERİLER ---
        public static string CardBackUrl { get; set; }
        public static List<string> ImageUrls { get; set; } = new List<string>();
        public static List<string> AudioUrls { get; set; } = new List<string>();
    }
}
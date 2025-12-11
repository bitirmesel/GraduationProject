using GraduationProject.Models;

namespace GraduationProject.Utilities
{
    public static class GameContext
    {
        public static long PlayerId;

        // Harf seçimi
        public static long SelectedLetterId;
        public static string SelectedLetterCode;

        // Oyun seçimi
        public static long SelectedGameId;      // Şimdilik kullanmasan da dursun
        public static string SelectedGameType;  // "Memory", "Syllable", "Word", "Sentence"...

        // Zorluk
        public static int SelectedDifficulty;   // 1: Kolay, 2: Orta, 3: Zor

        // Asset set
        public static long SelectedAssetSetId;
        public static AssetConfig CurrentAssetConfig; // ScriptableObject tipin
    }
}

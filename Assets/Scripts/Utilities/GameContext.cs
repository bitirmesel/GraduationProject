using System.Collections.Generic;

namespace GraduationProject.Utilities
{
    public static class GameContext
    {
        // Zaten vardı:
        public static long PlayerId { get; set; }
        public static string PlayerNickname { get; set; }

        // SEÇİLEN HARF
        public static long SelectedLetterId { get; set; }
        public static string SelectedLetterCode { get; set; }

        // SEÇİLEN OYUN
        // "Syllable", "Word", "Sentence", "Memory" vs.
        public static string SelectedGameType { get; set; }

        // ZORLUK: 1=Kolay, 2=Orta, 3=Zor
        public static int SelectedDifficulty { get; set; }

        // Backend’de bir AssetSet kaydı varsa:
        public static long SelectedAssetSetId { get; set; }

        // ↓↓↓ AssetLoader için URL’ler ↓↓↓
        public static string CardBackUrl { get; set; }
        public static List<string> ImageUrls { get; set; } = new List<string>();
        public static List<string> AudioUrls { get; set; } = new List<string>();
    }
}

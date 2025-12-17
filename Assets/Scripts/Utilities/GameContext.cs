using System.Collections.Generic;
using UnityEngine;

namespace GraduationProject.Utilities
{
    public static class GameContext
    {
        public static long PlayerId { get; set; }
        public static string PlayerNickname { get; set; }

        public static long SelectedLetterId { get; set; }
        public static string SelectedLetterCode { get; set; }

        public static long SelectedGameId { get; set; }
        public static string SelectedGameType { get; set; }
        public static int SelectedDifficulty { get; set; }

        public static long SelectedAssetSetId { get; set; }

        public static string CardBackUrl { get; set; }
        public static List<string> ImageUrls { get; set; } = new List<string>();
        public static List<string> AudioUrls { get; set; } = new List<string>();

        public static int CurrentLevelId { get; set; } // Oynanan level

        // --- YENİ EKLENEN ---
        // Eğer bu değer 0'dan büyükse, SelectionScene sadece bu leveli aktif yapar.
        public static int FocusLevelId { get; set; } = 0;

        // ✅ GameScene runtime için:
        public static Sprite MemoryBackSprite;
        public static List<Sprite> MemoryFaceSprites = new List<Sprite>();

        public static bool IsFocusMode { get; set; } = false;

    }
}

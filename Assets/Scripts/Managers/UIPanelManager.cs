using UnityEngine;

namespace GraduationProject.Managers
{
    public class UIPanelManager : MonoBehaviour
    {
        public static UIPanelManager Instance;

        [Header("Panel References")]
        [SerializeField] private GameObject victoryPopup;
        [SerializeField] private GameObject retryPopup;
        [SerializeField] private GameObject pronunciationPanel;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                // Sahne geçişlerinde objenin korunmasını istiyorsan alttaki satırı açabilirsin
                // DontDestroyOnLoad(gameObject); 
            }
            else
            {
                Destroy(gameObject);
            }

            // Sahne yeniden yüklendiğinde nesneleri tekrar bul
            if (pronunciationPanel == null) pronunciationPanel = GameObject.Find("PronunciationPanel");

            if (victoryPopup == null) victoryPopup = GameObject.Find("VictoryPopup");

            if (retryPopup == null) retryPopup = GameObject.Find("RetryPopup");
        }

        // Panelleri açıp kapatan metodlar
        public void ShowPronunciationPanel(bool show)
        {
            if (pronunciationPanel != null) pronunciationPanel.SetActive(show);
            else Debug.LogError("PronunciationPanel referansı UIPanelManager'da eksik!");
        }

        public void ShowVictoryPanel(bool show)
        {
            if (victoryPopup != null) victoryPopup.SetActive(show);
            else Debug.LogError("VictoryPopup referansı UIPanelManager'da eksik!");
        }

        public void ShowRetryPanel(bool show)
        {
            if (retryPopup != null) retryPopup.SetActive(show);
            else Debug.LogError("RetryPopup referansı UIPanelManager'da eksik!");
        }

        // Tüm panelleri tek seferde kapatmak için yardımcı metod (Opsiyonel)
        public void HideAllPanels()
        {
            ShowPronunciationPanel(false);
            ShowVictoryPanel(false);
            ShowRetryPanel(false);
        }

        private void OnEnable() // Sahne yenilendiğinde veya obje aktif olduğunda çalışır
        {
            // Referansları her seferinde hiyerarşiden bul
            if (pronunciationPanel == null) pronunciationPanel = GameObject.Find("PronunciationPanel");
        }
    }
}
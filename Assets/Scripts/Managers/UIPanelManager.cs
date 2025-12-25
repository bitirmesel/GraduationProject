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
            // Singleton mantığı: Sahne yenilense bile taze bir instance oluşmasını sağla
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            RefreshReferences();
        }

        private void Start()
        {
            RefreshReferences();
        }

        // --- EN KRİTİK METOT: SAHNE YENİLENDİĞİNDE ÇALIŞIR ---
        public void RefreshReferences()
        {
            // Missing (ölü) referansları kontrol et ve hiyerarşiden bul
            // GameObject.Find sadece aktif objeleri bulur. Kapalı objeleri bulmak için 
            // transform.Find veya Resources.FindObjectsOfTypeAll kullanılır.

            if (pronunciationPanel == null || pronunciationPanel.Equals(null))
                pronunciationPanel = FindInactiveObject("PronunciationPanel");

            if (victoryPopup == null || victoryPopup.Equals(null))
                victoryPopup = FindInactiveObject("VictoryPopup");

            if (retryPopup == null || retryPopup.Equals(null))
                retryPopup = FindInactiveObject("RetryPopup");
        }

        // Kapalı (Inactive) olan panelleri bulabilen yardımcı fonksiyon
        private GameObject FindInactiveObject(string objectName)
        {
            GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (GameObject obj in allObjects)
            {
                // Sadece sahnedeki (dosyadaki değil) ve doğru isimdeki objeyi seç
                if (obj.name == objectName && obj.hideFlags == HideFlags.None)
                {
                    return obj;
                }
            }
            return null;
        }

        public void ShowPronunciationPanel(bool show)
        {
            // Panel açılmadan hemen önce referansı son bir kez kontrol et
            if (pronunciationPanel == null || pronunciationPanel.Equals(null))
            {
                // FindInactiveObject metodunu bir önceki mesajda vermiştik, onu kullanıyoruz
                pronunciationPanel = FindInactiveObject("PronunciationPanel");
            }

            if (pronunciationPanel != null)
            {
                pronunciationPanel.SetActive(show);
            }
            else
            {
                Debug.LogError("PronunciationPanel hiyerarşide bulunamadı! İsim kontrolü yapın.");
            }
        }

        public void ShowVictoryPanel(bool show)
        {
            RefreshReferences();
            if (victoryPopup != null) victoryPopup.SetActive(show);
            else Debug.LogError("VictoryPopup referansı bulunamadı!");
        }

        public void ShowRetryPanel(bool show)
        {
            RefreshReferences();
            if (retryPopup != null) retryPopup.SetActive(show);
            else Debug.LogError("RetryPopup referansı bulunamadı!");
        }

        public void HideAllPanels()
        {
            ShowPronunciationPanel(false);
            ShowVictoryPanel(false);
            ShowRetryPanel(false);
        }

        private void OnEnable()
        {
            RefreshReferences();
        }
    }
}
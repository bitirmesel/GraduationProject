using UnityEngine;
using System.Collections.Generic;

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
            // Singleton Yapılandırması
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            // Sahne başladığında referansları otomatik bul ve ata
            RefreshReferences();
        }

        private void Start()
        {
            RefreshReferences();
        }

        /// <summary>
        /// Sahnede kapalı (inactive) olsalar dahi isimlerine göre objeleri bulur ve atar.
        /// </summary>
        public void RefreshReferences()
        {
            if (pronunciationPanel == null || pronunciationPanel.Equals(null))
                pronunciationPanel = FindInactiveObjectInScene("PronunciationPanel");

            if (victoryPopup == null || victoryPopup.Equals(null))
                victoryPopup = FindInactiveObjectInScene("VictoryPopup");

            if (retryPopup == null || retryPopup.Equals(null))
                retryPopup = FindInactiveObjectInScene("RetryPopup");

            // Başarı durumunu kontrol et (Debug log)
            if (pronunciationPanel != null) Debug.Log("[UIPanelManager] PronunciationPanel başarıyla bağlandı.");
        }

        private GameObject FindInactiveObjectInScene(string objectName)
        {
            // Resources.FindObjectsOfTypeAll hem aktif hem pasif sahne objelerini bulur
            GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (GameObject obj in allObjects)
            {
                // Sadece sahnedeki objeleri seç (Asset klasöründeki prefabları hariç tut)
                if (obj.name == objectName && obj.hideFlags == HideFlags.None)
                {
                    return obj;
                }
            }
            return null;
        }

        public void ShowPronunciationPanel(bool show)
        {
            // Panel gösterilmeden önce referansı son kez kontrol et
            if (pronunciationPanel == null) RefreshReferences();

            if (pronunciationPanel != null)
            {
                pronunciationPanel.SetActive(show);
            }
            else
            {
                Debug.LogError($"[UIPanelManager] PronunciationPanel bulunamadı! Lütfen hiyerarşide isminin '{nameof(pronunciationPanel)}' olduğundan emin olun.");
            }
        }

        public void ShowVictoryPanel(bool show)
        {
            if (victoryPopup == null) RefreshReferences();

            if (victoryPopup != null)
            {
                victoryPopup.SetActive(show);
            }
            else
            {
                Debug.LogError("[UIPanelManager] VictoryPopup referansı bulunamadı!");
            }
        }

        public void ShowRetryPanel(bool show)
        {
            if (retryPopup == null) RefreshReferences();

            if (retryPopup != null)
            {
                retryPopup.SetActive(show);
            }
            else
            {
                Debug.LogError("[UIPanelManager] RetryPopup referansı bulunamadı!");
            }
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
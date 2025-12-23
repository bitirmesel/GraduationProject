using UnityEngine;

namespace GraduationProject.Managers
{
    public class UIPanelManager : MonoBehaviour
    {
        // Sadece bir tane Instance tanımı olmalı
        public static UIPanelManager Instance;

        [SerializeField] private GameObject victoryPopup; 
        [SerializeField] private GameObject retryPopup;
        [SerializeField] private GameObject pronunciationPanel;

        // Sadece bir tane Awake metodu olmalı
        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void ShowPronunciationPanel(bool show) => pronunciationPanel?.SetActive(show);
        public void ShowVictoryPanel(bool show) => victoryPopup?.SetActive(show);
        public void ShowRetryPanel(bool show) => retryPopup?.SetActive(show);
    }
}
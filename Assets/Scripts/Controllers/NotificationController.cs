using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Threading.Tasks;
using GraduationProject.Managers;
using GraduationProject.Models;
using GraduationProject.Utilities;
using Newtonsoft.Json; // Eğer proje Newtonsoft kullanıyorsa debug için harika olur

namespace GraduationProject.Controllers
{
    public class NotificationController : MonoBehaviour
    {
        [Header("Geliştirici Ayarları")]
        [SerializeField] private int _debugPlayerId = 3;

        [Header("Panel Yönetimi")]
        public GameObject activeTaskPanel;
        public GameObject emptyStatePanel;

        [Header("İçerik Referansları")]
        public TextMeshProUGUI notificationText;
        public Button actionButton;

        private int _targetLevelId = 0;

        private async void Start()
        {
            if (actionButton != null)
            {
                actionButton.onClick.RemoveAllListeners();
                actionButton.onClick.AddListener(OnActionClick);
            }
            await RefreshTasksAsync();
        }

        public async Task RefreshTasksAsync()
        {
            long currentId = GameContext.PlayerId;

            // Debug modu veya Login kontrolü
            if (currentId <= 0)
            {
#if UNITY_EDITOR
                currentId = _debugPlayerId;
#else
                ShowEmptyState(); return;
#endif
            }

            if (APIManager.Instance == null) { ShowEmptyState(); return; }

            // Verileri Çek
            var tasks = await APIManager.Instance.GetTasksAsync(currentId);

            if (tasks != null && tasks.Count > 0)
            {
                // --- DÜZELTME 1: "ASSIGNED" HEPSİ BÜYÜK HARF OLMALI ---
                // API'den "ASSIGNED" geliyor, C# string karşılaştırması hassastır.
                var newTasks = tasks.FindAll(t => t.status == "ASSIGNED");

                if (newTasks.Count > 0)
                {
                    var priorityTask = newTasks[0];
                    _targetLevelId = priorityTask.taskId;

                    string fullText = "<size=120%><b>Bugünün Görevleri</b></size>\n\n";

                    foreach (var task in newTasks)
                    {
                        // --- DÜZELTME 2: YENİ DEĞİŞKEN İSİMLERİ ---
                        // letter -> letterCode
                        // description -> gameName + note

                        string letterVal = string.IsNullOrEmpty(task.letterCode) ? "?" : task.letterCode;

                        string descVal = task.gameName;
                        if (!string.IsNullOrEmpty(task.note)) descVal += $" ({task.note})";

                        // Ekrana Yazdır
                        fullText += $"<color=#FFA500>●</color> <b>Harf '{letterVal}'</b> : {descVal}\n\n";
                    }

                    if (notificationText != null) notificationText.text = fullText;

                    if (activeTaskPanel != null) activeTaskPanel.SetActive(true);
                    if (emptyStatePanel != null) emptyStatePanel.SetActive(false);
                }
                else
                {
                    // Görev var ama hepsi tamamlanmışsa
                    ShowEmptyState();
                }
            }
            else
            {
                ShowEmptyState();
            }
        }

        // ... (ShowEmptyState ve OnActionClick fonksiyonları aynen kalacak) ...
        private void ShowEmptyState()
        {
            if (activeTaskPanel != null) activeTaskPanel.SetActive(false);
            if (emptyStatePanel != null) emptyStatePanel.SetActive(true);
        }

        private void OnActionClick()
        {
            // ID göndermiyoruz, sadece "Modu Aç" diyoruz.
            // Çünkü SelectionScene zaten görevleri API'den çekip hangileri olduğunu bilecek.

            GameContext.IsFocusMode = true;
            UnityEngine.SceneManagement.SceneManager.LoadScene("SelectionScene");
        }
    }
}
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // <-- BUNU EKLEMEYİ UNUTMA (Yazı için gerekli)
using GraduationProject.Managers;
using GraduationProject.Models;
using GraduationProject.Utilities;

namespace GraduationProject.Controllers
{
    public class SelectionController : MonoBehaviour
    {
        [Header("UI Referansları")]
        public ScrollRect scrollView;

        [Header("Bildirim Ayarları")]
        public GameObject notificationBadge;       // Kırmızı Dairenin Kendisi
        public TextMeshProUGUI notificationText;   // Dairenin içindeki Yazı (Sayı)

        // SESSİZ HARFLER
        private readonly string[] _orderedConsonants = new string[]
        {
            "B", "C", "Ç", "D", "F", "G", "H", "J", "K", "L",
            "M", "N", "P", "R", "S", "Ş", "T", "V", "Y", "Z"
        };

        private async void Start()
        {
            AutoMapLevelButtons();

            if (scrollView != null)
            {
                Canvas.ForceUpdateCanvases();
                scrollView.verticalNormalizedPosition = 0f;
            }

            if (APIManager.Instance == null || GameContext.PlayerId <= 0)
            {
                Debug.LogWarning("[SelectionController] API veya PlayerId eksik.");
                return;
            }

            // Görevleri Çek
            List<TaskItem> tasks = await APIManager.Instance.GetTasksAsync(GameContext.PlayerId);

            if (tasks != null)
            {
                UpdateButtonsVisuals(tasks);

                // DİNAMİK SAYAÇ FONKSİYONU
                UpdateNotificationCount(tasks);
            }
        }

        private void AutoMapLevelButtons()
        {
            var allButtons = Object.FindObjectsByType<LevelIdentifier>(FindObjectsSortMode.None);
            foreach (var btn in allButtons)
            {
                string objName = btn.gameObject.name;
                if (!objName.Contains("_")) continue;

                string letterFromObj = objName.Split('_')[1];
                int index = System.Array.IndexOf(_orderedConsonants, letterFromObj);

                if (index != -1)
                {
                    btn.levelID = index + 1;
                    btn.letterCode = letterFromObj;
                    if (btn.letterText != null) btn.letterText.text = letterFromObj;
                }
            }
        }

        private void UpdateButtonsVisuals(List<TaskItem> tasks)
        {
            var allButtons = Object.FindObjectsByType<LevelIdentifier>(FindObjectsSortMode.None);
            foreach (var task in tasks)
            {
                var targetBtn = allButtons.FirstOrDefault(x => x.levelID == task.TaskId);
                if (targetBtn != null) ApplyButtonStatus(targetBtn, task);
            }
        }

        private void ApplyButtonStatus(LevelIdentifier btn, TaskItem task)
        {
            if (btn.myButton == null) return;

            switch (task.Status)
            {
                case "Completed":
                    if (btn.myImage) btn.myImage.color = Color.green;
                    if (btn.lockImage) btn.lockImage.SetActive(false);
                    btn.myButton.interactable = true;
                    break;
                case "Assigned":
                    if (btn.myImage) btn.myImage.color = Color.yellow;
                    if (btn.lockImage) btn.lockImage.SetActive(false);
                    btn.myButton.interactable = true;
                    break;
                default:
                    if (btn.myImage) btn.myImage.color = Color.gray;
                    if (btn.lockImage) btn.lockImage.SetActive(true);
                    btn.myButton.interactable = false;
                    break;
            }
        }

        // --- YENİ DİNAMİK SAYAÇ KODU ---
        private void UpdateNotificationCount(List<TaskItem> tasks)
        {
            if (notificationBadge == null) return;

            // 1. Sadece "Assigned" (Yeni) olanları say
            int newCount = tasks.Count(t => t.Status == "Assigned");

            Debug.Log($"[SelectionController] Yeni Bildirim Sayısı: {newCount}");

            if (newCount > 0)
            {
                // Bildirim varsa rozeti aç
                notificationBadge.SetActive(true);

                // Sayıyı yazdır
                if (notificationText != null)
                {
                    notificationText.text = newCount.ToString();
                }
            }
            else
            {
                // Bildirim yoksa (0 ise) rozeti kapat
                notificationBadge.SetActive(false);
            }
        }


        // SelectionController.cs içine ekle:
        public void BildirimSayfasiniAc()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("NotificationScene");
        }
    }
}

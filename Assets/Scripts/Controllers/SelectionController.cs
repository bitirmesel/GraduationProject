using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
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
        public GameObject notificationBadge;
        public TextMeshProUGUI notificationText;

        private readonly string[] _orderedConsonants = new string[]
        {
            "B", "C", "Ç", "D", "F", "G", "H", "J", "K", "L",
            "M", "N", "P", "R", "S", "Ş", "T", "V", "Y", "Z"
        };

        private async void Start()
        {
            // 1. Önce her şeyi aç (Varsayılan durum)
            AutoMapLevelButtons();

            if (scrollView != null)
            {
                Canvas.ForceUpdateCanvases();
                scrollView.verticalNormalizedPosition = 0f;
            }

            if (APIManager.Instance == null) return;
            long pId = GameContext.PlayerId;
            if (pId <= 0) return;

            // 2. Verileri Çek
            List<TaskItem> tasks = await APIManager.Instance.GetTasksAsync(pId);

            if (tasks != null)
            {
                // Renkleri boya (Yeşil/Sarı)
                UpdateButtonsVisuals(tasks);
                UpdateNotificationCount(tasks);

                // 3. ODAKLANMA MODU KONTROLÜ
                // Eğer Notification'dan geldiysek, görevi olmayanları kapat!
                if (GameContext.IsFocusMode)
                {
                    Debug.Log("[Selection] Odaklanma Modu Aktif: Sadece ödevler açık kalacak.");
                    FilterTasksOnly(tasks);

                    // Modu kapat ki bir dahaki girişte normal açılsın
                    GameContext.IsFocusMode = false;
                }
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
                    btn.letterCode = letterFromObj; // Örn: "D", "C"
                    if (btn.letterText != null) btn.letterText.text = letterFromObj;

                    // Herkesi aktif başlat
                    if (btn.myButton) btn.myButton.interactable = true;
                    if (btn.lockImage) btn.lockImage.SetActive(false);
                    if (btn.myImage) btn.myImage.color = Color.white;
                }
            }
        }

        private void UpdateButtonsVisuals(List<TaskItem> tasks)
        {
            var allButtons = Object.FindObjectsByType<LevelIdentifier>(FindObjectsSortMode.None);

            // "ASSIGNED" olanları SARI yap
            foreach (var task in tasks.Where(t => t.status == "ASSIGNED"))
            {
                // Harf koduna göre butonu bul (ID yerine LetterCode daha güvenli)
                var btn = allButtons.FirstOrDefault(b => b.letterCode == task.letterCode);
                if (btn != null && btn.myImage) btn.myImage.color = Color.yellow;
            }

            // "COMPLETED" olanları YEŞİL yap
            foreach (var task in tasks.Where(t => t.status == "COMPLETED"))
            {
                var btn = allButtons.FirstOrDefault(b => b.letterCode == task.letterCode);
                if (btn != null && btn.myImage) btn.myImage.color = Color.green;
            }
        }

        // --- YENİ FİLTRELEME FONKSİYONU ---
        private void FilterTasksOnly(List<TaskItem> tasks)
        {
            var allButtons = Object.FindObjectsByType<LevelIdentifier>(FindObjectsSortMode.None);

            // Hangi harflerin görevi var? (Listesini çıkar)
            // Sadece "ASSIGNED" (Ödev) olanları aktif tutmak istiyorsan:
            var activeLetters = tasks
                .Where(t => t.status == "ASSIGNED")
                .Select(t => t.letterCode)
                .ToList();

            foreach (var btn in allButtons)
            {
                // Eğer butonun harfi, aktif listemizde YOKSA -> KİLİTLE
                if (!activeLetters.Contains(btn.letterCode))
                {
                    if (btn.myButton) btn.myButton.interactable = false; // Tıklamayı kapat

                    // Rengi soldur (Gri ve Şeffaf)
                    if (btn.myImage)
                    {
                        var col = btn.myImage.color;
                        col.a = 0.2f;
                        btn.myImage.color = Color.gray;
                    }
                }
                else
                {
                    // Listede varsa zaten UpdateButtonsVisuals onu Sarı yapmıştı, dokunma.
                    Debug.Log($"[Focus] Açık kalan harf: {btn.letterCode}");
                }
            }
        }

        private void UpdateNotificationCount(List<TaskItem> tasks)
        {
            if (notificationBadge == null) return;
            // API'den "ASSIGNED" geliyor, dikkat!
            int newCount = tasks.Count(t => t.status == "ASSIGNED");

            if (newCount > 0)
            {
                notificationBadge.SetActive(true);
                if (notificationText != null) notificationText.text = newCount.ToString();
            }
            else
            {
                notificationBadge.SetActive(false);
            }
        }

        // --- BU FONKSİYON EKSİKTİ, BUNU EKLE ---
        public void BildirimSayfasiniAc()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("NotificationScene");
        }
    }

}
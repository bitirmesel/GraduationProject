using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Threading.Tasks; // Task kullanımı için
using GraduationProject.Managers; // Kendi namespace'lerin
using GraduationProject.Models;
using GraduationProject.Utilities;

namespace GraduationProject.Controllers
{
    public class SelectionController : MonoBehaviour
    {
        [Header("UI Referansları")]
        public ScrollRect scrollView;

        // SESSİZ HARFLER LİSTESİ (Sıralama ID belirler: B=1, C=2...)
        // Ğ hariç, alfabetik sıra.
        private readonly string[] _orderedConsonants = new string[]
        {
            "B", "C", "Ç", "D", "F", "G", "H", "J", "K", "L", 
            "M", "N", "P", "R", "S", "Ş", "T", "V", "Y", "Z"
        };

        private async void Start()
        {
            // 1) ÖNCE BUTONLARI OTOMATİK HARİTALANDIR
            // API isteği gelmeden önce butonların ID'lerinin set edilmiş olması lazım.
            AutoMapLevelButtons();

            // 2) Scroll Ayarı
            if (scrollView != null)
            {
                Canvas.ForceUpdateCanvases();
                scrollView.verticalNormalizedPosition = 0f;
            }

            // 3) Güvenlik Kontrolleri
            if (APIManager.Instance == null || GameContext.PlayerId <= 0)
            {
                Debug.LogWarning("APIManager eksik veya PlayerId yok. Test modunda olabilirsin.");
                // Test için return etmiyoruz, butonları görmen için devam etsin (Opsiyonel)
            }

            // 4) API'den Görevleri Çek
            // PlayerId varsa API'ye git, yoksa hata vermemesi için boş liste dön
            List<TaskItem> tasks = null;
            if (GameContext.PlayerId > 0 && APIManager.Instance != null)
            {
                tasks = await APIManager.Instance.GetTasksAsync(GameContext.PlayerId);
            }

            // 5) Butonları API verisine göre boya
            if (tasks != null)
            {
                UpdateButtonsVisuals(tasks);
            }
        }

        /// <summary>
        /// Sahnedeki LevelIdentifier butonlarını bulur, isimlerine bakar (örn: Level_B)
        /// ve listemize göre ID ve Harf Kodu atamasını otomatik yapar.
        /// </summary>
        private void AutoMapLevelButtons()
        {
            // Sahnedeki tüm level butonlarını bul
            var allButtons = FindObjectsOfType<LevelIdentifier>();

            foreach (var btn in allButtons)
            {
                // Objenin adı "Level_B" gibi olmalı. "_" işaretinden sonrasını alıyoruz.
                // Örnek: "Level_B" -> split[1] = "B"
                string objName = btn.gameObject.name;
                
                if (!objName.Contains("_"))
                {
                    Debug.LogWarning($"[AutoMap] '{objName}' isimlendirme formatına uymuyor! (Beklenen: Level_X)");
                    continue;
                }

                string letterFromObj = objName.Split('_')[1]; // B, C, Ç ...

                // Listede kaçıncı sırada olduğunu bul
                int index = System.Array.IndexOf(_orderedConsonants, letterFromObj);

                if (index != -1)
                {
                    // ID = Index + 1 (Çünkü Array 0'dan başlar, veritabanı genelde 1'den)
                    btn.levelID = index + 1;
                    btn.letterCode = letterFromObj;
                    
                    // Buton üzerindeki Text'i de hemen güncelleyelim (Görseli netleştirmek için)
                    if (btn.letterText != null)
                        btn.letterText.text = letterFromObj;

                    // Debug.Log($"[AutoMap] {objName} atandı -> ID: {btn.levelID}, Harf: {btn.letterCode}");
                }
                else
                {
                    Debug.LogError($"[AutoMap] '{objName}' objesindeki '{letterFromObj}' harfi sessiz harfler listesinde yok!");
                }
            }
        }

        private void UpdateButtonsVisuals(List<TaskItem> tasks)
        {
            var allButtons = FindObjectsOfType<LevelIdentifier>();

            foreach (var task in tasks)
            {
                // Artık butonların ID'si otomatik atandı, güvenle eşleştirebiliriz.
                var targetBtn = allButtons.FirstOrDefault(x => x.levelID == task.TaskId);

                if (targetBtn != null)
                {
                    ApplyButtonStatus(targetBtn, task);
                }
            }
        }

        private void ApplyButtonStatus(LevelIdentifier btn, TaskItem task)
        {
            // Senin yazdığın switch-case yapısı buraya
             switch (task.Status)
            {
                case "Completed":
                    if(btn.myImage) btn.myImage.color = Color.green;
                    if(btn.lockImage) btn.lockImage.SetActive(false);
                    btn.myButton.interactable = true;
                    break;

                case "Assigned":
                    if(btn.myImage) btn.myImage.color = Color.yellow;
                    if(btn.lockImage) btn.lockImage.SetActive(false);
                    btn.myButton.interactable = true;
                    break;

                default: // Locked
                    if(btn.myImage) btn.myImage.color = Color.gray;
                    if(btn.lockImage) btn.lockImage.SetActive(true);
                    btn.myButton.interactable = false;
                    break;
            }
        }
    }
}
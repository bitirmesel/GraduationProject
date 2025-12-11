using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq; // Listede arama yapmak için gerekli
using GraduationProject.Managers;
using GraduationProject.Models;
using GraduationProject.Utilities;

namespace GraduationProject.Controllers
{
    public class SelectionController : MonoBehaviour
    {
        [Header("UI Referansları")]
        public ScrollRect scrollView; 
        
        // Sahnedeki (Elle koyduğun) tüm butonların listesi
        private List<LevelIdentifier> tumButonlar;

        private async void Start()
        {
            // 1. Sahnedeki elle koyduğun tüm LevelIdentifier scriptlerini bul ve listeye al
            // (MapHolder'ın içindekileri bulur)
            tumButonlar = new List<LevelIdentifier>(FindObjectsOfType<LevelIdentifier>());

            // Scroll'u en aşağıya çek
            if (scrollView != null) 
            {
                Canvas.ForceUpdateCanvases();
                scrollView.verticalNormalizedPosition = 0f; 
            }

            // 2. Backend'den veriyi çek (Artık ID'ye göre eşleştireceğiz)
            var tasks = await APIManager.Instance.GetTasksAsync(GameContext.PlayerId);

            if (tasks != null)
            {
                UpdateButtons(tasks);
            }
        }

        private void UpdateButtons(List<TaskItem> tasks)
        {
            // Backend'den gelen her bir görev için...
            foreach (var task in tasks)
            {
                // Bu Task ID'sine sahip butonu sahnede bul
                // Örn: Backend TaskID=1 gönderdi, sahnede LevelID=1 olan butonu buluyoruz.
                var hedefButon = tumButonlar.FirstOrDefault(x => x.levelID == task.TaskId);

                if (hedefButon != null)
                {
                    // Butonu Bulduk! Şimdi durumunu güncelle.
                    Ayarla(hedefButon, task);
                }
            }
        }

        private void Ayarla(LevelIdentifier btnScript, TaskItem task)
        {
            // Yazıyı Backend'den gelenle güncelle (veya elle yazdığın kalabilir)
            if(btnScript.letterText) btnScript.letterText.text = task.LetterCode;

            switch (task.Status)
            {
                case "Completed":
                    btnScript.myImage.color = Color.green;
                    if (btnScript.lockImage) btnScript.lockImage.SetActive(false);
                    btnScript.myButton.interactable = true;
                    break;

                case "Assigned":
                    btnScript.myImage.color = Color.yellow;
                    if (btnScript.lockImage) btnScript.lockImage.SetActive(false);
                    btnScript.myButton.interactable = true;
                    break;

                default: // Locked
                    btnScript.myImage.color = Color.gray;
                    if (btnScript.lockImage) btnScript.lockImage.SetActive(true);
                    btnScript.myButton.interactable = false;
                    break;
            }
            
            // Tıklama özelliği
            btnScript.myButton.onClick.RemoveAllListeners(); // Eski tıklamaları temizle
            btnScript.myButton.onClick.AddListener(() => 
            {
                Debug.Log($"Level Seçildi! ID: {task.TaskId}, Harf: {task.LetterCode}");
                // GameContext.CurrentTaskID = task.TaskId; // İleride açacağız
                // SceneManager.LoadScene("GameScene");
            });
        }
    }
}
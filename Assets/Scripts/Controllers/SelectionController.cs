using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // ScrollRect ve Image için gerekli
using System.Linq;    // Listede arama yapmak için gerekli
using TMPro;          // TextMeshPro için gerekli
using GraduationProject.Managers;
using GraduationProject.Models;
using GraduationProject.Utilities;

namespace GraduationProject.Controllers
{
    public class SelectionController : MonoBehaviour
    {
        // --- İŞTE BU SATIR EKSİKTİ, O YÜZDEN HATA VERİYORDU ---
        [Header("UI Referansları")]
        public ScrollRect scrollView;
        // ------------------------------------------------------

private async void Start()
{
    // 1) Scroll'u en aşağı çekme kısmı
    if (scrollView != null)
    {
        Canvas.ForceUpdateCanvases();
        scrollView.verticalNormalizedPosition = 0f;
    }
    else
    {
        Debug.LogWarning("[SelectionController] Scroll View inspector'da atanmamış!");
    }

    // 2) APIManager var mı?
    if (APIManager.Instance == null)
    {
        Debug.LogError("[SelectionController] APIManager.Instance = null! " +
                       "Bu sahneyi doğrudan çalıştırıyorsun ya da sahnede APIManager yok. " +
                       "Oyunu LoginScene'den başlat veya sahneye APIManager prefabını ekle.");
        return;
    }

    // 3) PlayerId set edilmiş mi?
    if (GameContext.PlayerId <= 0)
    {
        Debug.LogWarning("[SelectionController] GameContext.PlayerId = 0 veya set edilmemiş. " +
                         "Muhtemelen login akışından gelmiyorsun.");
        return;
    }

    // 4) Görevleri çek
    var tasks = await APIManager.Instance.GetTasksAsync(GameContext.PlayerId);

    if (tasks == null)
    {
        Debug.LogWarning("[SelectionController] tasks listesi null döndü.");
        return;
    }

    // 5) Sahnedeki butonları bul
    var tumButonlar = FindObjectsOfType<LevelIdentifier>();

    foreach (var task in tasks)
    {
        var hedefButon = tumButonlar.FirstOrDefault(x => x.levelID == task.TaskId);
        if (hedefButon != null)
        {
            Ayarla(hedefButon, task);
        }
    }
}


        private void Ayarla(LevelIdentifier btn, TaskItem task)
        {
            // Yazıyı güncelle
            if (btn.letterText) btn.letterText.text = task.LetterCode;

            // Rengi ve kilidi güncelle
            switch (task.Status)
            {
                case "Completed":
                    btn.myImage.color = Color.green;
                    if (btn.lockImage) btn.lockImage.SetActive(false);
                    btn.myButton.interactable = true;
                    break;

                case "Assigned":
                    btn.myImage.color = Color.yellow;
                    if (btn.lockImage) btn.lockImage.SetActive(false);
                    btn.myButton.interactable = true;
                    break;

                default: // Locked
                    btn.myImage.color = Color.gray;
                    if (btn.lockImage) btn.lockImage.SetActive(true);
                    btn.myButton.interactable = false;
                    break;
            }
        }
    }
}
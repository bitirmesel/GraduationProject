using System.Collections.Generic;
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
        [Header("Ayarlar")]
        public GameObject levelButtonPrefab; 
        public Transform pointsContainer;
        
        // --- İŞTE EKSİK OLAN KISIM BURASIYDI ---
        [Header("UI Referansları")]
        public ScrollRect scrollView; 
        // ---------------------------------------

        private async void Start()
        {
            // Scroll'u en aşağıya çek (Canvas'ın güncellenmesi için ufak bir bekleme ile)
            if (scrollView != null) 
            {
                Canvas.ForceUpdateCanvases();
                scrollView.verticalNormalizedPosition = 0f; 
            }

            var tasks = await APIManager.Instance.GetTasksAsync(GameContext.PlayerId);
            if (tasks == null || tasks.Count == 0) return;

            SpawnLevelButtons(tasks);
        }

        private void SpawnLevelButtons(List<TaskItem> tasks)
        {
            int pointIndex = 0;
            foreach (var task in tasks)
            {
                if (pointIndex >= pointsContainer.childCount) break;

                Transform targetPoint = pointsContainer.GetChild(pointIndex);
                GameObject btnObj = Instantiate(levelButtonPrefab, targetPoint);
                
                // Butonu noktanın tam merkezine koy
                btnObj.transform.localPosition = Vector3.zero;

                SetupButtonVisuals(btnObj, task);
                pointIndex++;
            }
        }

        private void SetupButtonVisuals(GameObject btnObj, TaskItem task)
        {
            // İsimlendirme hatası olmasın diye hem Txt_Letter hem de Text (TMP) arıyoruz
            var letterText = btnObj.transform.Find("Txt_Letter")?.GetComponent<TMP_Text>();
            if (letterText == null) letterText = btnObj.transform.Find("Text (TMP)")?.GetComponent<TMP_Text>();
            
            if (letterText != null) letterText.text = task.LetterCode;

            var lockImg = btnObj.transform.Find("Img_Lock")?.gameObject;
            Image btnImage = btnObj.GetComponent<Image>();
            Button btn = btnObj.GetComponent<Button>();

            switch (task.Status)
            {
                case "Completed":
                    btnImage.color = Color.green;
                    if (lockImg) lockImg.SetActive(false);
                    btn.interactable = true;
                    break;
                case "Assigned":
                    btnImage.color = Color.yellow;
                    if (lockImg) lockImg.SetActive(false);
                    btn.interactable = true;
                    break;
                default: 
                    btnImage.color = Color.gray;
                    if (lockImg) lockImg.SetActive(true);
                    btn.interactable = false;
                    break;
            }
        }
    }
}
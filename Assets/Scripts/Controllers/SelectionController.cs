using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using GraduationProject.Managers;
using GraduationProject.Models;

namespace GraduationProject.Controllers
{
    public class SelectionController : MonoBehaviour
    {
        [Header("UI Referansları")]
        public ScrollRect scrollView;

        // SESSİZ HARFLER (ID sırası: B=1, C=2... T=17 ...)
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

            if (APIManager.Instance == null)
            {
                Debug.LogWarning("[SelectionController] APIManager.Instance yok. Sadece buton mapping çalıştı.");
                return;
            }

            // PlayerId yoksa API çağırma (ama butonlar yine görünür)
            if (GraduationProject.Utilities.GameContext.PlayerId <= 0)
            {
                Debug.LogWarning("[SelectionController] PlayerId yok. Task çekilmedi.");
                return;
            }

            List<TaskItem> tasks = await APIManager.Instance.GetTasksAsync(GraduationProject.Utilities.GameContext.PlayerId);
            if (tasks != null) UpdateButtonsVisuals(tasks);
        }

        private void AutoMapLevelButtons()
        {
            // Unity 6: FindObjectsByType önerilir
            var allButtons = Object.FindObjectsByType<LevelIdentifier>(FindObjectsSortMode.None);

            foreach (var btn in allButtons)
            {
                string objName = btn.gameObject.name;

                if (!objName.Contains("_"))
                {
                    Debug.LogWarning($"[AutoMap] '{objName}' format yanlış. Beklenen: Level_X (örn Level_T)");
                    continue;
                }

                string letterFromObj = objName.Split('_')[1]; // "T" vs.

                int index = System.Array.IndexOf(_orderedConsonants, letterFromObj);
                if (index == -1)
                {
                    Debug.LogError($"[AutoMap] '{objName}' içindeki '{letterFromObj}' listede yok!");
                    continue;
                }

                btn.levelID = index + 1;
                btn.letterCode = letterFromObj;

                if (btn.letterText != null)
                    btn.letterText.text = letterFromObj;
            }
        }

        private void UpdateButtonsVisuals(List<TaskItem> tasks)
        {
            var allButtons = Object.FindObjectsByType<LevelIdentifier>(FindObjectsSortMode.None);

            foreach (var task in tasks)
            {
                // Senin backend’de LetterId yerine TaskId kullanıyorsan burası öyle kalır.
                // Eğer backend letterId dönüyorsa: x.levelID == task.LetterId şeklinde değiştir.
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

                default: // Locked
                    if (btn.myImage) btn.myImage.color = Color.gray;
                    if (btn.lockImage) btn.lockImage.SetActive(true);
                    btn.myButton.interactable = false;
                    break;
            }
        }
    }
}

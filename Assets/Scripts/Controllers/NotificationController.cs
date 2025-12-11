using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GraduationProject.Managers;
using GraduationProject.Models;
using GraduationProject.Utilities;

namespace GraduationProject.Controllers
{
    public class NotificationController : MonoBehaviour
    {
        private TMP_Text _txtTitle;
        private TMP_Text _txtSubtitle;
        private TMP_Text _txtEmpty;
        private TMP_Text _txtTaskBody;
        private Button   _btnRefresh;

        private const string NAME_TXT_TITLE     = "Txt_Title";
        private const string NAME_TXT_SUBTITLE  = "Txt_Subtitle";
        private const string NAME_TXT_EMPTY     = "Txt_Empty";
        private const string NAME_TXT_TASK_BODY = "Txt_TaskBody";
        private const string NAME_BTN_REFRESH   = "Btn_Refresh";

        private void Awake()
        {
            _txtTitle    = transform.GetComponentInDeepChild<TMP_Text>(NAME_TXT_TITLE);
            _txtSubtitle = transform.GetComponentInDeepChild<TMP_Text>(NAME_TXT_SUBTITLE);
            _txtEmpty    = transform.GetComponentInDeepChild<TMP_Text>(NAME_TXT_EMPTY);
            _txtTaskBody = transform.GetComponentInDeepChild<TMP_Text>(NAME_TXT_TASK_BODY);
            _btnRefresh  = transform.GetComponentInDeepChild<Button>(NAME_BTN_REFRESH);
        }

        private async void Start()
        {
            if (_txtTitle != null)
                _txtTitle.text = "Bugünkü Görevlerin";

            if (_txtSubtitle != null)
                _txtSubtitle.text = "Terapistinin senin için seçtiği oyunlar";

            if (_btnRefresh != null)
                _btnRefresh.onClick.AddListener(() => { _ = RefreshTasksAsync(); });

            await RefreshTasksAsync();
        }

        private async Task RefreshTasksAsync()
        {
            // 1) APIManager var mı?
            if (APIManager.Instance == null)
            {
                Debug.LogError("[Notification] APIManager.Instance = null! " +
                               "Bu sahneyi doğrudan çalıştırıyorsun. " +
                               "Oyunu LoginScene'den başlat veya sahneye APIManager ekle.");
                ShowNoTask("Sunucuya bağlanırken hata oluştu.");
                return;
            }

            // 2) PlayerId set edilmiş mi?
            if (GameContext.PlayerId <= 0)
            {
                Debug.LogWarning("[Notification] GameContext.PlayerId set edilmemiş.");
                ShowNoTask("Önce giriş yapmalısın.");
                return;
            }

            SetLoadingState(true);

            long playerId = GameContext.PlayerId;

            // TÜM GÖREVLERİ ÇEK
            List<PlayerTaskDto> tasks = await APIManager.Instance.GetAllTasksForPlayer(playerId);

            // Loading bitti
            SetLoadingState(false);

            if (tasks == null)
            {
                ShowNoTask("Sunucuya bağlanırken hata oluştu.");
                return;
            }

            if (tasks.Count == 0)
            {
                ShowNoTask("Henüz atanmış görevin yok.");
                return;
            }

            // İstersen sadece ASSIGNED olanları göster
            var sb = new StringBuilder();

            foreach (var t in tasks)
            {
                // Burada status filtresi istersen:
                // if (t.status != "ASSIGNED") continue;

                string letter   = string.IsNullOrEmpty(t.letterCode) ? "?"        : t.letterCode;
                string gameName = string.IsNullOrEmpty(t.gameName)  ? "bir oyun" : t.gameName;

                sb.AppendLine($"• Harf '{letter}' için {gameName} oynayacaksın.");

                if (!string.IsNullOrEmpty(t.note))
                    sb.AppendLine($"  Not: {t.note}");

                sb.AppendLine(); // araya boş satır
            }

            string body = sb.ToString().Trim();
            if (string.IsNullOrEmpty(body))
            {
                ShowNoTask("Henüz atanmış görevin yok.");
            }
            else
            {
                ShowTask(body);
            }
        }

        private void SetLoadingState(bool isLoading)
        {
            if (_txtEmpty == null || _txtTaskBody == null)
                return;

            if (isLoading)
            {
                _txtEmpty.text = "Görevler yükleniyor...";
                _txtEmpty.gameObject.SetActive(true);
                _txtTaskBody.gameObject.SetActive(false);
            }
            else
            {
                // Yükleme bittiyse, burada ekstra bir şey yapmıyoruz
                // asıl görünürlüğü ShowNoTask / ShowTask belirliyor
            }
        }

        private void ShowNoTask(string message)
        {
            if (_txtEmpty != null)
            {
                _txtEmpty.text = message;
                _txtEmpty.gameObject.SetActive(true);
            }

            if (_txtTaskBody != null)
                _txtTaskBody.gameObject.SetActive(false);
        }

        private void ShowTask(string body)
        {
            if (_txtTaskBody != null)
            {
                _txtTaskBody.text = body;
                _txtTaskBody.gameObject.SetActive(true);
            }

            if (_txtEmpty != null)
                _txtEmpty.gameObject.SetActive(false);
        }
    }
}

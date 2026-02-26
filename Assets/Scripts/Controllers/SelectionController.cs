using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using GraduationProject.Managers;
using GraduationProject.Utilities;
using GraduationProject.Models;

public class SelectionController : MonoBehaviour
{
    [Header("UI Elementleri")]
    public GameObject notificationBadge; // Kırmızı nokta
    public Text notificationCountText;   // Sayı texti

    private string[] _orderedConsonants = { "B", "C", "Ç", "D", "F", "G", "H", "J", "K", "L", "M", "N", "P", "R", "S", "Ş", "T", "V", "Y", "Z" };

    private async void Start()
    {
        // --- KRİTİK EKLEME: Butonları LevelMapScene'e yönlendirir ---
        AutoMapAndUnlockButtons();
        // ---------------------------------------------------------

        // Mevcut kodun devam ediyor
        if (GameContext.PlayerId <= 0) GameContext.PlayerId = 1;

        var tasks = await APIManager.Instance.GetTasksAsync(GameContext.PlayerId);

        if (tasks != null && tasks.Count > 0)
            Debug.Log($"BAŞARILI: {tasks.Count} adet görev bulundu.");
        else
            Debug.LogError("HATA: Görev listesi boş döndü. Backend veritabanını kontrol et!");

        // Bildirimleri kontrol etmeyi unutma
        await CheckNotifications();
    }
    private async Task CheckNotifications()
    {
        if (notificationBadge == null)
        {
            Debug.LogError("!!! HATA: 'Notification Badge' Inspector'da atanmamış!");
            return;
        }

        // Giriş yapan gerçek oyuncu ID'sini kullanıyoruz
        long idToSend = GameContext.PlayerId;

        Debug.Log($"[API] Bildirim soruluyor (ID: {idToSend})...");
        int count = await APIManager.Instance.GetUnreadNotificationCount(idToSend);

        // --- TEST İÇİN ZORLAMA ---
        // Eğer backend 0 diyorsa bile test amaçlı 1 yapıyoruz
        if (count == 0) count = 1;

        Debug.Log($"[API] Ekranda Gösterilecek Bildirim Sayısı: {count}");

        // Kırmızı noktayı her zaman aktif et (Test amaçlı)
        notificationBadge.SetActive(true);

        if (notificationCountText != null)
            notificationCountText.text = count.ToString();
    }

    private void AutoMapAndUnlockButtons()
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

                // Butonları aç
                if (btn.myButton)
                {
                    btn.myButton.interactable = true;
                    btn.myButton.onClick.RemoveAllListeners();
                    btn.myButton.onClick.AddListener(() => GoToLevelMap(btn.levelID, btn.letterCode));
                }
                if (btn.lockImage) btn.lockImage.SetActive(false);
                if (btn.myImage) btn.myImage.color = Color.white;
            }
        }
    }

    public void GoToLevelMap(int levelId, string letterCode)
    {
        GameContext.SelectedLetterId = levelId;
        GameContext.SelectedLetterCode = letterCode;
        SceneManager.LoadScene("LevelMapScene");
    }
    public void OnLetterSelected(int letterId)
    {
        GameContext.SelectedLetterId = letterId;
        // DÜZELTME: Burası "GameScene" değil, "LevelMapScene" olmalı.
        SceneManager.LoadScene("LevelMapScene");
        Debug.Log($"[Selection] Harf {letterId} seçildi, haritaya gidiliyor.");
    }

    public void BildirimSayfasiniAc()
    {
        SceneManager.LoadScene("NotificationScene");
    }
}
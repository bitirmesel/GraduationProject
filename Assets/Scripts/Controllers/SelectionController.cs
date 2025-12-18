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
        Debug.Log($"[DEBUG] Selection Başladı. Gelen PlayerID: {GameContext.PlayerId}");

        // 1. Önce butonları aç
        AutoMapAndUnlockButtons();

        // 2. Bildirim Kontrolü
        if (GameContext.PlayerId > 0)
        {
            // Normal durum: Giriş yapılmış
            await CheckNotifications();
        }
        else
        {
            // --- BURAYI DEĞİŞTİRDİK ---
            // HATA VARSA BİLE ÇALIŞTIR:
            Debug.LogWarning("!!! ID 0 geldi ama test için ID: 1 kullanılarak devam ediliyor.");

            // Geçici olarak ID'yi 1 yapıyoruz ki sistem çalışsın
            GameContext.PlayerId = 1;

            await CheckNotifications();
        }
    }

    private async Task CheckNotifications()
    {
        if (notificationBadge == null)
        {
            Debug.LogError("!!! HATA: 'Notification Badge' Inspector'da atanmamış! Sürükleyip bırak.");
            return;
        }

        // Eğer ID 0 ise test amaçlı 1 gönderelim, yoksa gerçek ID'yi kullanalım
        long idToSend = (GameContext.PlayerId > 0) ? GameContext.PlayerId : 1;

        Debug.Log($"[API] Bildirim soruluyor (ID: {idToSend})...");
        int count = await APIManager.Instance.GetUnreadNotificationCount(idToSend);

        Debug.Log($"[API] Gelen Bildirim Sayısı: {count}");

        if (count > 0)
        {
            notificationBadge.SetActive(true);
            if (notificationCountText != null) notificationCountText.text = count.ToString();
        }
        else
        {
            notificationBadge.SetActive(false);
        }
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

    public void BildirimSayfasiniAc()
    {
        SceneManager.LoadScene("NotificationScene");
    }
}
using UnityEngine;
using UnityEngine.SceneManagement;

using GraduationProject.Managers;
using GraduationProject.Models;
using GraduationProject.Controllers;

public class SceneNavigator : MonoBehaviour
{

    public static SceneNavigator Instance; // Diğer scriptlerden ulaşmak için anahtar

    private void Awake()
    {
        // Eğer daha önce yaratılmış bir Navigator varsa, kendini yok et (Çakışmayı önle)
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // İlk kez yaratılıyorsa, "Ben patronum" de ve sahneler arası geçişte yok olma.
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void GoToLogin()
    {
        Debug.Log("Giriş Ekranına Dönülüyor...");
        SceneManager.LoadScene("LoginScene");
    }

    public void GoToSelection()
    {
        Debug.Log("Seviye Seçim Ekranına Gidiliyor...");
        SceneManager.LoadScene("SelectionScene");
    }

    public void GoToGame()
    {
        // Buraya istersen "Hangi level seçili?" kontrolü koyabilirsin
        Debug.Log("Oyun Sahnesi Açılıyor...");
        SceneManager.LoadScene("GameScene");
    }

    public void GoToNotification()
    {
        Debug.Log("Bildirimler Açılıyor...");
        SceneManager.LoadScene("NotificationScene");
    }

    public void GoToLevelMap()
    {
        // Hata veren satırı namespace ekledikten sonra böyle kullanabilirsin
        GameContext.IsFocusMode = false;

        Debug.Log("Seçim yapıldı, haritaya gidiliyor...");
        UnityEngine.SceneManagement.SceneManager.LoadScene("LevelMapScene");
    }

    // --- GENEL FONKSİYON (Kod İçinden Çağırmak İçin) ---
    // Başka bir scriptin içinden sahne değiştirmek istersen bunu kullanırsın.
    // Örnek: SceneNavigator.Instance.ChangeScene("GameScene");
    public void ChangeScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    public void BackToMapFromGame()
{
    // Odak modunu kapatıyoruz
    GameContext.IsFocusMode = false;
    
    Debug.Log("Oyundan haritaya dönülüyor...");
    UnityEngine.SceneManagement.SceneManager.LoadScene("LevelMapScene");
}

    public void ReturnToMap()
    {
        // Odak modunu temizle ki haritaya dönünce tekrar takılı kalmasın
        GameContext.IsFocusMode = false;

        Debug.Log("Haritaya dönülüyor...");
        SceneManager.LoadScene("LevelMapScene");
    }
}
using UnityEngine;
using UnityEngine.SceneManagement;

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
         Debug.Log("Harita Yükleniyor...");
         SceneManager.LoadScene("LevelMapScene");
    }

    // --- GENEL FONKSİYON (Kod İçinden Çağırmak İçin) ---
    // Başka bir scriptin içinden sahne değiştirmek istersen bunu kullanırsın.
    // Örnek: SceneNavigator.Instance.ChangeScene("GameScene");
    public void ChangeScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}
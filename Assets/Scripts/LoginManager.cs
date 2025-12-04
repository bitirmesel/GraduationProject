using UnityEngine;
using TMPro; // TextMeshPro kullanmak için kütüphane
using UnityEngine.UI; // UI işlemleri için

public class LoginManager : MonoBehaviour
{
    // Editörden sürükleyip bırakacağımız kutucuklar
    public TMP_InputField usernameField;
    public TMP_InputField passwordField;

    // Butona basılınca çalışacak fonksiyon
    public void GirisYap()
    {
        string kAdi = usernameField.text;
        string sifre = passwordField.text;

        // Şimdilik sadece konsola yazdıralım (Backend yokken test için)
        Debug.Log("Giriş Denemesi Yapıldı!");
        Debug.Log("Kullanıcı: " + kAdi);
        Debug.Log("Şifre: " + sifre);

        // İleride buraya Backend bağlantı kodlarını yazacağız.
    }
}
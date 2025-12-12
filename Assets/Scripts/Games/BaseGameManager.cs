using UnityEngine;
using System.Threading.Tasks;

public abstract class BaseGameManager : MonoBehaviour
{
    // Her oyunun kendi "Başlatma" komutu olacak
    // letterId: Hangi harfin içeriği yüklenecek?
    public abstract Task InitializeGame(long letterId);
    
    // Her oyunun bittiğinde çağıracağı ortak fonksiyon
    protected void GameCompleted()
    {
        Debug.Log("Oyun Tamamlandı! Base Manager sinyali aldı.");
        // İleride buraya "Level End Panel" açma kodu gelecek
    }
}
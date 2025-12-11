using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LevelIdentifier : MonoBehaviour
{
    [Header("Bu Levelin Kimliği")]
    public int levelID;      // Veritabanındaki Task ID (1, 2, 3...)
    public string harfKodu;  // Hangi harf? (B, C, Ç...)

    [Header("Otomatik Bağlantılar")]
    public Button myButton;
    public Image myImage;
    public GameObject lockImage;
    public TMP_Text letterText;

    // Editörde kolaylık olsun diye, değer değişince text'i güncelle
    private void OnValidate()
    {
        if (letterText != null) letterText.text = harfKodu;
        gameObject.name = "Level_" + harfKodu; // Obje ismini de düzeltir
    }
}
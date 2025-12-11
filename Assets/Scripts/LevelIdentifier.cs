using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LevelIdentifier : MonoBehaviour
{
    [Header("Otomatik Atanacak (Elle Dokunma)")]
    public int levelID;       // SelectionController tarafından doldurulacak
    public string letterCode; // SelectionController tarafından doldurulacak

    [Header("Otomatik Bağlantılar")]
    public Button myButton;
    public Image myImage;
    public GameObject lockImage;
    public TextMeshProUGUI letterText;

    // Editörde componentleri otomatik bulmak için kolaylık (Opsiyonel)
    private void OnValidate()
    {
        if (myButton == null) myButton = GetComponent<Button>();
        if (myImage == null) myImage = GetComponent<Image>();
        // Text ve Lock objeleri genelde child olduğu için elle atamak daha sağlıklı olabilir
    }
}
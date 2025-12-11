using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using GraduationProject.Utilities;

public class LevelIdentifier : MonoBehaviour
{
    [Header("Harf Bilgisi")]
    public int levelID;           // Biz bunu LetterId olarak kullanacağız
    public string letterCode;     // "B", "C", "Ç"...

    [Header("UI Referansları")]
    public Button myButton;
    public Image myImage;
    public GameObject lockImage;
    public TMP_Text letterText;

    private void Awake()
    {
        if (myButton == null) myButton = GetComponent<Button>();
        if (myImage == null) myImage = GetComponent<Image>();
    }

    private void Start()
    {
        if (letterText != null && !string.IsNullOrEmpty(letterCode))
            letterText.text = letterCode;

        if (myButton != null)
            myButton.onClick.AddListener(OnClicked);
    }

    private void OnClicked()
    {
        // BU KISIM TÜM AKIŞIN BAŞLANGICI
        GameContext.SelectedLetterId = levelID;
        GameContext.SelectedLetterCode = letterCode;

        Debug.Log($"[Selection] Seçilen harf: {letterCode} (Id={levelID})");

        // LevelMap scene'e geç
        SceneManager.LoadScene(GameConstants.SCENE_MAP);
    }
}

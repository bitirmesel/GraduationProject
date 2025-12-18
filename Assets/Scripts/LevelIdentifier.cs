using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using GraduationProject.Utilities;
using GraduationProject.Models;

public class LevelIdentifier : MonoBehaviour
{
    [Header("Harf Bilgisi")]
    public int levelID;           // SelectedLetterId
    public string letterCode;     // "K", "T" ...

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

    private void OnEnable()
    {
        if (myButton != null)
        {
            myButton.onClick.RemoveListener(OnClicked);
            myButton.onClick.AddListener(OnClicked);
        }
    }

    private void Start()
    {
        if (letterText != null && !string.IsNullOrEmpty(letterCode))
            letterText.text = letterCode;
    }

    private void OnClicked()
{
    GameContext.SelectedLetterId = levelID;
    GameContext.SelectedLetterCode = letterCode;

    GameContext.SelectedGameId = 4;        // ✅ ekle
    GameContext.SelectedGameType = "Memory";
    GameContext.SelectedDifficulty = 1;

    Debug.Log($"[Selection] Letter={letterCode} Id={levelID} => Memory diff=1");
    SceneManager.LoadScene(GameConstants.SCENE_GAME);
}


}

using GraduationProject.Utilities;
using UnityEngine;
using UnityEngine.UI; // Button bileşeni için gerekli
using GraduationProject.Models;

public class GameSceneController : MonoBehaviour
{
    public BaseGameManager CurrentGameManager;

    private async void Start()
    {
        // --- BACK BUTONU GARANTİLEME ---
        GameObject backBtnObj = GameObject.Find("BackButton");
        if (backBtnObj != null)
        {
            Button btn = backBtnObj.GetComponent<Button>();
            btn.onClick.RemoveAllListeners(); // Eski hatalı bağlantıları sil
            // DontDestroyOnLoad ile gelen navigator instance'ına bağla
            btn.onClick.AddListener(() => SceneNavigator.Instance.GoToSelection());
        }
        // -------------------------------

        if (CurrentGameManager == null)
        {
            Debug.LogError("CurrentGameManager atanmamış!");
            return;
        }

        if (GameContext.SelectedLetterId <= 0)
        {
            GameContext.SelectedLetterId = 9;
            GameContext.SelectedGameType = "Memory";
            GameContext.SelectedDifficulty = 1;
        }

        await CurrentGameManager.InitializeGame(GameContext.SelectedLetterId);
    }
}
using GraduationProject.Utilities;
using UnityEngine;

public class GameSceneController : MonoBehaviour
{
    public BaseGameManager CurrentGameManager;

    private async void Start()
{
    if (CurrentGameManager == null)
    {
        Debug.LogError("CurrentGameManager atanmamış!");
        return;
    }

    // Seçim yoksa fallback
    if (GameContext.SelectedLetterId <= 0)
    {
        Debug.LogWarning("SelectedLetterId yok, fallback=K(9)!");
        GameContext.SelectedLetterId = 9;
        GameContext.SelectedGameType = "Memory";
        GameContext.SelectedDifficulty = 1;
    }

    await CurrentGameManager.InitializeGame(GameContext.SelectedLetterId);
}

}

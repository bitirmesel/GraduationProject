using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using GraduationProject.Managers;
using GraduationProject.Utilities;

namespace GraduationProject.Controllers
{
    public class LoginController : MonoBehaviour
    {
        // Inspector'da görünmez, kod tarafından doldurulur.
        private TMP_InputField _usernameInput;
        private TMP_InputField _passwordInput;
        private Button _loginButton;
        private TMP_Text _feedbackText;

        // UI Obje İsimleri (Sözleşme)
        private const string NAME_INPUT_USER = "Input_Username";
        private const string NAME_INPUT_PASS = "Input_Password";
        private const string NAME_BTN_LOGIN = "Btn_Login";
        private const string NAME_TXT_FEEDBACK = "Txt_Feedback";

        private void Awake()
        {
            // LoginPanel_Prefab içindeki child objeleri isimle buluyoruz
            _usernameInput = transform.GetComponentInDeepChild<TMP_InputField>(NAME_INPUT_USER);
            _passwordInput = transform.GetComponentInDeepChild<TMP_InputField>(NAME_INPUT_PASS);
            _loginButton   = transform.GetComponentInDeepChild<Button>(NAME_BTN_LOGIN);
            _feedbackText  = transform.GetComponentInDeepChild<TMP_Text>(NAME_TXT_FEEDBACK);
        }

        private void Start()
        {
            if (_loginButton != null)
                _loginButton.onClick.AddListener(OnLoginClicked);

            ResetUI();
        }

        private void OnDestroy()
        {
            if (_loginButton != null)
                _loginButton.onClick.RemoveListener(OnLoginClicked);
        }

        private async void OnLoginClicked()
        {
            if (_usernameInput == null || _passwordInput == null)
            {
                Debug.LogError("[LoginController] Input referansları bulunamadı.");
                ShowFeedback("Bir hata oluştu. Lütfen geliştiriciye haber ver.", Color.red);
                return;
            }

            if (APIManager.Instance == null)
            {
                Debug.LogError("[LoginController] APIManager.Instance = null");
                ShowFeedback("Sunucuya bağlanırken hata oluştu.", Color.red);
                return;
            }

            string nickname = _usernameInput.text.Trim();
            string password = _passwordInput.text.Trim();

            if (string.IsNullOrEmpty(nickname) || string.IsNullOrEmpty(password))
            {
                ShowFeedback("Lütfen kullanıcı adı ve şifreyi gir.", Color.red);
                return;
            }

            ToggleInteractable(false);
            ShowFeedback("Bağlanılıyor...", Color.yellow);

            try
            {
                // PLAYER LOGIN – backend: /api/player/auth/login
                var player = await APIManager.Instance.PlayerLoginAsync(nickname, password);

                if (player == null)
                {
                    ShowFeedback("Giriş başarısız. Bilgilerini kontrol et.", Color.red);
                    ToggleInteractable(true);
                    return;
                }

                // Oturum bilgisini global context'e yaz
                GameContext.PlayerId   = player.PlayerId;
                GameContext.Nickname   = player.Nickname;
                GameContext.TotalScore = player.TotalScore;

                ShowFeedback($"Hoş geldin {player.Nickname}!", Color.green);

                // Küçük bir bekleme – çocuk ekranda yazıyı görsün
                await System.Threading.Tasks.Task.Delay(800);

                // Görev/harita sahnesine geç
                SceneManager.LoadScene(GameConstants.SCENE_MAP);
            }
            catch (System.SystemException ex)
            {
                Debug.LogError("[LoginController] Login sırasında hata: " + ex);
                ShowFeedback("Beklenmeyen bir hata oluştu.", Color.red);
                ToggleInteractable(true);
            }
        }

        private void ShowFeedback(string message, Color color)
        {
            if (_feedbackText != null)
            {
                _feedbackText.text = message;
                _feedbackText.color = color;
            }
        }

        private void ToggleInteractable(bool state)
        {
            if (_usernameInput != null) _usernameInput.interactable = state;
            if (_passwordInput != null) _passwordInput.interactable = state;
            if (_loginButton   != null) _loginButton.interactable = state;
        }

        private void ResetUI()
        {
            if (_feedbackText  != null) _feedbackText.text = "";
            if (_usernameInput != null) _usernameInput.text = "";
            if (_passwordInput != null) _passwordInput.text = "";
        }
    }
}

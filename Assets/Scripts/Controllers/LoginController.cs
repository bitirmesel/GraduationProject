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
            // OYUN BAŞLARKEN OTOMATİK BAĞLAMA (Dedektif Çalışıyor)
            _usernameInput = transform.GetComponentInDeepChild<TMP_InputField>(NAME_INPUT_USER);
            _passwordInput = transform.GetComponentInDeepChild<TMP_InputField>(NAME_INPUT_PASS);
            _loginButton = transform.GetComponentInDeepChild<Button>(NAME_BTN_LOGIN);
            _feedbackText = transform.GetComponentInDeepChild<TMP_Text>(NAME_TXT_FEEDBACK);
        }

        private void Start()
        {
            if (_loginButton != null)
                _loginButton.onClick.AddListener(OnLoginClicked);
            
            ResetUI();
        }

        private async void OnLoginClicked()
        {
            string username = _usernameInput.text.Trim();
            string password = _passwordInput.text.Trim();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ShowFeedback("Lütfen bilgileri giriniz.", Color.red);
                return;
            }

            ToggleInteractable(false);
            ShowFeedback("Bağlanılıyor...", Color.yellow);

            // API Çağrısı
            var user = await APIManager.Instance.Login(username, password);

            if (user != null)
            {
                ShowFeedback($"Hoşgeldin {user.Username}!", Color.green);
                await System.Threading.Tasks.Task.Delay(1000);
                SceneManager.LoadScene(GameConstants.SCENE_MAP);
            }
            else
            {
                ShowFeedback("Giriş Başarısız!", Color.red);
                ToggleInteractable(true);
            }
        }

        private void ShowFeedback(string message, Color color)
        {
            if (_feedbackText) { _feedbackText.text = message; _feedbackText.color = color; }
        }

        private void ToggleInteractable(bool state)
        {
            if (_usernameInput) _usernameInput.interactable = state;
            if (_passwordInput) _passwordInput.interactable = state;
            if (_loginButton) _loginButton.interactable = state;
        }

        private void ResetUI()
        {
            if (_feedbackText) _feedbackText.text = "";
            if (_usernameInput) _usernameInput.text = "";
            if (_passwordInput) _passwordInput.text = "";
        }
    }
}
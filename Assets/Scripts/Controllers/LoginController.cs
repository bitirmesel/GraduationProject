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
            _loginButton = transform.GetComponentInDeepChild<Button>(NAME_BTN_LOGIN);
            _feedbackText = transform.GetComponentInDeepChild<TMP_Text>(NAME_TXT_FEEDBACK);
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
            // 1. Input Kontrolleri
            if (_usernameInput == null || _passwordInput == null)
            {
                Debug.LogError("[LoginController] Input referansları eksik.");
                return;
            }

            if (APIManager.Instance == null)
            {
                ShowFeedback("Sunucu bağlantısı hatası.", Color.red);
                return;
            }

            string nickname = _usernameInput.text.Trim();
            string password = _passwordInput.text.Trim();

            if (string.IsNullOrEmpty(nickname) || string.IsNullOrEmpty(password))
            {
                ShowFeedback("Kullanıcı adı ve şifre gerekli.", Color.red);
                return;
            }

            // 2. Giriş İşlemi Başlıyor
            ToggleInteractable(false);
            ShowFeedback("Giriş yapılıyor...", Color.yellow);

            try
            {
                // 1. API'den Giriş Yap
                var player = await APIManager.Instance.PlayerLoginAsync(nickname, password);

                if (player == null)
                {
                    ShowFeedback("Hatalı kullanıcı adı veya şifre.", Color.red);
                    ToggleInteractable(true);
                    return;
                }

                // 2. Başarılı! Verileri Kaydet
                GameContext.PlayerId = player.PlayerId;
                ShowFeedback($"Hoş geldin {player.Nickname}!", Color.green);

                await System.Threading.Tasks.Task.Delay(500);

                // 3. HİÇ SORMADAN DİREKT HARİTAYA GİT
                // Görev kontrolünü burada yapmıyoruz, Selection ekranı kendi halledecek.
                UnityEngine.SceneManagement.SceneManager.LoadScene("SelectionScene");
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[LoginController] Hata: " + ex.Message);
                ShowFeedback("Hata oluştu.", Color.red);
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
            if (_loginButton != null) _loginButton.interactable = state;
        }

        private void ResetUI()
        {
            if (_feedbackText != null) _feedbackText.text = "";
            if (_usernameInput != null) _usernameInput.text = "";
            if (_passwordInput != null) _passwordInput.text = "";
        }
    }
}

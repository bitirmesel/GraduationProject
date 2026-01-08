using UnityEngine;
using UnityEngine.EventSystems;
using GraduationProject.Managers;

namespace GraduationProject.Controllers
{
    public class MicHoldHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [Header("Görsel Efekt")]
        [SerializeField] private Vector3 pressedScale = new Vector3(1.2f, 1.2f, 1.2f);
        private Vector3 _originalScale;

        private void Start()
        {
            _originalScale = transform.localScale;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            // 1. Başlangıç Sesi (Click 1)
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayEffect("Click1"); // AudioManager'daki tam adı yazın

            // 2. Görsel Geri Bildirim
            transform.localScale = pressedScale;

            // 3. Kaydı Başlat
            Debug.Log("[MicHoldHandler] PointerDown");
            if (PronunciationManager.Instance != null)
                PronunciationManager.Instance.StartRecording();
            else
                Debug.LogError("[MicHoldHandler] PronunciationManager.Instance NULL");
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            // 1. Bitiş Sesi (Click 2)
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayEffect("Click2");

            // 2. Boyutu Normale Döndür
            transform.localScale = _originalScale;

            // 3. Kaydı Durdur
            Debug.Log("[MicHoldHandler] PointerUp");
            if (PronunciationManager.Instance != null)
                PronunciationManager.Instance.StopRecording();
            
        }
    }
}
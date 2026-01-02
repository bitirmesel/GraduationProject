using UnityEngine;
using UnityEngine.EventSystems;
using GraduationProject.Managers;

namespace GraduationProject.Controllers
{
    public class MicHoldHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public void OnPointerDown(PointerEventData eventData)
        {
            Debug.Log("[MicHoldHandler] PointerDown");
            if (PronunciationManager.Instance != null)
                PronunciationManager.Instance.StartRecording();
            else
                Debug.LogError("[MicHoldHandler] PronunciationManager.Instance NULL");
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            Debug.Log("[MicHoldHandler] PointerUp");
            if (PronunciationManager.Instance != null)
                PronunciationManager.Instance.StopRecording();
        }
    }
}

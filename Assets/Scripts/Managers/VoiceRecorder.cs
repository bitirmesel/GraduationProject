using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using GraduationProject.Models;

namespace GraduationProject.Managers
{
    public class VoiceRecorder : MonoBehaviour
    {
        public static VoiceRecorder Instance;
        
        private AudioClip _recordingClip;
        private string _microphoneDevice;
        private bool _isRecording = false;

        private void Awake()
        {
            Instance = this;
            if (Microphone.devices.Length > 0)
                _microphoneDevice = Microphone.devices[0];
        }

        public void StartRecording()
        {
            if (_isRecording) return;
            
            // 10 saniyelik bir kayıt alanı açıyoruz
            _recordingClip = Microphone.Start(_microphoneDevice, false, 10, 44100);
            _isRecording = true;
            Debug.Log("Kayıt başladı...");
        }

        public async void StopRecording()
        {
            if (!_isRecording) return;

            Microphone.End(_microphoneDevice);
            _isRecording = false;
            Debug.Log("Kayıt durduruldu. API'ye gönderiliyor...");

            // Kaydı byte dizisine çevirip APIManager'a gönderiyoruz
            byte[] audioData = WavUtility.FromAudioClip(_recordingClip);
            var result = await APIManager.Instance.CheckPronunciationAsync(audioData);
            
            // 5/6 Kuralını burada işletiyoruz
            MemoryGameManager.Instance.HandlePronunciationResult(result);
        }
    }
}
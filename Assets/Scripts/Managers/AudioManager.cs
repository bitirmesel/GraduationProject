using UnityEngine;

namespace GraduationProject.Managers
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance;

        private void Awake()
        {
            if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
            else { Destroy(gameObject); }
        }

        public void PlayEffect(string clipName) { /* Ses efekti çalma mantığı */ }

        // AudioManager.cs içine ekle
        public void PlayVoiceOver(string clipName)
        {
            // Resources altındaki ses dosyasını yükleyip çalma mantığı
            AudioClip clip = Resources.Load<AudioClip>("Audio/" + clipName);
            if (clip != null)
            {
                // AudioSource bileşeni üzerinden sesi çal
                GetComponent<AudioSource>().PlayOneShot(clip);
            }
            else
            {
                Debug.LogWarning("Ses dosyası bulunamadı: " + clipName);
            }
        }

        public async System.Threading.Tasks.Task PlayVoiceOverAsync(string clipName)
        {
            AudioClip clip = Resources.Load<AudioClip>("Audio/" + clipName);
            if (clip != null)
            {
                AudioSource source = GetComponent<AudioSource>();
                source.clip = clip;
                source.Play();
                // Ses bitene kadar bekle
                while (source.isPlaying) await System.Threading.Tasks.Task.Yield();
            }
        }
    }
}
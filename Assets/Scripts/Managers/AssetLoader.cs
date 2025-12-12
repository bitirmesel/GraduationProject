using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace GraduationProject.Managers
{
    public class AssetLoader : MonoBehaviour
    {
        // Singleton Yapısı
        public static AssetLoader Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject); // Sahne değişse de yok olmasın
            }
            else
            {
                Destroy(gameObject);
            }
        }

        // RAM Önbellekleri
        private Dictionary<string, Sprite> _spriteCache = new Dictionary<string, Sprite>();
        private Dictionary<string, AudioClip> _audioCache = new Dictionary<string, AudioClip>();

        // -------------------------------------------------------------------------
        // 1. SPRITE (RESİM) YÜKLEME
        // -------------------------------------------------------------------------
        public async Task<Sprite> GetSpriteAsync(string remoteUrl, string fileName)
        {
            if (string.IsNullOrEmpty(remoteUrl)) return null;

            // ADIM 1: RAM KONTROLÜ (Zombie Object Korumalı)
            if (_spriteCache.ContainsKey(remoteUrl))
            {
                // Unity objesi hala yaşıyor mu? (Destroy edilmemiş mi?)
                if (_spriteCache[remoteUrl] != null)
                {
                    return _spriteCache[remoteUrl];
                }
                else
                {
                    // Eğer obje yok edilmişse, listeden anahtarı sil ve yeniden yükle
                    _spriteCache.Remove(remoteUrl);
                }
            }

            // Dosya yolunu belirle
            string savePath = Path.Combine(Application.persistentDataPath, fileName);

            // ADIM 2: DİSK KONTROLÜ
            if (File.Exists(savePath))
            {
                byte[] fileData = File.ReadAllBytes(savePath);
                Texture2D texture = new Texture2D(2, 2);

                if (texture.LoadImage(fileData))
                {
                    Sprite diskSprite = CreateSpriteFromTexture(texture);
                    diskSprite.name = fileName;
                    
                    _spriteCache[remoteUrl] = diskSprite; // RAM'e geri yükle
                    return diskSprite;
                }
                else
                {
                    // Dosya bozuksa sil
                    File.Delete(savePath);
                }
            }

            // ADIM 3: İNTERNETTEN İNDİRME
            using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(remoteUrl))
            {
                var operation = request.SendWebRequest();
                while (!operation.isDone) await Task.Yield();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Texture2D texture = DownloadHandlerTexture.GetContent(request);
                    
                    // Diske Kaydet
                    byte[] bytes = texture.EncodeToPNG();
                    File.WriteAllBytes(savePath, bytes);

                    // Sprite Oluştur
                    Sprite newSprite = CreateSpriteFromTexture(texture);
                    newSprite.name = fileName;

                    _spriteCache[remoteUrl] = newSprite; // RAM'e ekle
                    return newSprite;
                }
                
                Debug.LogError($"[AssetLoader] Resim İndirme Hatası: {request.error} URL: {remoteUrl}");
                return null;
            }
        }

        // -------------------------------------------------------------------------
        // 2. AUDIO (SES) YÜKLEME
        // -------------------------------------------------------------------------
        public async Task<AudioClip> GetAudioAsync(string remoteUrl, string fileName)
        {
            if (string.IsNullOrEmpty(remoteUrl)) return null;

            // RAM Kontrolü (Zombie Object Korumalı)
            if (_audioCache.ContainsKey(remoteUrl))
            {
                if (_audioCache[remoteUrl] != null) return _audioCache[remoteUrl];
                _audioCache.Remove(remoteUrl);
            }

            string savePath = Path.Combine(Application.persistentDataPath, fileName);
            AudioType audioType = GetAudioTypeFromUrl(remoteUrl);

            // Diskten Yükleme
            if (File.Exists(savePath))
            {
                using (UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip("file://" + savePath, audioType))
                {
                    var operation = request.SendWebRequest();
                    while (!operation.isDone) await Task.Yield();

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
                        clip.name = fileName;
                        _audioCache[remoteUrl] = clip;
                        return clip;
                    }
                }
            }

            // İnternetten İndirme
            using (UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(remoteUrl, audioType))
            {
                var operation = request.SendWebRequest();
                while (!operation.isDone) await Task.Yield();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
                    
                    // Diske Kaydet
                    byte[] bytes = request.downloadHandler.data;
                    File.WriteAllBytes(savePath, bytes);

                    clip.name = fileName;
                    _audioCache[remoteUrl] = clip;
                    return clip;
                }

                Debug.LogError($"[AssetLoader] Ses İndirme Hatası: {request.error} URL: {remoteUrl}");
                return null;
            }
        }

        // -------------------------------------------------------------------------
        // YARDIMCI METODLAR
        // -------------------------------------------------------------------------
        
        public async void LoadImageIntoUI(string remoteUrl, string fileName, Image targetImage)
        {
            if (targetImage == null) return;
            Sprite sprite = await GetSpriteAsync(remoteUrl, fileName);
            if (sprite != null && targetImage != null) // targetImage yok olmuş olabilir
            {
                targetImage.sprite = sprite;
                targetImage.preserveAspect = true;
            }
        }

        private Sprite CreateSpriteFromTexture(Texture2D texture)
        {
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        }

        private AudioType GetAudioTypeFromUrl(string url)
        {
            if (url.EndsWith(".mp3")) return AudioType.MPEG;
            if (url.EndsWith(".wav")) return AudioType.WAV;
            if (url.EndsWith(".ogg")) return AudioType.OGGVORBIS;
            return AudioType.UNKNOWN;
        }

        public void ClearCache()
        {
            _spriteCache.Clear();
            _audioCache.Clear();
        }
    }
}
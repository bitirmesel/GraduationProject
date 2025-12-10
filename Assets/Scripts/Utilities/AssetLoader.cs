using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace GraduationProject.Utilities
{
    public static class AssetLoader
    {
        private static Dictionary<string, Sprite> _spriteCache = new Dictionary<string, Sprite>();
        private static Dictionary<string, AudioClip> _audioCache = new Dictionary<string, AudioClip>();

        public static async Task<Sprite> LoadSpriteAsync(string url)
        {
            if (string.IsNullOrEmpty(url)) return null;
            if (_spriteCache.ContainsKey(url)) return _spriteCache[url];

            using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
            {
                var operation = request.SendWebRequest();
                while (!operation.isDone) await Task.Yield();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Texture2D texture = DownloadHandlerTexture.GetContent(request);
                    Sprite newSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                    newSprite.name = "WebSprite_" + url.GetHashCode();
                    _spriteCache[url] = newSprite;
                    return newSprite;
                }
                Debug.LogError($"Resim Hatası: {request.error} URL: {url}");
                return null;
            }
        }

        public static async Task<AudioClip> LoadAudioAsync(string url)
        {
            if (string.IsNullOrEmpty(url)) return null;
            if (_audioCache.ContainsKey(url)) return _audioCache[url];

            using (UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.UNKNOWN))
            {
                var operation = request.SendWebRequest();
                while (!operation.isDone) await Task.Yield();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
                    clip.name = "WebAudio_" + url.GetHashCode();
                    _audioCache[url] = clip;
                    return clip;
                }
                Debug.LogError($"Ses Hatası: {request.error} URL: {url}");
                return null;
            }
        }
        
        public static void ClearCache() { _spriteCache.Clear(); _audioCache.Clear(); }
    }
}
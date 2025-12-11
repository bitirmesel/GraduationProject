using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace GraduationProject.Managers
{
    public class AssetLoader : MonoBehaviour
    {
        public static AssetLoader Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
            else { Destroy(gameObject); }
        }

        /// <summary>
        /// Resmi indirir (veya cache'den alır) ve belirtilen UI Image nesnesine basar.
        /// </summary>
        public async void LoadImageIntoUI(string remoteUrl, string fileName, Image targetImage)
        {
            if (targetImage == null) return;

            Sprite sprite = await GetSpriteAsync(remoteUrl, fileName);
            if (sprite != null)
            {
                targetImage.sprite = sprite;
                targetImage.preserveAspect = true; // Resim oranını koru
            }
        }

        /// <summary>
        /// Resmi indirir (veya cache'den alır) ve Sprite olarak döner.
        /// </summary>
        public async Task<Sprite> GetSpriteAsync(string remoteUrl, string fileName)
        {
            // 1. Önbellek (Cache) yolunu belirle
            string savePath = Path.Combine(Application.persistentDataPath, fileName);

            Texture2D texture = null;

            // 2. KONTROL: Dosya diskte var mı?
            if (File.Exists(savePath))
            {
                // VARSA: Diskten oku
                // Debug.Log($"[AssetLoader] Cache'den yükleniyor: {fileName}");
                byte[] fileData = File.ReadAllBytes(savePath);
                
                texture = new Texture2D(2, 2);
                if (!texture.LoadImage(fileData)) // LoadImage otomatik boyutlandırır
                {
                    Debug.LogError("[AssetLoader] Cache dosyası bozuk, siliniyor: " + fileName);
                    File.Delete(savePath);
                    texture = null;
                }
            }

            // 3. YOKSA (veya bozuksa): İnternetten İndir
            if (texture == null)
            {
                // Debug.Log($"[AssetLoader] İndiriliyor: {remoteUrl}");
                texture = await DownloadTextureAsync(remoteUrl);

                if (texture != null)
                {
                    // 4. KAYDET: Gelecekte kullanmak için diske yaz
                    byte[] bytes = texture.EncodeToPNG();
                    File.WriteAllBytes(savePath, bytes);
                    // Debug.Log($"[AssetLoader] Diske kaydedildi: {savePath}");
                }
            }

            if (texture != null)
            {
                // Texture'ı Sprite'a çevir
                return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            }

            return null;
        }

        // Texture İndirme Yardımcısı
        private async Task<Texture2D> DownloadTextureAsync(string url)
        {
            using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
            {
                var operation = request.SendWebRequest();
                while (!operation.isDone) await Task.Yield();

#if UNITY_2020_1_OR_NEWER
                if (request.result == UnityWebRequest.Result.Success)
#else
                if (!request.isNetworkError && !request.isHttpError)
#endif
                {
                    return DownloadHandlerTexture.GetContent(request);
                }
                else
                {
                    Debug.LogError($"[AssetLoader Error] {request.responseCode} - {request.error}");
                    return null;
                }
            }
        }
    }
}
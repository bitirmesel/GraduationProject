using UnityEngine;
using UnityEditor;
using UnityEngine.UI; // Image bileşeni için gerekli

// Bu script sadece Container_Levels (RectTransform) objesi seçiliyken çalışır.
[CustomEditor(typeof(RectTransform))] 
public class LevelPathEditor : Editor
{
    // Hangi isimde obje aradığımızı belirleyelim
    private const string TARGET_NAME = "Container_Levels";

    public override void OnInspectorGUI()
    {
        // Standart inspector'ı çiz (Position, Rotation vb. görünsün)
        DrawDefaultInspector();

        // Seçili obje bizim aradığımız container mı?
        RectTransform selectedObject = (RectTransform)target;
        
        // Eğer seçili objenin adı "Container_Levels" değilse butonları gösterme
        if (selectedObject.name != TARGET_NAME) return;

        GUILayout.Space(20); // Biraz boşluk bırak
        GUILayout.Label("🗺️ Harita Yol Düzenleyici", EditorStyles.boldLabel);

        // BUTON: Yeni Slot Ekle
        GUI.backgroundColor = Color.green; // Buton yeşil olsun
        if (GUILayout.Button("➕ Yeni Durak (Slot) Ekle", GUILayout.Height(30)))
        {
            AddNewSlot(selectedObject);
        }
        GUI.backgroundColor = Color.white; // Rengi normale döndür

        GUILayout.Space(10);

        // BUTON: Tüm Slotları Temizle (Tehlikeli!)
        GUI.backgroundColor = Color.red; // Buton kırmızı olsun
        if (GUILayout.Button("🗑️ TÜM SLOTLARI SİL"))
        {
            if (EditorUtility.DisplayDialog("Emin misin?", 
                "Bu işlem 'LevelSlot_' ile başlayan TÜM objeleri silecek. Geri alınamaz!", "Evet, Sil", "İptal"))
            {
                DeleteAllSlots(selectedObject);
            }
        }
        GUI.backgroundColor = Color.white;
    }

    private void AddNewSlot(RectTransform parent)
    {
        // Mevcut slot sayısını bul (İsimlendirme için: LevelSlot_0, LevelSlot_1...)
        int currentSlotCount = 0;
        foreach (Transform child in parent)
        {
            if (child.name.StartsWith("LevelSlot_")) currentSlotCount++;
        }

        // 1. Yeni objeyi oluştur
        GameObject newSlot = new GameObject($"LevelSlot_{currentSlotCount}");
        RectTransform rect = newSlot.AddComponent<RectTransform>();
        
        // 2. GÖRÜNÜRLÜK İÇİN IMAGE EKLE 🖼️
        // Böylece sahnede kırmızı bir kare olarak görebilirsin.
        Image img = newSlot.AddComponent<Image>();
        img.color = new Color(1f, 0f, 0f, 0.5f); // Yarı şeffaf kırmızı
        img.raycastTarget = false; // Tıklamayı engellemesin (Buton bunun içine gelecek)

        // 3. Parent ve Boyut Ayarları
        rect.SetParent(parent, false);
        // LevelButton prefabın ne kadarsa (örn 200x200) o boyutta olsun
        rect.sizeDelta = new Vector2(200, 200); 

        // 4. Editör İşlemleri (Undo ve Seçim)
        Undo.RegisterCreatedObjectUndo(newSlot, "Add Level Slot"); // Ctrl+Z ile geri alınabilsin
        Selection.activeGameObject = newSlot; // Oluşan objeyi hemen seç

        Debug.Log($"[Path Editor] Yeni durak eklendi: {newSlot.name}");
    }

    private void DeleteAllSlots(RectTransform parent)
    {
        // Tersten döngü kur ki silerken indeksler kaymasın
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            // Sadece bizim oluşturduğumuz slotları sil, arkaplan resmine dokunma!
            if (child.name.StartsWith("LevelSlot_"))
            {
                Undo.DestroyObjectImmediate(child.gameObject);
            }
        }
        Debug.Log("[Path Editor] Tüm slotlar temizlendi.");
    }
}
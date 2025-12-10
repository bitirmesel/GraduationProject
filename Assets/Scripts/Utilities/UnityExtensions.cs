using UnityEngine;

namespace GraduationProject.Utilities
{
    public static class UnityExtensions
    {
        // Derinlemesine Obje Arama
        public static Transform FindDeepChild(this Transform parent, string name)
        {
            var result = parent.Find(name);
            if (result != null) return result;

            foreach (Transform child in parent)
            {
                result = child.FindDeepChild(name);
                if (result != null) return result;
            }
            return null;
        }

        // Derinlemesine Component Getirme
        public static T GetComponentInDeepChild<T>(this Transform parent, string name) where T : Component
        {
            var target = parent.FindDeepChild(name);
            if (target == null)
            {
                Debug.LogError($"[Missing Reference] '{parent.name}' altında '{name}' bulunamadı!");
                return null;
            }
            
            var component = target.GetComponent<T>();
            if (component == null)
            {
                Debug.LogError($"[Missing Component] '{name}' üstünde '{typeof(T).Name}' yok!");
            }
            return component;
        }
    }
}
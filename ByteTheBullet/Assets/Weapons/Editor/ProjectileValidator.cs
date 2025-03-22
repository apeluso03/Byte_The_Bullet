using UnityEngine;
using UnityEditor;
using Weapons;

public class ProjectileValidator : EditorWindow
{
    [MenuItem("Weapons/Test Projectile Prefab")]
    static void ValidateProjectilePrefab()
    {
        // Find all projectile prefabs in the project
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Weapons/Prefabs" });
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            
            if (prefab == null) continue;
            
            // Check for projectile components
            bool hasRenderer = prefab.GetComponentInChildren<Renderer>() != null;
            bool hasCollider = prefab.GetComponent<Collider2D>() != null || prefab.GetComponent<Collider>() != null;
            bool hasRigidbody = prefab.GetComponent<Rigidbody2D>() != null || prefab.GetComponent<Rigidbody>() != null;
            
            if (!hasRenderer || !hasCollider || !hasRigidbody)
            {
                Debug.LogWarning($"Projectile prefab {prefab.name} may have issues:");
                if (!hasRenderer) Debug.LogWarning("- Missing renderer component");
                if (!hasCollider) Debug.LogWarning("- Missing collider component");
                if (!hasRigidbody) Debug.LogWarning("- Missing rigidbody component");
            }
            else
            {
                Debug.Log($"Projectile prefab {prefab.name} looks good!");
            }
        }
    }
} 
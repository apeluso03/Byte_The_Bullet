using UnityEngine;
using System.Collections;

// Renamed to avoid conflict
public class ExplosionAutoDestroy : MonoBehaviour
{
    // Static list to track all explosions
    private static System.Collections.Generic.List<GameObject> allExplosions = new System.Collections.Generic.List<GameObject>();
    
    void Awake()
    {
        // Register this explosion
        allExplosions.Add(gameObject);
        
        // Debug all active explosions
        Debug.LogWarning("NEW EXPLOSION CREATED! Total count: " + allExplosions.Count);
        
        // Try to delete all old explosions (anything older than 5 seconds)
        StartCoroutine(CleanupOldExplosions());
        
        // Check for animator to get proper animation length
        Animator animator = GetComponent<Animator>();
        float destroyDelay = 2f; // Default delay
        
        if (animator != null)
        {
            // Get animation length for better timing
            AnimatorClipInfo[] clipInfo = animator.GetCurrentAnimatorClipInfo(0);
            if (clipInfo.Length > 0)
            {
                // Set destroy time to animation length plus a small buffer
                destroyDelay = clipInfo[0].clip.length + 0.5f;
                Debug.Log("Explosion animation length: " + clipInfo[0].clip.length + ", setting destroy delay to: " + destroyDelay);
            }
        }
        
        ParticleSystem ps = GetComponent<ParticleSystem>();
        if (ps != null)
        {
            // Get the duration of the particle system
            float totalDuration = ps.main.duration + ps.main.startLifetime.constantMax;
            destroyDelay = totalDuration + 0.5f;
            Debug.Log("Explosion particle duration: " + totalDuration + ", setting destroy delay to: " + destroyDelay);
        }
        
        // Set a longer destroy time to allow animation to complete
        Destroy(gameObject, destroyDelay);
        
        // Disable colliders but don't disable animator
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
        foreach (Collider2D col in colliders) {
            col.enabled = false;
        }
        
        // Disable ANY colliders (3D version just in case)
        Collider[] colliders3D = GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders3D) {
            col.enabled = false;
        }
        
        // Disable ANY scripts except this one and animators
        MonoBehaviour[] scripts = GetComponentsInChildren<MonoBehaviour>();
        foreach (MonoBehaviour script in scripts) {
            if (script != this && script.enabled) {
                // Check if it's an Animator using GetType
                if (script.GetType() != typeof(Animator)) {
                    script.enabled = false;
                }
            }
        }
        
        // Destroy ALL child objects that aren't part of the animation
        for (int i = transform.childCount - 1; i >= 0; i--) {
            Transform child = transform.GetChild(i);
            if (child.GetComponent<Animator>() == null && child.GetComponent<ParticleSystem>() == null) {
                Destroy(child.gameObject);
            }
        }
    }
    
    IEnumerator CleanupOldExplosions()
    {
        yield return new WaitForSeconds(0.1f);
        
        // Create a temporary list to hold explosions to remove
        System.Collections.Generic.List<GameObject> toRemove = new System.Collections.Generic.List<GameObject>();
        
        foreach (GameObject explosion in allExplosions)
        {
            if (explosion == null || explosion != gameObject)
            {
                toRemove.Add(explosion);
            }
        }
        
        // Remove the marked explosions
        foreach (GameObject explosion in toRemove)
        {
            allExplosions.Remove(explosion);
        }
        
        // Force destroy ALL existing explosions
        foreach (GameObject explosion in allExplosions)
        {
            if (explosion != null && explosion != gameObject)
            {
                Debug.LogWarning("FORCE DESTROYING OLD EXPLOSION: " + explosion.name);
                Destroy(explosion);
            }
        }
    }
    
    // Static method to destroy all explosions in the game
    public static void DestroyAllExplosions()
    {
        Debug.LogWarning("DESTROYING ALL EXPLOSIONS: " + allExplosions.Count);
        foreach (GameObject explosion in allExplosions)
        {
            if (explosion != null)
            {
                Destroy(explosion);
            }
        }
        allExplosions.Clear();
    }
    
    void OnDestroy()
    {
        // Remove from tracking list
        allExplosions.Remove(gameObject);
        Debug.Log("Explosion destroyed, remaining: " + allExplosions.Count);
    }
} 
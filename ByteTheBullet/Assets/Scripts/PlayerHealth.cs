using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 3;
    public int currentHealth;

    public List<Image> heartImages; // Assign heart icons in the Inspector
    public Sprite fullHeart;
    public Sprite emptyHeart;

    public float invincibilityDuration = 1f;
    private bool isInvincible = false;

    private SpriteRenderer spriteRenderer;


    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        currentHealth = maxHealth;
        UpdateHearts();
    }

    public void TakeDamage(int amount)
    {
        if (isInvincible) return;

        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        UpdateHearts();

        if (currentHealth <= 0)
        {
            Debug.Log("Player has died!");
            SceneManager.LoadScene("GameOver");
            // Optionally trigger game over here
        }
        else
        {
            StartCoroutine(InvincibilityCoroutine());
        }
    }

    void UpdateHearts()
    {
        for (int i = 0; i < heartImages.Count; i++)
        {
            Color color = heartImages[i].color;
            color.a = (i < currentHealth) ? 1f : 0f;  // Fully visible if health, invisible otherwise
            heartImages[i].color = color;
        }
    }

    private IEnumerator InvincibilityCoroutine()
    {
        isInvincible = true;

        float elapsed = 0f;
        float flashInterval = 0.1f; // Time between flashes

        while (elapsed < invincibilityDuration)
        {
            // Toggle visibility
            spriteRenderer.enabled = !spriteRenderer.enabled;

            yield return new WaitForSeconds(flashInterval);
            elapsed += flashInterval;
        }

        // Ensure sprite is visible and invincibility ends
        spriteRenderer.enabled = true;
        isInvincible = false;
    }

}

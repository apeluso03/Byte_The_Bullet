using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 3;
    public int currentHealth;

    public List<Image> heartImages; // Assign heart icons in the Inspector
    public Sprite fullHeart;
    public Sprite emptyHeart;

    void Start()
    {
        currentHealth = maxHealth;
        UpdateHearts();
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        UpdateHearts();

        if (currentHealth <= 0)
        {
            Debug.Log("Player has died!");
            Time.timeScale = 0f;
            // Optionally trigger game over here
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
}

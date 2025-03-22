using UnityEngine;

namespace Weapons 
{
    public class ShotgunPellet : MonoBehaviour
    {
        public float damage = 10f;
        public Color pelletColor = Color.yellow;
        public float lifetime = 5f;
        
        // Simple visual setup
        private SpriteRenderer spriteRenderer;
        private TrailRenderer trailRenderer;
        
        void Awake()
        {
            // Create a basic visual if none exists
            if (GetComponent<Renderer>() == null)
            {
                // Add a sprite renderer
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
                spriteRenderer.sprite = CreateCircleSprite();
                spriteRenderer.color = pelletColor;
                spriteRenderer.sortingOrder = 10;
                
                // Scale appropriately
                transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
                
                // Add a trail for better visibility
                trailRenderer = gameObject.AddComponent<TrailRenderer>();
                trailRenderer.startWidth = 0.1f;
                trailRenderer.endWidth = 0.0f;
                trailRenderer.time = 0.2f;
                trailRenderer.material = new Material(Shader.Find("Sprites/Default"));
                trailRenderer.startColor = pelletColor;
                trailRenderer.endColor = new Color(pelletColor.r, pelletColor.g, pelletColor.b, 0);
            }
            
            // Set a fixed lifetime
            Destroy(gameObject, lifetime);
            
            Debug.Log($"ShotgunPellet created at {transform.position}, will live for {lifetime} seconds");
        }
        
        private Sprite CreateCircleSprite()
        {
            // Create a simple circle texture
            Texture2D texture = new Texture2D(32, 32);
            Color[] colors = new Color[32 * 32];
            
            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    float dx = x - 16;
                    float dy = y - 16;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    
                    if (dist < 16)
                        colors[y * 32 + x] = Color.white;
                    else
                        colors[y * 32 + x] = Color.clear;
                }
            }
            
            texture.SetPixels(colors);
            texture.Apply();
            
            return Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));
        }
        
        void OnCollisionEnter2D(Collision2D collision)
        {
            Debug.Log($"ShotgunPellet hit {collision.gameObject.name}");
            Destroy(gameObject);
        }
        
        void OnTriggerEnter2D(Collider2D other)
        {
            Debug.Log($"ShotgunPellet triggered with {other.gameObject.name}");
            Destroy(gameObject);
        }
    }
} 
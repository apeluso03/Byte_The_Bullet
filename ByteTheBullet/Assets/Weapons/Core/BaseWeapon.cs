using UnityEngine;
using System.Collections;
using UnityEngine.Events;

namespace Weapons
{
    /// <summary>
    /// Base weapon class that handles core functionality for all weapon types
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class BaseWeapon : MonoBehaviour
    {
        #region Events
        [System.Serializable] public class AmmoChangedEvent : UnityEvent<int, int> { } // Current, Max
        
        [Header("Events")]
        public UnityEvent onFire;
        public UnityEvent onReloadStart;
        public UnityEvent onReloadComplete;
        public UnityEvent onOutOfAmmo;
        public AmmoChangedEvent onAmmoChanged;
        #endregion
        
        #region Weapon Properties
        [Header("Weapon Info")]
        [SerializeField] public string weaponName = "Standard Weapon";
        [SerializeField] public string weaponType = "Standard";
        [SerializeField] public string rarity = "Common";
        [TextArea(3, 5)]
        [SerializeField] public string description = "";
        
        [Header("Damage Settings")]
        [SerializeField] public float damage = 10f;
        [SerializeField] public string damageType = "Physical";
        
        [Header("Ammunition")]
        [SerializeField] protected int currentAmmo;
        [SerializeField] public int magazineSize = 10;
        [SerializeField] protected int reserveAmmo = 100;
        [SerializeField] protected int maxReserveAmmo = 300;
        [SerializeField] public float reloadTime = 1.5f;
        [SerializeField] protected bool isReloading = false;
        
        [Header("Fire Settings")]
        [SerializeField] public float fireRate = 5f; // Rounds per second
        [SerializeField] protected FireMode fireMode = FireMode.SemiAuto;
        [SerializeField] protected float nextFireTime = 0f;
        [SerializeField] protected float maxChargeTime = 1.5f;
        [SerializeField] protected float maxChargeDamageMultiplier = 2.0f;
        
        [Header("Projectile Settings")]
        [SerializeField] public GameObject projectilePrefab;
        [SerializeField] public Transform firePoint;
        [SerializeField] public float projectileSpeed = 20f;
        [Range(0, 1)]
        [SerializeField] public float accuracy = 0.8f; // 0-1, higher is more accurate
        
        [Header("Range Settings")]
        [SerializeField] protected float maxRange = 50f;
        [SerializeField] protected float damageDropoffStart = 25f;
        [SerializeField] protected float minDamageMultiplier = 0.5f;
        
        [Header("Effects")]
        [SerializeField] protected ParticleSystem muzzleFlash;
        [SerializeField] protected AudioClip shootSound;
        [SerializeField] protected AudioClip reloadSound;
        [SerializeField] protected AudioClip emptySound;
        [SerializeField] protected GameObject impactEffectPrefab;
        
        [Header("Camera Shake")]
        [SerializeField] protected float shakeIntensity = 0.2f;
        [SerializeField] protected float shakeDuration = 0.1f;
        
        // References
        protected AudioSource audioSource;
        protected CameraShake cameraShake;
        #endregion
        
        #region Properties
        public bool CanFire => CurrentAmmo > 0 && !isReloading && Time.time >= nextFireTime;
        public bool IsMagazineFull => CurrentAmmo >= magazineSize;
        public float DamageValue => damage;
        
        public int CurrentAmmo 
        { 
            get => currentAmmo;
            protected set 
            {
                int previousAmmo = currentAmmo;
                currentAmmo = Mathf.Clamp(value, 0, magazineSize);
                
                if (previousAmmo != currentAmmo)
                {
                    onAmmoChanged?.Invoke(currentAmmo, magazineSize);
                }
            }
        }
        #endregion
        
        #region Enums
        public enum FireMode 
        { 
            SemiAuto,   // One shot per click
            FullAuto,   // Hold to continue firing
            Burst,      // Fire multiple shots with one click
            Charged     // Hold to charge a more powerful shot
        }
        #endregion
        
        #region Unity Lifecycle
        protected virtual void Awake()
        {
            // Get required components
            audioSource = GetComponent<AudioSource>();
            
            // Initialize ammo
            CurrentAmmo = magazineSize;
            
            // Setup fire point if needed
            if (firePoint == null)
            {
                GameObject firePointObj = new GameObject("FirePoint");
                firePointObj.transform.SetParent(transform);
                firePointObj.transform.localPosition = new Vector3(0.5f, 0, 0);
                firePoint = firePointObj.transform;
            }
            
            // Find or create camera shake component (using modern API)
            cameraShake = FindAnyObjectByType<CameraShake>();
            if (cameraShake == null && Camera.main != null)
            {
                cameraShake = Camera.main.gameObject.AddComponent<CameraShake>();
            }
        }
        
        protected virtual void Update()
        {
            // Handle input in derived classes
        }
        
        protected virtual void OnValidate()
        {
            // Update save indicator when values change in editor
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"Updated properties for {gameObject.name}");
            #endif
        }
        #endregion
        
        #region Weapon Actions
        /// <summary>
        /// Main fire method - should be overridden by weapon types
        /// </summary>
        public virtual void Shoot()
        {
            if (!CanFire)
                return;
                
            // Create projectile
            if (projectilePrefab != null && firePoint != null)
            {
                GameObject projectile = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
                SetupProjectile(projectile, damage);
                ApplyProjectileVelocity(projectile, GetFireDirection());
                projectile.layer = LayerMask.NameToLayer("Projectile");
            }
            
            // Visual and audio effects
            PlayShootEffects();
            
            // Apply camera shake
            ApplyCameraShake(shakeIntensity, shakeDuration);
            
            // Update ammo and timing
            CurrentAmmo--;
            nextFireTime = Time.time + 1f / fireRate;
            
            // Trigger events
            onFire?.Invoke();
        }
        
        /// <summary>
        /// Charged shot - overridden by weapons that support charging
        /// </summary>
        public virtual void ShootCharged(float chargePercent)
        {
            // Base implementation just does a normal shot
            Shoot();
        }
        
        /// <summary>
        /// Start reload process if not already reloading and not full
        /// </summary>
        public virtual void StartReload()
        {
            if (isReloading || IsMagazineFull || reserveAmmo <= 0)
                return;
                
            StartCoroutine(ReloadCoroutine());
        }
        
        /// <summary>
        /// Force-cancel any active reload
        /// </summary>
        public virtual void CancelReload()
        {
            if (isReloading)
            {
                StopAllCoroutines();
                isReloading = false;
            }
        }
        
        /// <summary>
        /// Add ammo to reserves
        /// </summary>
        public virtual void AddAmmo(int amount)
        {
            reserveAmmo = Mathf.Min(reserveAmmo + amount, maxReserveAmmo);
        }
        #endregion
        
        #region Helper Methods
        protected virtual Vector3 GetFireDirection()
        {
            // Apply accuracy/spread
            if (accuracy < 1f)
            {
                float spreadFactor = 1f - accuracy;
                Vector3 spread = new Vector3(
                    Random.Range(-spreadFactor, spreadFactor),
                    Random.Range(-spreadFactor, spreadFactor),
                    0
                );
                return (firePoint.forward + spread * 0.1f).normalized;
            }
            
            return firePoint.forward;
        }
        
        protected virtual void SetupProjectile(GameObject projectile, float projectileDamage)
        {
            // Add ProjectileBehavior component if it doesn't have one
            ProjectileBehavior behavior = projectile.GetComponent<ProjectileBehavior>();
            if (behavior == null)
            {
                behavior = projectile.AddComponent<ProjectileBehavior>();
            }
            
            // Set projectile properties
            if (behavior != null)
            {
                behavior.damage = projectileDamage;
                behavior.damageType = damageType;
                behavior.impactEffectPrefab = impactEffectPrefab;
                behavior.maxRange = maxRange;
            }
        }
        
        protected virtual void ApplyProjectileVelocity(GameObject projectile, Vector3 direction)
        {
            // Apply velocity based on component type
            Rigidbody2D rb2d = projectile.GetComponent<Rigidbody2D>();
            if (rb2d != null)
            {
                rb2d.linearVelocity = direction * projectileSpeed;
                return;
            }
            
            Rigidbody rb = projectile.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = direction * projectileSpeed;
            }
        }
        
        protected virtual void PlayShootEffects()
        {
            // Play muzzle flash if available
            if (muzzleFlash != null)
            {
                muzzleFlash.Play();
            }
            
            // Play sound effect
            if (audioSource != null && shootSound != null)
            {
                audioSource.PlayOneShot(shootSound);
            }
        }
        
        protected virtual void ApplyCameraShake(float intensity, float duration)
        {
            if (cameraShake != null)
            {
                cameraShake.ShakeCamera(intensity, duration);
            }
        }
        
        protected virtual IEnumerator ReloadCoroutine()
        {
            isReloading = true;
            onReloadStart?.Invoke();
            
            // Play reload sound
            if (audioSource != null && reloadSound != null)
            {
                audioSource.PlayOneShot(reloadSound);
            }
            
            // Wait for reload time
            yield return new WaitForSeconds(reloadTime);
            
            // Calculate how much ammo to reload
            int ammoNeeded = magazineSize - CurrentAmmo;
            int ammoToAdd = Mathf.Min(ammoNeeded, reserveAmmo);
            
            // Add ammo to magazine and remove from reserve
            CurrentAmmo += ammoToAdd;
            reserveAmmo -= ammoToAdd;
            
            isReloading = false;
            onReloadComplete?.Invoke();
        }
        
        /// <summary>
        /// Calculate damage based on distance (implements damage falloff)
        /// </summary>
        public virtual float CalculateDamageForDistance(float baseDamage, float distance)
        {
            // Within effective range, full damage
            if (distance <= damageDropoffStart)
                return baseDamage;
                
            // Beyond max range, minimum damage
            if (distance >= maxRange)
                return baseDamage * minDamageMultiplier;
                
            // Calculate damage falloff
            float falloffRange = maxRange - damageDropoffStart;
            float distanceInFalloffRange = distance - damageDropoffStart;
            float falloffPercent = distanceInFalloffRange / falloffRange;
            
            // Linear interpolation between full damage and minimum damage
            float damageMultiplier = Mathf.Lerp(1f, minDamageMultiplier, falloffPercent);
            return baseDamage * damageMultiplier;
        }
        #endregion
        
        #region Debug Visualization
        protected virtual void OnDrawGizmosSelected()
        {
            if (firePoint == null) return;
            
            // Draw effective range circle (full damage)
            Gizmos.color = Color.green;
            DrawGizmoCircle(firePoint.position, damageDropoffStart, 24);
            
            // Draw max range circle (minimum damage)
            Gizmos.color = Color.red;
            DrawGizmoCircle(firePoint.position, maxRange, 24);
            
            // Draw fire point
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(firePoint.position, 0.1f);
            Gizmos.DrawRay(firePoint.position, firePoint.forward * 1f);
        }
        
        private void DrawGizmoCircle(Vector3 center, float radius, int segments)
        {
            Vector3 prevPos = center + new Vector3(radius, 0, 0);
            
            for (int i = 0; i < segments + 1; i++)
            {
                float angle = (float)i / segments * Mathf.PI * 2;
                Vector3 pos = center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
                Gizmos.DrawLine(prevPos, pos);
                prevPos = pos;
            }
        }
        #endregion
    }
    
    /// <summary>
    /// Basic projectile behavior - handles damage, impacts, and destruction
    /// </summary>
    public class ProjectileBehavior : MonoBehaviour
    {
        public float damage = 10f;
        public string damageType = "Physical";
        public GameObject impactEffectPrefab;
        public float maxRange = 50f;
        public float minTimeToLive = 5f; // Increased from 0.5f to 5f for longer flight time
        
        private Vector3 startPosition;
        private float startTime;
        
        void Start()
        {
            startPosition = transform.position;
            
            // Calculate time to live based on speed and range, but with a much longer minimum
            float speed = GetComponent<Rigidbody2D>()?.linearVelocity.magnitude 
                          ?? GetComponent<Rigidbody>()?.linearVelocity.magnitude 
                          ?? 20f;
            
            // Calculate time based on distance/speed but ensure long visibility time
            float timeToLive = Mathf.Max(maxRange / speed * 3f, minTimeToLive); // Multiplied by 3 for longer flight
            
            // Ensure projectile lasts long enough to be seen
            Destroy(gameObject, timeToLive);
            
            // Initialize startTime
            startTime = Time.time;
        }
        
        void Update()
        {
            // Only destroy if we've exceeded max range by a large amount AND minimum time has passed
            if (Vector3.Distance(startPosition, transform.position) > maxRange * 3f && 
                Time.time - startTime > minTimeToLive)
            {
                Destroy(gameObject);
            }
        }
        
        void OnCollisionEnter2D(Collision2D collision)
        {
            HandleImpact(collision.gameObject, collision.contacts[0].point);
        }
        
        void OnCollisionEnter(Collision collision)
        {
            HandleImpact(collision.gameObject, collision.contacts[0].point);
        }
        
        void OnTriggerEnter2D(Collider2D other)
        {
            HandleImpact(other.gameObject, transform.position);
        }
        
        void OnTriggerEnter(Collider other)
        {
            HandleImpact(other.gameObject, transform.position);
        }
        
        void HandleImpact(GameObject hitObject, Vector3 hitPoint)
        {
            // Spawn impact effect
            if (impactEffectPrefab != null)
            {
                Instantiate(impactEffectPrefab, hitPoint, Quaternion.identity);
            }
            
            // Destroy projectile
            Destroy(gameObject);
        }
    }
} 
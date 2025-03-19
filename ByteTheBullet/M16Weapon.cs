using UnityEngine;

public class M16Weapon : MonoBehaviour
{
    public float grenadeFireRate = 1f;
    private float lastGrenadeFireTime = 0f;
    public int currentGrenadeAmmo = 1;
    public GameObject grenadePrefab;
    public Transform grenadeSpawnPoint;
    private bool grenadeMode = false;
    private Animator weaponAnimator;

    [Header("Grenade Launcher Settings")]
    public int maxGrenadeAmmo = 1;
    public float grenadeReloadTime = 2.5f;
    private bool isGrenadeReloading = false;

    private void Start()
    {
        weaponAnimator = GetComponent<Animator>();
    }

    private void FireGrenade()
    {
        if (currentGrenadeAmmo <= 0 || Time.time - lastGrenadeFireTime < grenadeFireRate || isGrenadeReloading)
        {
            if (currentGrenadeAmmo <= 0)
                Debug.Log("Cannot fire: No grenade ammo");
            return;
        }
        
        weaponAnimator.SetTrigger("FireGrenade");
        
        if (grenadePrefab != null)
        {
            Instantiate(grenadePrefab, grenadeSpawnPoint.position, grenadeSpawnPoint.rotation);
        }
        
        currentGrenadeAmmo--;
        lastGrenadeFireTime = Time.time;
        
        Debug.Log("Fired grenade! Remaining: " + currentGrenadeAmmo);
        
        if (currentGrenadeAmmo <= 0)
        {
            StartGrenadeReload();
        }
    }

    public void ToggleWeaponMode()
    {
        if (isReloading || isGrenadeReloading)
            return;
        
        grenadeMode = !grenadeMode;
        weaponAnimator.SetBool("GrenadeMode", grenadeMode);
        
        Debug.Log("Switched to " + (grenadeMode ? "Grenade Mode (Ammo: " + currentGrenadeAmmo + ")" : "Bullet Mode"));
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            ToggleWeaponMode();
        }
        
        if (Input.GetKeyDown(KeyCode.R))
        {
            if (grenadeMode)
            {
                if (currentGrenadeAmmo < maxGrenadeAmmo && !isGrenadeReloading)
                {
                    StartGrenadeReload();
                }
            }
            else
            {
                if (currentBulletAmmo < maxBulletAmmo && !isReloading)
                {
                    StartBulletReload();
                }
            }
        }
        
        if (Input.GetButton("Fire1"))
        {
            if (grenadeMode)
            {
                FireGrenade();
                
                if (currentGrenadeAmmo <= 0 && !isGrenadeReloading)
                {
                    Debug.Log("Out of grenades! Need to reload.");
                }
            }
            else
            {
                if (!isReloading)
                    FireBullet();
            }
        }
    }

    private void StartBulletReload()
    {
        if (isReloading) return;
        
        isReloading = true;
        weaponAnimator.SetTrigger("Reload");
        Debug.Log("Reloading bullets...");
        
        Invoke("CompleteBulletReload", reloadTime);
    }

    private void CompleteBulletReload()
    {
        currentBulletAmmo = maxBulletAmmo;
        isReloading = false;
        Debug.Log("Bullet reload complete. Ammo: " + currentBulletAmmo);
    }

    private void StartGrenadeReload()
    {
        if (isGrenadeReloading)
            return;
        
        isGrenadeReloading = true;
        weaponAnimator.SetTrigger("Reload");
        Debug.Log("Reloading grenade launcher...");
        
        Invoke("CompleteGrenadeReload", grenadeReloadTime);
    }

    private void CompleteGrenadeReload()
    {
        currentGrenadeAmmo = maxGrenadeAmmo;
        isGrenadeReloading = false;
        Debug.Log("Grenade reload complete. Grenades: " + currentGrenadeAmmo);
    }

    void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 200, 20), "Mode: " + (grenadeMode ? "Grenade" : "Bullet"));
        GUI.Label(new Rect(10, 30, 200, 20), "Grenade Ammo: " + currentGrenadeAmmo + "/" + maxGrenadeAmmo);
        GUI.Label(new Rect(10, 50, 200, 20), "Bullet Ammo: " + currentBulletAmmo + "/" + maxBulletAmmo);
        GUI.Label(new Rect(10, 70, 200, 20), "Reloading: " + (grenadeMode ? isGrenadeReloading : isReloading));
    }
} 
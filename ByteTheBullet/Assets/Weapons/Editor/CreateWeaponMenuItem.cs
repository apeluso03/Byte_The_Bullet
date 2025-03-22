using UnityEngine;
using UnityEditor;

public static class CreateWeaponMenuItem
{
    [MenuItem("Assets/Create/Weapons/New Weapon", false, 10)]
    public static void CreateNewWeapon()
    {
        // Open the weapon creator window
        WeaponCreatorWindow.ShowWindow();
    }
    
    [MenuItem("GameObject/2D Object/Weapons/Shotgun", false, 10)]
    public static void CreateShotgunGameObject()
    {
        // Open the weapon creator window with shotgun pre-selected
        WeaponCreatorWindow window = EditorWindow.GetWindow<WeaponCreatorWindow>("Weapon Creator");
        // The shotgun is already selected by default (index 0)
    }
} 
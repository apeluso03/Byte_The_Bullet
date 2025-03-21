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
} 
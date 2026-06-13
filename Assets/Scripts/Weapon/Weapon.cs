using Fusion;
using UnityEngine;
using static GlobalVariables;

public class Weapon
{
    [Header("Temel Bilgiler")]
    public WeaponID ID;
    public string Name;
    public string Description;
    public WeaponType WeaponType;

    [Header("Atış Özellikleri")]
    public WeaponFireType WeaponFireType;
    public int MagCapacity;
    public int MagAmount;
    public float FireRate;
    public float FireRange;
    public float Damage;
    public float ReloadTime = 2.0f;

    [Header("Geri Tepme ve Dağılma")]
    public Vector2[] RecoilData;
    public float RecoilResetTime = 0.5f;
    public float RecoilStrength = 1.0f;
    public float BaseSpread = 0f;
    public float MaxSpread;
    public float MovementSpreadMultiplier = 0.05f;

    [Header("Fiziksel Referanslar (IK & VFX)")]
    [Tooltip("Merminin ve Muzzle Flash efektinin çıkacağı nokta")]
    public Transform MuzzlePoint;

    [Tooltip("Karakterin sol elinin tutacağı nokta")]
    public Transform LeftGripPoint;

    public Weapon(int magCapacity)
    {
        this.MagCapacity = magCapacity;
    }
}
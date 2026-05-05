using Fusion;
using UnityEngine;
using static GlobalVariables;

public class Weapon
{
    public string Name;
    public string Description;
    public int MagCapacity;
    public int MagAmount;
    public WeaponType WeaponType;
    public WeaponFireType WeaponFireType;
    public float FireRate;
    public float FireRange;
    public float Damage;
    public Vector2[] RecoilData;
    public float RecoilResetTime = 0.5f;
    public float BaseSpread = 0f;      // Dururken bile olan minimum dağılma (0 yaparsan lazer olur)
    public float MaxSpread;       // Zıplarken/Koşarken çıkabileceği maksimum dağılma
    public float MovementSpreadMultiplier = 0.05f; // Hızın dağılmaya olan etkisi (Çarpan)

    public Weapon(int magCapacity)
    {
        this.MagCapacity = magCapacity;
    }
}
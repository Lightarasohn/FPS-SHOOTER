using UnityEngine;
using static GlobalVariables;
public class DesertEagle : Weapon
{
    public DesertEagle() : base(7)
    {
        this.Name = "Desert Eagle";
        this.Description = "Deagle";
        this.MagAmount = 3;
        this.WeaponType = WeaponType.Pistol;
        this.WeaponFireType = WeaponFireType.Single;
        this.FireRate = 0.1f;
        this.FireRange = 150f;
        this.Damage = 35f;
    }
}

public class AK47 : Weapon
{
    public AK47() : base(30)
    {
        this.Name = "AK-47";
        this.Description = "Russian Death Machine, Made By Kalashnikov.";
        this.MagAmount = 3;
        this.WeaponType = WeaponType.Rifle;
        this.WeaponFireType = WeaponFireType.Auto;
        this.FireRate = 0.06f;
        this.FireRange = 200f;
        this.Damage = 32f;
    }
}

public class MG48 : Weapon
{
    public MG48() : base(150)
    {
        this.Name = "MG48";
        this.Description = "Bullet Rain";
        this.MagAmount = 2;
        this.WeaponType = WeaponType.Heavy;
        this.WeaponFireType = WeaponFireType.Auto;
        this.FireRate = 0.05f;
        this.FireRange = 120f;
        this.Damage = 20f;
    }
}
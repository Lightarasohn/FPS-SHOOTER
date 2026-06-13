using UnityEngine;
using static GlobalVariables;
public class Baretta92 : Weapon
{
    public Baretta92() : base(12)
    {
        this.ID = WeaponID.Baretta92;
        this.Name = "Baretta 92";
        this.Description = "Baretta 92";
        this.MagAmount = 3;
        this.WeaponType = WeaponType.Pistol;
        this.WeaponFireType = WeaponFireType.Single;
        this.FireRate = 0.1f;
        this.FireRange = 150f;
        this.Damage = 35f;
        this.RecoilData = WeaponRecoil.Baretta92;
        this.MaxSpread = 0.07f;
        this.RecoilStrength = 0.8f;
    }
}

public class M4A4 : Weapon
{
    public M4A4(): base(25)
    {
        this.ID = WeaponID.M4A4;
        this.Name = "M4A4";
        this.Description = "SWAT's Favorite";
        this.MagAmount = 3;
        this.WeaponType = WeaponType.Rifle;
        this.WeaponFireType = WeaponFireType.Auto;
        this.FireRate = 0.1f;
        this.FireRange = 150f;
        this.Damage = 30f;
        this.RecoilData = WeaponRecoil.M4A4;
        this.MaxSpread = 0.07f;
        this.RecoilStrength = 0.2f;
    }
}

public class AK47 : Weapon
{
    public AK47(): base(30)
    {
        this.ID = WeaponID.AK47;
        this.Name = "AK-47";
        this.Description = "Russian Death Machine, Made By Kalashnikov.";
        this.MagAmount = 3;
        this.WeaponType = WeaponType.Rifle;
        this.WeaponFireType = WeaponFireType.Auto;
        this.FireRate = 0.12f;
        this.FireRange = 200f;
        this.Damage = 32f;
        this.RecoilData = WeaponRecoil.AK47;
        this.MaxSpread = 0.07f;
        this.RecoilStrength = 0.22f;
    }
}

public class MP9 : Weapon
{
    public MP9() : base(30)
    {
        this.ID = WeaponID.MP9;
        this.Name = "MP9";
        this.Description = "Compact Submachine Gun";
        this.MagAmount = 2;
        this.WeaponType = WeaponType.Heavy;
        this.WeaponFireType = WeaponFireType.Auto;
        this.FireRate = 0.08f;
        this.FireRange = 120f;
        this.Damage = 20f;
        this.RecoilData = WeaponRecoil.MP9;
        this.MaxSpread = 0.07f;
        this.RecoilStrength = 0.6f;
    }
}

public class MP5 : Weapon
{
    public MP5() : base(30)
    {
        this.ID = WeaponID.MP5;
        this.Name = "MP5";
        this.Description = "Classic Submachine Gun";
        this.MagAmount = 2;
        this.WeaponType = WeaponType.Heavy;
        this.WeaponFireType = WeaponFireType.Auto;
        this.FireRate = 0.1f;
        this.FireRange = 130f;
        this.Damage = 22f;
        this.RecoilData = WeaponRecoil.MP5;
        this.MaxSpread = 0.07f;
        this.RecoilStrength = 0.4f;
    }
}   
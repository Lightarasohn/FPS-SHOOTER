using UnityEngine;

public class GlobalVariables
{
    // Hangi tuşların basıldığını bit seviyesinde tutacağımız Enum (Bitmask için)
    public enum PlayerAction
    {
        Jump = 0,
        Crouch = 1,
        sprint = 2,      // CS:GO'daki Shift ile yürüme (sessiz/yavaş)
        Fire = 3,      // Sol tık (Ateş)
        Reload = 4     // R tuşu
    }

    // Silah türleri
    public enum WeaponType
    {
        Pistol = 0,
        Shotgun = 1,
        Rifle = 2,
        Heavy = 3
    }

    // Silah ateş etme türleri
    public enum WeaponFireType
    {
        Single = 0,
        Triple = 1,
        Auto = 2
    }

    // Crosshair türleri
    public enum CrosshairType
    {
        Default = 0,
        X = 1,
        Triangle = 2
    }
}

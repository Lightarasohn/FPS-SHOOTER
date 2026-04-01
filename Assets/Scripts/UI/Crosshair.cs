using UnityEngine;
using static GlobalVariables;

public class Crosshair
{
    public CrosshairType CrosshairType;
    public float Length;
    public float Width;
    public float Space;
    public static float MaxValue = 1;
    public static float MinValue = 0;

    public Crosshair(
        CrosshairType crosshairType,
        float length,
        float width,
        float space)
    {
        this.CrosshairType = crosshairType;
        this.Length = length;
        this.Width = width;
    }
}

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

    // Takım Türleri
    public enum Team {
        Spectator, Red, Blue 
    }

    public static class WeaponRecoil
    {
        public static Vector2[] DesertEagle = new Vector2[]
        {
            new(0f, 0f), new(0.2586f, 0.9896f), new(-0.2545f, 0.3122f), new(0.089f, 0.9854f), new(-0.613f, 0.8981f),
            new(0.9684f, 1.3488f), new(-1.5065f, 2.0666f),
        };

        public static Vector2[] AK47 = new Vector2[]
        {
            new(0f, 0f), new(0f, 0.5f), new(0.25f, 1f), new(0f, 1.6f), new(-0.25f, 2.15f),
            new(-0.3f, 3.1f), new(-0.5f, 4.4f), new(-0.7f, 5.8f), new(-1.1f, 7.3f), new(-2f, 9.2f),
            new(-2.5f, 9.8f), new(-2.1f, 9.9f), new(-1.6f, 9.8f), new(-0.9f, 9.7f), new(0.2f, 9.4f),
            new(1.2f, 9.6f), new(2f, 9.8f), new(1f, 10f), new(0f, 9.8f), new(-1f, 10f),
            new(-1.4f, 9.6f), new(-0.5999f, 9.4975f), new(0.2f, 9.6f), new(1f, 9.6f), new(1.8f, 9.6f),
            new(2.6f, 9.8f), new(2.2f, 10f), new(1.6024f, 9.9232f), new(0.8596f, 9.7972f), new(0.2427f, 9.9145f),
        };

        public static Vector2[] M4A1 = new Vector2[]
        {
            new(0f, 0f), new(0.0924f, 0.5436f), new(0.3393f, 1.0382f), new(-0.015f, 1.6358f), new(-0.1959f, 2.2311f),
            new(-0.357f, 3.2015f), new(-0.4675f, 4.6316f), new(-0.7642f, 6.0245f), new(-1.1025f, 7.6695f), new(-2.0087f, 9.7117f),
            new(-2.5471f, 10.369f), new(-2.236f, 10.5295f), new(-1.7632f, 10.4815f), new(-0.9157f, 10.3964f), new(0.1187f, 10.1167f),
            new(1.3097f, 10.3915f), new(2.366f, 10.6371f), new(0.9563f, 10.914f), new(0.1543f, 10.7361f), new(-1.0521f, 11.026f),
        };

        public static Vector2[] MG48 = new Vector2[]
        {
            new(0f, 0f), new(0.247f, 0.517f), new(-0.0078f, 1.036f), new(0.274f, 1.7495f), new(-0.2745f, 2.3226f),
            new(-0.139f, 3.1618f), new(-0.2189f, 4.593f), new(-0.6565f, 5.9406f), new(-1.2889f, 7.5123f), new(-2.1414f, 9.4918f),
            new(-2.4409f, 10.2636f), new(-1.9529f, 10.3725f), new(-1.8645f, 10.3078f), new(-0.9043f, 10.3166f), new(0.0133f, 9.906f),
            new(1.2595f, 10.1112f), new(2.0501f, 10.4858f), new(1.2291f, 10.6191f), new(0.1612f, 10.5453f), new(-1.2846f, 10.7491f),
            new(-1.5755f, 10.4257f), new(-0.3789f, 10.3659f), new(0.126f, 10.4195f), new(0.7849f, 10.3859f), new(1.6291f, 10.4169f),
            new(2.5323f, 10.8298f), new(2.2617f, 10.8837f), new(1.7272f, 10.8314f), new(0.7127f, 10.8245f), new(0.3399f, 11.017f),
            new(-0.0021f, 10.9669f), new(-0.0082f, 11.1472f), new(0.2417f, 10.9982f), new(0.0137f, 11.0312f), new(0.2005f, 11.1608f),
            new(0.3172f, 11.1238f), new(-0.0006f, 11.1258f), new(0.2509f, 11.2988f), new(0.3048f, 11.2665f), new(-0.0154f, 11.4356f),
            new(0.3086f, 11.2502f), new(0.2477f, 11.4876f), new(0.3757f, 11.4558f), new(0.5753f, 11.573f), new(0.0662f, 11.5818f),
            new(0.0023f, 11.555f), new(0.2634f, 11.6743f), new(0.6103f, 11.6419f), new(0.2503f, 11.6923f), new(0.422f, 11.735f),
            new(0.4988f, 11.7691f), new(0.2899f, 11.8425f), new(0.0906f, 11.8105f), new(0.6334f, 11.909f), new(0.0759f, 11.76f),
            new(0.5514f, 11.8394f), new(0.6361f, 11.9001f), new(0.4227f, 11.9299f), new(0.6333f, 11.8761f), new(0.5876f, 12.1024f),
            new(0.5704f, 11.9454f), new(0.2657f, 12.1477f), new(0.3477f, 12.1832f), new(0.3831f, 12.1047f), new(0.5526f, 12.1396f),
            new(0.0705f, 12.1268f), new(0.4085f, 12.2869f), new(0.061f, 12.3319f), new(0.3075f, 12.3074f), new(0.027f, 12.3927f),
            new(0.5905f, 12.2888f), new(0.5796f, 12.4944f), new(-0.0063f, 12.5411f), new(0.4817f, 12.4388f), new(0.0781f, 12.3964f),
            new(0.0826f, 12.5494f), new(0.2234f, 12.5558f), new(0.1947f, 12.4997f), new(0.2334f, 12.6627f), new(0.2693f, 12.5515f),
            new(0.5064f, 12.6389f), new(0.5764f, 12.7319f), new(0.2234f, 12.8087f), new(0.4419f, 12.7696f), new(0.4827f, 12.9087f),
            new(0.4254f, 12.8322f), new(0.6879f, 13.031f), new(0.0053f, 13.0401f), new(0.3364f, 12.9206f), new(0.3369f, 13.0012f),
            new(0.4745f, 13.1104f), new(0.4817f, 12.9697f), new(0.1631f, 13.003f), new(0.5778f, 13.0136f), new(-0.359f, 13.0826f),
            new(0.6239f, 13.1195f), new(0.1657f, 13.1443f), new(-0.3017f, 13.2166f), new(-0.0057f, 13.2568f), new(-0.4443f, 13.2515f),
            new(0.5855f, 13.3359f), new(0.552f, 13.3677f), new(-0.301f, 13.3995f), new(0.4443f, 13.3421f), new(1.1939f, 13.3829f),
            new(0.9719f, 13.4976f), new(0.9747f, 13.4503f), new(0.3717f, 13.5405f), new(1.1357f, 13.601f), new(0.6674f, 13.5676f),
            new(1.128f, 13.6506f), new(0.5558f, 13.6288f), new(0.4753f, 13.6596f), new(1.1005f, 13.747f), new(-0.4237f, 13.795f),
            new(0.7781f, 13.7742f), new(-0.1541f, 13.8554f), new(0.9617f, 13.8928f), new(0.8625f, 13.8796f), new(0.5699f, 13.9477f),
            new(-0.5224f, 13.9165f), new(-0.1842f, 13.9862f), new(0.717f, 13.9994f), new(-0.4438f, 14.086f), new(0.4882f, 14.1153f),
            new(0.9614f, 14.079f), new(0.1694f, 14.1274f), new(-0.3891f, 14.1497f), new(0.8282f, 14.252f), new(1.1756f, 14.2475f),
            new(0.8532f, 14.2557f), new(0.4989f, 14.285f), new(-0.5957f, 14.3248f), new(0.6097f, 14.4114f), new(1.1942f, 14.4491f),
            new(0.2978f, 14.4189f), new(1.1993f, 14.4817f), new(-0.4982f, 14.5304f), new(0.5759f, 14.527f), new(0.6939f, 14.6294f),
            new(0.0332f, 14.6003f), new(0.2979f, 14.6552f), new(0.422f, 14.7106f), new(0.434f, 14.7606f), new(0.911f, 14.79f),
            new(0.6725f, 14.7502f), new(0.056f, 14.8022f), new(0.0419f, 14.8392f), new(0.8986f, 14.871f), new(-0.6432f, 14.9415f),
        };
    }
}

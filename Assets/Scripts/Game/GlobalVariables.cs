using NUnit.Framework;
using System.Collections.Generic;
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
        Reload = 4,     // R tuşu
        Aim = 5,    // Sağ tık (Nişan alma)
        Interact = 6,   // Yerden silah alma (E Tuşu)
        DropWeapon = 7  // Silahı manuel yere atma (G Tuşu)
    }

    // Silah türleri
    public enum WeaponType
    {
        Pistol = 0,
        Shotgun = 1,
        Rifle = 2,
        Heavy = 3
    }

    public enum WeaponID
    {
        None = 0,
        Baretta92 = 1,
        M4A4 = 2,
        AK47 = 3,
        MP9 = 4,
        MP5 = 5,
    }

    // Silah ateş etme türleri
    public enum WeaponFireType
    {
        Single = 0,
        Triple = 1,
        Auto = 2
    }

    // Gövdde Türleri
    public enum HitZoneType
    {
        Head = 0,
        Body = 1,
        BodyPart = 2
    }

    // Crosshair türleri
    public enum CrosshairType
    {
        Default = 0,
        Triangle = 1,
        X = 2
    }

    // Takım Türleri
    public enum Team
    {
        Spectator, Red, Blue
    }
    public enum RoundState
    {
        WaitingForPlayers, // Yeterli oyuncu bekleniyor (Warmup)
        PreRound,          // Freeze time (Satın alma evresi, hareket kapalı)
        Playing,           // Round oynanıyor
        RoundEnd,          // Round bitti, skor dağıtıldı
        MatchEnd           // Maç bitti (örn. 16'ya ulaşan kazandı)
    }

    public enum AugmentType
    {
        Debuff = 0,
        Buff = 1,
        Normal = 2,
    }

    // Hem buff hem de debuff en az 3'er tane olmalı
    public static List<BuffDebuff> ALL_BUFFS_AND_DEBUFFS = new List<BuffDebuff>
    {
        // --- DEBUFFS ---
        new LowGravity(),
        new SlipperyGround(),
        new ShakyHands(),   // YENİ: Titrek Eller (Sekme x1.5)
        new CementShoes(),  // YENİ: Beton Ayaklar (Zıplama İptal)

        // --- BUFFS ---
        new InfiniteAmmo(),
        new DoubleDamage(),
        new DoubleHealth(),
        new ZeroRecoil(),           // YENİ: Sıfır Geri Tepme
        new FirstBulletOneShot(),   // YENİ: Ölümcül İlk Kurşun
        new HalfRecoil(),           // YENİ: Kontrollü Atış (Sekme /2)
        new AdrenalineRush()        // YENİ: Adrenalin Patlaması (Hız x1.5, Can -50)
    };

    public static class WeaponRecoil
    {
        public static Vector2[] Baretta92 = new Vector2[]
        {

            new(0f, 0f), new(0.0164f, 0.9482f), new(0.1025f, 0.4945f), new(-0.1768f, 0.3727f), new(-0.3232f, 0.9386f),
            new(0.0164f, 0.7105f), new(-0.2692f, 1.7175f), new(-0.3956f, 1.6016f), new(1.0151f, 1.258f), new(-3.0844f, 2.1274f),
            new(0.3861f, 0.7505f), new(0.8042f, 0.1958f),
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

        public static Vector2[] M4A4 = new Vector2[]
        {
            new(0f, 0f), new(0.0924f, 0.5436f), new(0.3393f, 1.0382f), new(-0.015f, 1.6358f), new(-0.1959f, 2.2311f),
            new(-0.357f, 3.2015f), new(-0.4675f, 4.6316f), new(-0.7642f, 6.0245f), new(-1.1025f, 7.6695f), new(-2.0087f, 9.7117f),
            new(-2.5471f, 10.369f), new(-2.236f, 10.5295f), new(-1.7632f, 10.4815f), new(-0.9157f, 10.3964f), new(0.1187f, 10.1167f),
            new(1.3097f, 10.3915f), new(2.366f, 10.6371f), new(0.9563f, 10.914f), new(0.1543f, 10.7361f), new(-1.0521f, 11.026f),
        };

        public static Vector2[] MP9 = new Vector2[]
        {
            new(0f, 0f), new(-0.0729f, 0.6145f), new(-0.0796f, 0.835f), new(0.5275f, 0.7459f), new(-0.1978f, 0.4266f),
            new(-0.247f, 0.6526f), new(-0.5502f, 1.6615f), new(0.3487f, 1.5528f), new(-0.6051f, 1.3313f), new(-1.0751f, 2.2821f),
            new(-0.331f, 0.3016f), new(0.4389f, 0.3383f), new(0.4905f, -0.4592f), new(-0.0249f, 0.388f), new(1.4325f, -0.5616f),
            new(1.3752f, 0.6601f), new(0.6083f, -0.2578f), new(-0.7864f, 0.6037f), new(-1.1402f, -0.6217f), new(-1.4182f, 0.3229f),
            new(0.6022f, -0.4213f), new(0.5845f, 0.0294f), new(0.5162f, 0.0838f), new(1.2797f, 0.0875f), new(0.4373f, -0.1084f),
            new(1.3676f, 0.4013f), new(-0.0966f, 0.2328f), new(-2.5563f, -0.0352f), new(0.8058f, -0.1081f), new(-2.1636f, 0.0327f),
        };

        public static Vector2[] MP5 = new Vector2[]
        {
            new(0f, 0f), new(0.0707f, 0.6279f), new(0.4907f, 1.0114f), new(-0.7335f, 0.6301f), new(-0.5323f, 0.0868f),
            new(0.2149f, 0.8908f), new(0.033f, 1.7995f), new(-0.0775f, 1.1281f), new(-0.6299f, 1.9562f), new(-0.655f, 1.4835f),
            new(-1.2834f, 0.6528f), new(0.7217f, 0.6158f), new(1.2238f, -0.4339f), new(0.2925f, 0.151f), new(1.5438f, -0.0506f),
            new(0.7472f, 0.1825f), new(0.2143f, 0.1999f), new(-1.0503f, -0.3402f), new(-1.0301f, -0.0961f), new(-0.2972f, 0.405f),
            new(-0.319f, -0.4518f), new(0.2723f, -0.2375f), new(0.3811f, 0.1261f), new(0.841f, 0.0707f), new(1.5878f, 0.2196f),
            new(0.1118f, 0.1012f), new(0.4108f, 0.4058f), new(-0.787f, -0.0845f), new(-1.1403f, -0.1413f), new(0.8324f, 0.1141f),
        };
    }
}
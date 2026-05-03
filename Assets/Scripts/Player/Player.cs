using Fusion;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Windows;
using static Fusion.NetworkBehaviour;
using static GlobalVariables;

public class Player : NetworkBehaviour
{
    [Networked] public float Health { get; set; } = 100;
    public int MaxHealth = 500;
    public int MinHealth = 0;
    public Color DefaultColor = Color.blue;
    public Weapon PlayerWeapon;
    public Crosshair PlayerCrosshair;

    // YENİ: Arayüz (HUD) Referansını buraya ekliyoruz
    public PlayerHUD LocalHUD;

    public void Awake()
    {
        PlayerWeapon = new DesertEagle();
        PlayerCrosshair = new Crosshair(CrosshairType.X, 0.2f, 0.06f, 0.03f, 0.3f);
    }

    public override void Spawned()
    {
        bool isLocal = Object.HasInputAuthority;

        if (!isLocal)
        {
            // 1. BAŞKA OYUNCU: Kamerasını ve sesini kapatıyoruz (Canvas'a dokunmuyoruz!)
            Camera playerLocalCamera = GetComponentInChildren<Camera>();
            if (playerLocalCamera != null)
                playerLocalCamera.enabled = false;

            AudioListener playerLocalAudioListener = GetComponentInChildren<AudioListener>();
            if (playerLocalAudioListener != null)
                playerLocalAudioListener.enabled = false;
        }
        else
        {
            // 2. BİZİM OYUNCUMUZ: Sahnede duran PlayerHUD scriptini otomatik bul ve eşle
            LocalHUD = FindFirstObjectByType<PlayerHUD>();
        }

        CrosshairManager crosshairManager = FindFirstObjectByType<CrosshairManager>();

        // 3. CROSSHAIR: Sadece bizim oyuncumuz doğduğunda ekrandaki crosshair güncellensin
        if (crosshairManager != null && isLocal)
        {
            crosshairManager.ApplyCrosshairSettings(PlayerCrosshair);
        }
    }

    public void TakeDamage(float damage)
    {
        if (Object.HasStateAuthority)
        {
            Health -= damage;
        }
    }

    // YENİ: Arayüzü (Can ve Mermi) her karede güncelleme işlemi
    public override void Render()
    {
        if (Object.HasInputAuthority && LocalHUD != null)
        {
            int currentAmmo = PlayerWeapon != null ? PlayerWeapon.BulletInMag : 0;
            int totalMags = PlayerWeapon != null ? PlayerWeapon.MagAmount : 0;

            // Health float olduğu için arayüze gönderirken (int) ile tam sayıya çeviriyoruz
            LocalHUD.ArayuzuGuncelle((int)Health, currentAmmo, totalMags);
        }
    }
}
using Fusion;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Windows;
using static Fusion.NetworkBehaviour;
using static GlobalVariables;

public class Player : NetworkBehaviour
{
    [Networked] public int Health { get; set; } = 100;
    public int MaxHealth = 500;
    public int MinHealth = 0;
    public Color DefaultColor = Color.blue;
    public Weapon PlayerWeapon;
    public Crosshair PlayerCrosshair;

    public void Awake()
    {
        PlayerWeapon = new MG48();
        PlayerCrosshair = new Crosshair(CrosshairType.X, 0.2f, 0.06f, 0.03f);
    }

    public override void Spawned()
    {
        bool isLocal = Object.HasInputAuthority;

        if (!isLocal)
        {
            Camera playerLocalCamera = GetComponentInChildren<Camera>();
            if (playerLocalCamera != null)
                playerLocalCamera.enabled = false;

            AudioListener playerLocalAudioListener = GetComponentInChildren<AudioListener>();
            if (playerLocalAudioListener != null)
                playerLocalAudioListener.enabled = false;
        }

        CrosshairManager crosshairManager = FindFirstObjectByType<CrosshairManager>();

        // 2. Kendi belirlediğimiz Crosshair verilerini ekrana çizdir
        if (crosshairManager != null)
        {
            crosshairManager.ApplyCrosshairSettings(PlayerCrosshair);
        }
    }

    public void TakeDamage(int damage)
    {
        if (Object.HasStateAuthority)
        {
            Health -= damage;
        }
    }
}

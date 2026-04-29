using Fusion;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Windows;
using static GlobalVariables;

public class Player : NetworkBehaviour
{
    [Networked] public float Health { get; set; } = 100;
    [Networked] public bool IsAlive { get; set; }
    [Networked] public Team PlayerTeam { get; set; }

    public int MaxHealth = 500;
    public int MinHealth = 0;
    public Color DefaultColor = Color.blue;
    public Weapon PlayerWeapon;
    public Crosshair PlayerCrosshair;

    public PlayerHUD LocalHUD;

    public void Awake()
    {
        PlayerWeapon = new DesertEagle();
        PlayerCrosshair = new Crosshair(CrosshairType.X, 0.2f, 0.06f, 0.03f, 0.3f);
    }

    public override void Spawned()
    {
        // KONSOLA 

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddPlayer(this);

        }
        bool isLocal = Object.HasInputAuthority;
  
        // 1. DÜZELTME: Sadece sunucu (kurucu) doğan oyuncuyu hayatta olarak işaretler
        if (Object.HasStateAuthority)
        {
            IsAlive = true;
        }

        if (!isLocal)
        {
            Camera playerLocalCamera = GetComponentInChildren<Camera>();
            if (playerLocalCamera != null)
                playerLocalCamera.enabled = false;

            AudioListener playerLocalAudioListener = GetComponentInChildren<AudioListener>();
            if (playerLocalAudioListener != null)
                playerLocalAudioListener.enabled = false;
        }
        else
        {
            LocalHUD = FindFirstObjectByType<PlayerHUD>();
        }

        CrosshairManager crosshairManager = FindFirstObjectByType<CrosshairManager>();

        if (crosshairManager != null && isLocal)
        {
            crosshairManager.ApplyCrosshairSettings(PlayerCrosshair);
        }
    }

    // 2. DÜZELTME: Oyuncu sunucudan koparsa (veya silinirse) listeden adını çıkar
    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RemovePlayer(this);
        }
    }

    public void TakeDamage(float damage)
    {
        // Ölü birine hasar vurulmasını engellemek için IsAlive kontrolü eklendi
        if (Object.HasStateAuthority && IsAlive)
        {
            Health -= damage;
            if (Health <= 0)
            {
                Health = 0;
                IsAlive = false;

                // 3. DÜZELTME: Biri öldüğünde GameManager'a kazanan var mı diye kontrol etmesini söyle
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.CheckWinCondition();
                }
            }
        }
    }

    public override void Render()
    {
        if (Object.HasInputAuthority && LocalHUD != null)
        {
            int currentAmmo = PlayerWeapon != null ? PlayerWeapon.BulletInMag : 0;
            int totalMags = PlayerWeapon != null ? PlayerWeapon.MagAmount : 0;

            LocalHUD.ArayuzuGuncelle((int)Health, currentAmmo, totalMags);
        }
    }
}
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

    public void Awake()
    {
        PlayerWeapon = new DesertEagle();
        PlayerCrosshair = PlayerSaveManager.LoadCrosshair();
    }

    public override void Spawned()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddPlayer(this);
        }

        bool isLocal = Object.HasInputAuthority;

        if (Object.HasStateAuthority)
        {
            IsAlive = true;
        }

        if (!isLocal)
        {
            Camera playerLocalCamera = GetComponentInChildren<Camera>();
            if (playerLocalCamera != null) playerLocalCamera.enabled = false;

            AudioListener playerLocalAudioListener = GetComponentInChildren<AudioListener>();
            if (playerLocalAudioListener != null) playerLocalAudioListener.enabled = false;
        }
        else
        {
            // YENİ: FindFirstObjectByType saçmalığını sildik, direkt Instance'dan gidiyoruz.
            if (PlayerHUD.Instance != null && PlayerHUD.Instance.HudCrosshair != null)
            {
                PlayerHUD.Instance.HudCrosshair.ApplyCrosshairSettings(PlayerCrosshair);
            }
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

    // Bu metodu Player sınıfının içine ekle (örneğin Render metodunun üstüne)
    public void UpdateLocalCrosshair(Crosshair newCrosshair)
    {
        if (!Object.HasInputAuthority) return;

        PlayerCrosshair = newCrosshair;

        // YENİ: Sahneyi aramak yerine doğrudan kendi HUD referansımıza gidiyoruz
        if (PlayerHUD.Instance != null && PlayerHUD.Instance.HudCrosshair != null)
        {
            PlayerHUD.Instance.HudCrosshair.ApplyCrosshairSettings(PlayerCrosshair);
        }

    }

    public override void Render()
    {
        // YENİ: LocalHUD yerine doğrudan PlayerHUD.Instance var mı diye soruyoruz
        if (Object.HasInputAuthority && PlayerHUD.Instance != null)
        {
            int currentAmmo = PlayerWeapon != null ? PlayerWeapon.BulletInMag : 0;
            int totalMags = PlayerWeapon != null ? PlayerWeapon.MagAmount : 0;

            // Veriyi doğrudan gönderiyoruz
            PlayerHUD.Instance.ArayuzuGuncelle((int)Health, currentAmmo, totalMags);
        }
    }
}
using Fusion;
using UnityEngine;
using static GlobalVariables;

public class Player : NetworkBehaviour
{
    [Networked] public float Health { get; set; } = 100;
    [Networked] public bool IsAlive { get; set; }
    [Networked] public Team PlayerTeam { get; set; }

    public int MaxHealth = 500;
    public int MinHealth = 0;
    public Color DefaultColor = Color.blue;
    public Crosshair PlayerCrosshair;

    // YENİ: Sadece Weapon nesnesi değil, sahnedeki ağ silahımız (Component)
    public PlayerWeapon EquippedWeapon;

    public void Awake()
    {
        EquippedWeapon = GetComponent<PlayerWeapon>();
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
            // YENİ: Doğduğunda silaha "Sen bir Desert Eagle'sın, mermilerini ona göre ayarla" diyoruz.
            if (EquippedWeapon != null)
            {
                EquippedWeapon.InitializeWeapon(new DesertEagle());
            }
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
            if (PlayerHUD.Instance != null && PlayerHUD.Instance.HudCrosshair != null)
            {
                PlayerHUD.Instance.HudCrosshair.ApplyCrosshairSettings(PlayerCrosshair);
            }
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RemovePlayer(this);
        }
    }

    public void TakeDamage(float damage)
    {
        if (Object.HasStateAuthority && IsAlive)
        {
            Health -= damage;
            if (Health <= 0)
            {
                Health = 0;
                IsAlive = false;

                if (GameManager.Instance != null)
                {
                    GameManager.Instance.CheckWinCondition();
                }
            }
        }
    }

    public void UpdateLocalCrosshair(Crosshair newCrosshair)
    {
        if (!Object.HasInputAuthority) return;

        PlayerCrosshair = newCrosshair;

        if (PlayerHUD.Instance != null && PlayerHUD.Instance.HudCrosshair != null)
        {
            PlayerHUD.Instance.HudCrosshair.ApplyCrosshairSettings(PlayerCrosshair);
        }
    }

    public override void Render()
    {
        if (Object.HasInputAuthority && PlayerHUD.Instance != null)
        {
            // YENİ: Artık ağ silahımız olan EquippedWeapon üzerinden GÜNCEL AĞ verilerini alıyoruz
            int currentAmmo = EquippedWeapon != null ? EquippedWeapon.CurrentAmmo : 0;
            int totalMags = EquippedWeapon != null ? EquippedWeapon.CurrentMags : 0;

            PlayerHUD.Instance.ArayuzuGuncelle((int)Health, currentAmmo, totalMags);
        }
    }
}
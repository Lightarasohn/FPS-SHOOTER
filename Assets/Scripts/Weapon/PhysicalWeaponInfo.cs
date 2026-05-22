using Fusion;
using UnityEngine;
using static GlobalVariables;

public class PhysicalWeaponInfo : NetworkBehaviour
{
    [Header("Kimlik ve Durum")]
    public WeaponID weaponID;

    [Networked] public int DroppedAmmo { get; set; }
    [Networked] public int DroppedMags { get; set; } // YENİ: Yedek şarjörleri de saklayalım
    [Networked] public TickTimer PickupDelay { get; set; } // YENİ: Yere düşünce anında geri alınmasın diye

    [Header("Görsel Referanslar (IK ve VFX)")]
    public Transform LeftGripPoint;
    public Transform MuzzlePoint;

    public override void Spawned()
    {
        // Silah yere düştüğünde (Spawn edildiğinde) 1 saniye boyunca yerden alınamaz
        PickupDelay = TickTimer.CreateFromSeconds(Runner, 1f);
    }
}
using Fusion;
using UnityEngine;
using static GlobalVariables;

public class PhysicalWeaponInfo : NetworkBehaviour
{
    [Header("Kimlik ve Durum")]
    public WeaponID weaponID;

    [Networked] public WeaponID NetworkedWeaponID { get; set; }
    [Networked] public int DroppedAmmo { get; set; }
    [Networked] public int DroppedMags { get; set; }
    [Networked] public TickTimer PickupDelay { get; set; }

    [Header("Görsel Referanslar")]
    public Transform LeftGripPoint;
    public Transform MuzzlePoint;

    public override void Spawned()
    {
        // Fizik yok, her yerde kinematic — pozisyon NetworkObject tarafından senkronize edilir
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        PickupDelay = TickTimer.CreateFromSeconds(Runner, 1f);

        if (NetworkedWeaponID != WeaponID.None)
            weaponID = NetworkedWeaponID;
    }

    // FixedUpdateNetwork tamamen kaldırıldı — gerek yok
}
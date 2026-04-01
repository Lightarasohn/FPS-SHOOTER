using Fusion;
using UnityEngine;
using static GlobalVariables;

public class Weapon
{
    public string Name;
    public string Description;
    public int MagCapacity;
    public int MagAmount;
    public int BulletInMag;
    public WeaponType WeaponType;
    public WeaponFireType WeaponFireType;
    public float FireRate; // Mermiler arası bekleme süresi (Saniye cinsinden, örn: 0.1f)
    public float FireRange;
    public float Damage;

    public Weapon(int magCapacity)
    {
        this.MagCapacity = magCapacity;
        this.BulletInMag = this.MagCapacity;
    }

    // Void yerine bool dönüyoruz ki isabet durumunu bilelim
    public bool Shoot(NetworkRunner runner, PlayerRef player, Vector3 firePointPosition, Vector3 firePointDirection)
    {
        if (this.BulletInMag == 0)
        {
            Debug.Log("Weapon.cs: No More Bullet In The Mag. RELOAD!");
            return false;
        }
        this.BulletInMag -= 1;
        if (runner.LagCompensation.Raycast(
            firePointPosition,
            firePointDirection,
            this.FireRange,
            player,
            out var hit,
            LayerMask.GetMask("Player")))
        {
            Debug.Log("HIT: " + hit.Hitbox.name);

            var playerScript = hit.Hitbox.GetComponent<Player>();
            if (playerScript != null)
            {
                playerScript.TakeDamage(10);
            }
            return true; // Vurduk!
        }

        Debug.Log($"Weapon.cs: Bullet Amount: {this.BulletInMag} || Mag Amount: {this.MagAmount}");

        return false; // Karavana
    }

    public void Reload()
    {
        this.BulletInMag = this.MagCapacity;
        this.MagAmount -= 1;
        Debug.Log($"Weapon.cs: Bullet Amount: {this.BulletInMag} || Mag Amount: {this.MagAmount}");
    }
}
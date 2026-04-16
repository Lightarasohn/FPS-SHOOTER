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
    public float FireRate;
    public float FireRange;
    public float Damage;
    public Vector2[] RecoilData;
    public float RecoilResetTime = 0.5f;


    public Weapon(int magCapacity)
    {
        this.MagCapacity = magCapacity;
        this.BulletInMag = this.MagCapacity;
    }

    public bool CanShoot()
    {
        if (this.BulletInMag == 0)
        {
            Debug.Log("Weapon.cs: No More Bullet In The Mag. RELOAD!");
            return false;
        }
        return true;
    }

    public bool Shoot(NetworkRunner runner, PlayerRef player, Vector3 firePointPosition, Vector3 firePointDirection)
    {
        if (!CanShoot())
            return false;

        this.BulletInMag--;

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
                playerScript.TakeDamage(this.Damage);
            }
            return true;
        }

        Debug.Log($"MISS | Ammo: {this.BulletInMag} || Mag: {this.MagAmount}");
        return false;
    }

    public void Reload()
    {
        this.BulletInMag = this.MagCapacity;
        this.MagAmount -= 1;

        Debug.Log($"Weapon.cs: Bullet Amount: {this.BulletInMag} || Mag Amount: {this.MagAmount}");
    }
}
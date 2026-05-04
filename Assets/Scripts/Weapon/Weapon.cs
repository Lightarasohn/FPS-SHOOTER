using Fusion;
using UnityEngine;
using static GlobalVariables;

public class Weapon
{
    public string Name;
    public string Description;
    public int MagCapacity;
    public int MagAmount;
    public WeaponType WeaponType;
    public WeaponFireType WeaponFireType;
    public float FireRate;
    public float FireRange;
    public float Damage;
    public Vector2[] RecoilData;
    public float RecoilResetTime = 0.5f;
    public float BaseSpread = 0f;      // Dururken bile olan minimum dağılma (0 yaparsan lazer olur)
    public float MaxSpread;       // Zıplarken/Koşarken çıkabileceği maksimum dağılma
    public float MovementSpreadMultiplier = 0.05f; // Hızın dağılmaya olan etkisi (Çarpan)

    public Weapon(int magCapacity)
    {
        this.MagCapacity = magCapacity;
    }

    // YENİ: Shoot metodu artık mermi eksiltmiyor, sadece hasar vurma işini yapıyor.
    public bool Shoot(NetworkRunner runner, PlayerRef player, Vector3 firePointPosition, Vector3 firePointDirection)
    {
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

        return false;
    }
}
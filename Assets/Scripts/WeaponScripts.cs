using Fusion;
using UnityEngine;

public class Weapon : NetworkBehaviour
{
    public Transform firePoint;
    public float range = 100f;

    public void Shoot(NetworkRunner runner, PlayerRef player)
    {
        if (runner.LagCompensation.Raycast(
            firePoint.position,
            firePoint.forward,
            range,
            player,
            out var hit,
            LayerMask.GetMask("Player")))
        {
            Debug.Log("HIT: " + hit.Hitbox.name);

            var health = hit.Hitbox.GetComponent<Health>();
            if (health != null)
            {
                health.TakeDamage(10);
            }
        }
    }
}
using Fusion;
using UnityEngine;

public class Weapon : NetworkBehaviour
{
    public Transform firePoint;
    public float range = 100f;
    private bool isShooting = false;

    public void Shoot(NetworkRunner runner, PlayerRef player)
    {
        isShooting = true;
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
        isShooting = false;
    }

    public void OnDrawGizmos()
    {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(firePoint.position, firePoint.position + (firePoint.forward * range));

        if (isShooting)
        {
            Debug.Log("Shooting Gizmos");
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(firePoint.position, firePoint.position + (firePoint.forward * range));
        }
    }
}
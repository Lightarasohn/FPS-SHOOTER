using Fusion;
using UnityEngine;

public class Weapon : NetworkBehaviour
{
    public Transform firePoint;
    public float range = 100f;

    // Anlık true/false yerine, çizimin ne zamana kadar ekranda kalacağını tutacağımız değişken
    private float _gizmoHideTime = 0f;

    public void Shoot(NetworkRunner runner, PlayerRef player)
    {
        // Ateş edildiğinde, şimdiki zamana 0.1 saniye ekleyip hafızaya alıyoruz.
        // Bu sayede sarı çizgi ekranda 0.1 saniye boyunca görünür kalacak.
        _gizmoHideTime = Time.time + 0.1f;

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

    public void OnDrawGizmos()
    {
        if (firePoint == null) return;

        // Geçerli zaman, belirlediğimiz gizlenme zamanından küçükse (yani ateş edeli 0.1 saniye geçmediyse)
        if (Time.time < _gizmoHideTime)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(firePoint.position, firePoint.position + (firePoint.forward * range));
        }
        else
        {
            // Ateş edilmiyorsa standart kırmızı çizgi
            Gizmos.color = Color.red;
            Gizmos.DrawLine(firePoint.position, firePoint.position + (firePoint.forward * range));
        }
    }
}
using Fusion;

public class Health : NetworkBehaviour
{
    [Networked] public int HP { get; set; } = 100;

    public void TakeDamage(int damage)
    {
        if (Object.HasStateAuthority)
        {
            HP -= damage;
        }
    }
}
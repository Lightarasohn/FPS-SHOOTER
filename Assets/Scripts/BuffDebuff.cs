using Unity.VisualScripting;
using UnityEngine;
using static GlobalVariables;

public abstract class BuffDebuff
{
    public string Name;
    public string Description;
    public AugmentType Type;

    public abstract void ApplyAugment(Player targetPlayer);
    public abstract void RemoveAugment(Player targetPlayer);
}

// HER YENİ EKLENEN BUFF/DEBUFF/NORMAL İÇİN GlobalVariables.cs İÇERİSİNDEKİ ALL_BUFFS_AND_DEBUFFS İÇERİSİNE DE EKLE

public class LowGravity : BuffDebuff
{
    private float _originalGravity;
    public LowGravity()
    {
        this.Name = "Düşük Yerçekimi";
        this.Description = "Yerçekimini yarıya düşürür";
        this.Type = AugmentType.Debuff;
    }

    public override void ApplyAugment(Player targetPlayer)
    {
        PlayerMovement movement = targetPlayer.GetComponent<PlayerMovement>();
        if(movement != null)
        {
            _originalGravity = movement.Gravity;
            movement.Gravity /= 2;
        }
        else
        {
            Debug.LogError("BuffDebuff.cs:LowGravity:ApplyAugment: Movemen Script'i Bulunamadı");
        }
    }

    public override void RemoveAugment(Player targetPlayer)
    {
        PlayerMovement movement = targetPlayer.GetComponent<PlayerMovement>();
        if (movement != null)
        {
            movement.Gravity = _originalGravity;
        }
        else
        {
            Debug.LogError("BuffDebuff.cs:LowGravity:ApplyAugment: Movemen Script'i Bulunamadı");
        }
    }
}

public class ZeroGravity : BuffDebuff
{
    private float _originalGravity;
    public ZeroGravity()
    {
        this.Name = "Sıfır Yerçekimi";
        this.Description = "Yerçekimini sıfırlar ve UÇARSIN!";
        this.Type = AugmentType.Debuff;
    }

    public override void ApplyAugment(Player targetPlayer)
    {
        PlayerMovement movement = targetPlayer.GetComponent<PlayerMovement>();
        if (movement != null)
        {
            _originalGravity = movement.Gravity;
            movement.Gravity = 0;
        }
        else
        {
            Debug.LogError("BuffDebuff.cs:ZeroGravity:ApplyAugment: Movement Script'i Bulunamadı");
        }
    }

    public override void RemoveAugment(Player targetPlayer)
    {
        PlayerMovement movement = targetPlayer.GetComponent<PlayerMovement>();
        if (movement != null)
        {
            movement.Gravity = _originalGravity;
        }
        else
        {
            Debug.LogError("BuffDebuff.cs:ZeroGravity:RemoveAugment: Movement Script'i Bulunamadı");
        }
    }
}

public class SlipperyGround : BuffDebuff
{
    private float _originalFriction;
    
    public SlipperyGround()
    {
        this.Name = "Kaygan Zemin";
        this.Description = "Sürtünmeyi oldukça azaltır \n Zemin sanki ayaklarının altından kayıyor gibi";
        this.Type = AugmentType.Debuff;
    }
    public override void ApplyAugment(Player targetPlayer)
    {
        PlayerMovement movement = targetPlayer.GetComponent<PlayerMovement>();
        if (movement != null)
        {
            _originalFriction = movement.Friction;
            movement.Friction /= 5;
        }
        else
        {
            Debug.LogError("BuffDebuff.cs:SlipperyGround:ApplyAugment: Movement Script'i Bulunamadı");
        }
    }

    public override void RemoveAugment(Player targetPlayer)
    {
        PlayerMovement movement = targetPlayer.GetComponent<PlayerMovement>();
        if (movement != null)
        {
            movement.Friction = _originalFriction;
        }
        else
        {
            Debug.LogError("BuffDebuff.cs:SlipperyGround:RemoveAugment: Movement Script'i Bulunamadı");
        }
    }
}

public class InfiniteAmmo : BuffDebuff
{
    private int _originalMagAmount;

    public InfiniteAmmo()
    {
        this.Name = "Mermi Cehennemi";
        this.Description = "Parmakların yorulana kadar ateş et!";
        this.Type = AugmentType.Buff;
    }

    public override void ApplyAugment(Player targetPlayer)
    {
        PlayerWeapon weapon = targetPlayer.GetComponent<PlayerWeapon>();
        if (weapon != null)
        {
            _originalMagAmount = weapon.WeaponData.MagAmount;
            weapon.WeaponData.MagAmount = 999;
            weapon.CurrentMags = 999;
        }
        else
        {
            Debug.LogError("BuffDebuff.cs:InfiniteAmmo:ApplyAugment: Weapon Script'i Bulunamadı");
        }
    }

    public override void RemoveAugment(Player targetPlayer)
    {
        PlayerWeapon weapon = targetPlayer.GetComponent<PlayerWeapon>();
        if (weapon != null)
        {
            weapon.WeaponData.MagAmount = _originalMagAmount;
        }
        else
        {
            Debug.LogError("BuffDebuff.cs:InfiniteAmmo:RemoveAugment: Weapon Script'i Bulunamadı");
        }
    }
}

public class DoubleDamage : BuffDebuff
{
    private float _originalDamage;
    private int _originalMagAmount;

    public DoubleDamage()
    {
        this.Name = "Daha Güçlü Silahlar";
        this.Description = "Silahın iki kat hasar verir ama tek şarjor ile savaşırsın";
        this.Type = AugmentType.Buff;
    }

    public override void ApplyAugment(Player targetPlayer)
    {
        PlayerWeapon weapon = targetPlayer.GetComponent<PlayerWeapon>();
        if (weapon != null)
        {
            _originalDamage = weapon.WeaponData.Damage;
            _originalMagAmount = weapon.WeaponData.MagAmount;
            weapon.WeaponData.Damage *= 2;
            weapon.WeaponData.MagAmount = 0;
            weapon.CurrentMags = 0;
        }
        else
        {
            Debug.LogError("BuffDebuff.cs:DoubleDamage:ApplyAugment: Weapon Script'i Bulunamadı");
        }
    }

    public override void RemoveAugment(Player targetPlayer)
    {
        PlayerWeapon weapon = targetPlayer.GetComponent<PlayerWeapon>();
        if (weapon != null)
        {
            weapon.WeaponData.MagAmount = _originalMagAmount;
            weapon.WeaponData.Damage = _originalDamage;
        }
        else
        {
            Debug.LogError("BuffDebuff.cs:DoubleDamage:RemoveAugment: Weapon Script'i Bulunamadı");
        }
    }
}

public class DoubleHealth : BuffDebuff
{
    private int _originalMaxHealth;
    public DoubleHealth()
    {
        this.Name = "Yavaş Ölüm";
        this.Description = "Canın iki katına çıkar";
        this.Type = AugmentType.Buff;
    }

    public override void ApplyAugment(Player targetPlayer)
    {
        Player player = targetPlayer.GetComponent<Player>();
        if (player != null)
        {
            _originalMaxHealth = player.MaxHealth;
            player.MaxHealth *= 2;
            player.Health *= 2;
        }
        else
        {
            Debug.LogError("BuffDebuff.cs:DoubleHealth:ApplyAugment: Player Script'i Bulunamadı");
        }
    }

    public override void RemoveAugment(Player targetPlayer)
    {
        Player player = targetPlayer.GetComponent<Player>();
        if (player != null)
        {
            player.MaxHealth = _originalMaxHealth;
        }
        else
        {
            Debug.LogError("BuffDebuff.cs:DoubleHealth:RemoveAugment: Player Script'i Bulunamadı");
        }
    }
}
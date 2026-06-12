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

        // YENİ: Hem weapon scripti var mı, hem de elinde fiziksel bir WeaponData var mı kontrolü
        if (weapon != null && weapon.WeaponData != null)
        {
            _originalMagAmount = weapon.WeaponData.MagAmount;
            weapon.WeaponData.MagAmount = 999;
            weapon.CurrentMags = 999;
        }
        else
        {
            Debug.LogWarning("BuffDebuff.cs:InfiniteAmmo:ApplyAugment: Oyuncunun elinde silah verisi yok, buff uygulanamadı.");
        }
    }

    public override void RemoveAugment(Player targetPlayer)
    {
        PlayerWeapon weapon = targetPlayer.GetComponent<PlayerWeapon>();

        // YENİ: Oyuncu öldüyse ve silahını düşürdüyse null döner, güvenlice atlarız
        if (weapon != null && weapon.WeaponData != null)
        {
            weapon.WeaponData.MagAmount = _originalMagAmount;
            weapon.CurrentMags = _originalMagAmount;
            weapon.CurrentAmmo = weapon.WeaponData.MagCapacity;
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

        // YENİ: Hem weapon scripti var mı, hem de elinde fiziksel bir WeaponData var mı kontrolü
        if (weapon != null && weapon.WeaponData != null)
        {
            _originalDamage = weapon.WeaponData.Damage;
            _originalMagAmount = weapon.WeaponData.MagAmount;

            weapon.WeaponData.Damage *= 2;
            weapon.WeaponData.MagAmount = 0;
            weapon.CurrentMags = 0;
        }
        else
        {
            Debug.LogWarning("BuffDebuff.cs:DoubleDamage:ApplyAugment: Oyuncunun elinde silah verisi yok, buff uygulanamadı.");
        }
    }

    public override void RemoveAugment(Player targetPlayer)
    {
        PlayerWeapon weapon = targetPlayer.GetComponent<PlayerWeapon>();

        // YENİ: Oyuncu öldüyse ve silahını düşürdüyse null döner, güvenlice atlarız
        if (weapon != null && weapon.WeaponData != null)
        {
            weapon.WeaponData.MagAmount = _originalMagAmount;
            weapon.WeaponData.Damage = _originalDamage;
            weapon.CurrentMags = _originalMagAmount;
            weapon.CurrentAmmo = weapon.WeaponData.MagCapacity;
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

public class ZeroRecoil : BuffDebuff
{
    private Vector2[] _originalRecoilData;
    private float _originalBaseSpread;
    private float _originalMaxSpread;
    private float _originalMovementSpreadMultiplier;

    public ZeroRecoil()
    {
        this.Name = "Sıfır Geri Tepme";
        this.Description = "Silahın sekmesini tamamen ortadan kaldırır. Aduket gibi";
        this.Type = AugmentType.Buff;
    }

    public override void ApplyAugment(Player targetPlayer)
    {
        PlayerWeapon weapon = targetPlayer.GetComponent<PlayerWeapon>();

        if (weapon != null && weapon.WeaponData != null)
        {
            // 1. Kamera sekmesini (Recoil) sıfırla
            // PlayerWeapon içindeki "if (WeaponData.RecoilData != null)" kontrolü sayesinde 
            // burayı null yapmak kameranın sekmesini tamamen durduracaktır.
            _originalRecoilData = weapon.WeaponData.RecoilData;
            weapon.WeaponData.RecoilData = null;

            // 2. Mermi dağılımını (Spread) sıfırla
            _originalBaseSpread = weapon.WeaponData.BaseSpread;
            _originalMaxSpread = weapon.WeaponData.MaxSpread;
            _originalMovementSpreadMultiplier = weapon.WeaponData.MovementSpreadMultiplier;

            weapon.WeaponData.BaseSpread = 0f;
            weapon.WeaponData.MaxSpread = 0f;
            weapon.WeaponData.MovementSpreadMultiplier = 0f;
        }
        else
        {
            Debug.LogWarning("BuffDebuff.cs:ZeroRecoil:ApplyAugment: Oyuncunun elinde silah verisi yok, buff uygulanamadı.");
        }
    }

    public override void RemoveAugment(Player targetPlayer)
    {
        PlayerWeapon weapon = targetPlayer.GetComponent<PlayerWeapon>();

        if (weapon != null && weapon.WeaponData != null)
        {
            // Buff bitince orijinal değerleri geri yükle
            weapon.WeaponData.RecoilData = _originalRecoilData;
            weapon.WeaponData.BaseSpread = _originalBaseSpread;
            weapon.WeaponData.MaxSpread = _originalMaxSpread;
            weapon.WeaponData.MovementSpreadMultiplier = _originalMovementSpreadMultiplier;
        }
    }
}

public class FirstBulletOneShot : BuffDebuff
{
    public FirstBulletOneShot()
    {
        this.Name = "Ölümcül İlk Kurşun";
        this.Description = "Her yeni şarjörün ilk mermisi hedefini anında öldürür. Iskalamak istemezsin";
        this.Type = AugmentType.Buff;
    }

    public override void ApplyAugment(Player targetPlayer)
    {
        PlayerWeapon weapon = targetPlayer.GetComponent<PlayerWeapon>();

        if (weapon != null)
        {
            weapon.HasFirstBulletOneShotBuff = true;
        }
        else
        {
            Debug.LogWarning("BuffDebuff.cs:FirstBulletOneShot:ApplyAugment: Oyuncunun elinde silah verisi yok, buff uygulanamadı.");
        }
    }

    public override void RemoveAugment(Player targetPlayer)
    {
        PlayerWeapon weapon = targetPlayer.GetComponent<PlayerWeapon>();

        if (weapon != null)
        {
            weapon.HasFirstBulletOneShotBuff = false;
        }
    }
}

public class HalfRecoil : BuffDebuff
{
    private Vector2[] _originalRecoilData;
    private float _originalBaseSpread;
    private float _originalMaxSpread;
    private float _originalMovementSpreadMultiplier;

    public HalfRecoil()
    {
        this.Name = "Kontrollü Atış";
        this.Description = "Silahın sekmesini yarıya indirir. Hedefi tutturmak artık çok daha kolay";
        this.Type = AugmentType.Buff;
    }

    public override void ApplyAugment(Player targetPlayer)
    {
        PlayerWeapon weapon = targetPlayer.GetComponent<PlayerWeapon>();

        if (weapon != null && weapon.WeaponData != null)
        {
            // --- RECOIL (KAMERA SEKMESİ) YARIYA İNDİRME ---
            if (weapon.WeaponData.RecoilData != null)
            {
                _originalRecoilData = (Vector2[])weapon.WeaponData.RecoilData.Clone();

                Vector2[] halvedRecoil = new Vector2[_originalRecoilData.Length];
                for (int i = 0; i < _originalRecoilData.Length; i++)
                {
                    halvedRecoil[i] = _originalRecoilData[i] / 2f;
                }
                weapon.WeaponData.RecoilData = halvedRecoil;
            }

            // --- SPREAD (MERMİ DAĞILIMI) YARIYA İNDİRME ---
            _originalBaseSpread = weapon.WeaponData.BaseSpread;
            _originalMaxSpread = weapon.WeaponData.MaxSpread;
            _originalMovementSpreadMultiplier = weapon.WeaponData.MovementSpreadMultiplier;

            weapon.WeaponData.BaseSpread /= 2f;
            weapon.WeaponData.MaxSpread /= 2f;
            weapon.WeaponData.MovementSpreadMultiplier /= 2f;
        }
        else
        {
            Debug.LogWarning("BuffDebuff.cs:HalfRecoil:ApplyAugment: Oyuncunun elinde silah verisi yok, buff uygulanamadı.");
        }
    }

    public override void RemoveAugment(Player targetPlayer)
    {
        PlayerWeapon weapon = targetPlayer.GetComponent<PlayerWeapon>();

        if (weapon != null && weapon.WeaponData != null)
        {
            if (_originalRecoilData != null)
            {
                weapon.WeaponData.RecoilData = _originalRecoilData;
            }

            weapon.WeaponData.BaseSpread = _originalBaseSpread;
            weapon.WeaponData.MaxSpread = _originalMaxSpread;
            weapon.WeaponData.MovementSpreadMultiplier = _originalMovementSpreadMultiplier;
        }
    }
}

public class AdrenalineRush : BuffDebuff
{
    private float _originalMaxGroundSpeed;
    private float _originalMaxAirSpeed;
    private int _originalMaxHealth;

    public AdrenalineRush()
    {
        this.Name = "Adrenalin Patlaması";
        this.Description = "Adrenale var mısın hareket hızını arttırır ama sağlığını kalıcı olarak 50 azaltır. Hızlı yaşa, çabuk öl!";
        this.Type = AugmentType.Buff; // Hem iyi hem kötü yönü var ama genelde Buff sayılır.
    }

    public override void ApplyAugment(Player targetPlayer)
    {
        PlayerMovement movement = targetPlayer.GetComponent<PlayerMovement>();
        Player player = targetPlayer.GetComponent<Player>();

        if (movement != null && player != null)
        {
            // --- HIZI 1.5 KATINA ÇIKARMA ---
            _originalMaxGroundSpeed = movement.MaxGroundSpeed;
            _originalMaxAirSpeed = movement.MaxAirSpeed;

            movement.MaxGroundSpeed *= 1.5f;
            movement.MaxAirSpeed *= 1.5f;

            // --- CANI 50 AZALTMA ---
            _originalMaxHealth = player.MaxHealth;

            player.MaxHealth -= 50;
            player.Health -= 50;

            // Oyuncu buff yüzünden ölmesin diye son bir can simidi:
            if (player.Health <= 0)
            {
                player.Health = 1;
            }
        }
        else
        {
            Debug.LogError("BuffDebuff.cs:AdrenalineRush:ApplyAugment: Movement veya Player Script'i Bulunamadı");
        }
    }

    public override void RemoveAugment(Player targetPlayer)
    {
        PlayerMovement movement = targetPlayer.GetComponent<PlayerMovement>();
        Player player = targetPlayer.GetComponent<Player>();

        if (movement != null && player != null)
        {
            // --- DEĞERLERİ ESKİ HALİNE GETİRME ---
            movement.MaxGroundSpeed = _originalMaxGroundSpeed;
            movement.MaxAirSpeed = _originalMaxAirSpeed;

            player.MaxHealth = _originalMaxHealth;
            // Not: Anlık canı (Health) geri vermiyoruz, sadece MaxHealth eski haline dönüyor ki oyuncu tekrar iyileşebilsin.
        }
    }
}

public class ShakyHands : BuffDebuff
{
    private Vector2[] _originalRecoilData;
    private float _originalBaseSpread;
    private float _originalMaxSpread;
    private float _originalMovementSpreadMultiplier;

    public ShakyHands()
    {
        this.Name = "Titrek Eller";
        this.Description = "Silahın sekmesi artar.Bakalım ne kadar profesyonelsin, nişan almak bir kabusa dönüşür";
        this.Type = AugmentType.Debuff;
    }

    public override void ApplyAugment(Player targetPlayer)
    {
        PlayerWeapon weapon = targetPlayer.GetComponent<PlayerWeapon>();

        if (weapon != null && weapon.WeaponData != null)
        {
            // --- RECOIL (KAMERA SEKMESİ) 1.5 KATINA ÇIKARMA ---
            if (weapon.WeaponData.RecoilData != null)
            {
                // Orijinal veriyi bozmamak için kopyasını (clone) alıyoruz
                _originalRecoilData = (Vector2[])weapon.WeaponData.RecoilData.Clone();

                Vector2[] multipliedRecoil = new Vector2[_originalRecoilData.Length];
                for (int i = 0; i < _originalRecoilData.Length; i++)
                {
                    multipliedRecoil[i] = _originalRecoilData[i] * 1.5f;
                }
                weapon.WeaponData.RecoilData = multipliedRecoil;
            }

            // --- SPREAD (MERMİ DAĞILIMI) 1.5 KATINA ÇIKARMA ---
            _originalBaseSpread = weapon.WeaponData.BaseSpread;
            _originalMaxSpread = weapon.WeaponData.MaxSpread;
            _originalMovementSpreadMultiplier = weapon.WeaponData.MovementSpreadMultiplier;

            weapon.WeaponData.BaseSpread *= 1.5f;
            weapon.WeaponData.MaxSpread *= 1.5f;
            weapon.WeaponData.MovementSpreadMultiplier *= 1.5f;
        }
        else
        {
            Debug.LogWarning("BuffDebuff.cs:ShakyHands:ApplyAugment: Oyuncunun elinde silah verisi yok, debuff uygulanamadı.");
        }
    }

    public override void RemoveAugment(Player targetPlayer)
    {
        PlayerWeapon weapon = targetPlayer.GetComponent<PlayerWeapon>();

        if (weapon != null && weapon.WeaponData != null)
        {
            // --- ORİJİNAL DEĞERLERİ GERİ YÜKLEME ---
            if (_originalRecoilData != null)
            {
                weapon.WeaponData.RecoilData = _originalRecoilData;
            }

            weapon.WeaponData.BaseSpread = _originalBaseSpread;
            weapon.WeaponData.MaxSpread = _originalMaxSpread;
            weapon.WeaponData.MovementSpreadMultiplier = _originalMovementSpreadMultiplier;
        }
    }
}

public class CementShoes : BuffDebuff
{
    private float _originalJumpForce;

    public CementShoes()
    {
        this.Name = "Beton Ayaklar";
        this.Description = "Ayaklarına beton dökülmüş gibi hissediyorsun. Zıplamak artık imkansız";
        this.Type = AugmentType.Debuff;
    }

    public override void ApplyAugment(Player targetPlayer)
    {
        PlayerMovement movement = targetPlayer.GetComponent<PlayerMovement>();

        if (movement != null)
        {
            // Orijinal zıplama gücünü hafızaya al ve ardından sıfırla
            _originalJumpForce = movement.JumpForce;
            movement.JumpForce = 0f;
        }
        else
        {
            Debug.LogWarning("BuffDebuff.cs:CementShoes:ApplyAugment: PlayerMovement Script'i Bulunamadı, debuff uygulanamadı.");
        }
    }

    public override void RemoveAugment(Player targetPlayer)
    {
        PlayerMovement movement = targetPlayer.GetComponent<PlayerMovement>();

        if (movement != null)
        {
            // Debuff süresi bitince oyuncunun zıplama yeteneğini geri ver
            movement.JumpForce = _originalJumpForce;
        }
    }
}
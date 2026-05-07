using Fusion;
using UnityEngine;
using static GlobalVariables;

public class Player : NetworkBehaviour
{
    [Networked] public float Health { get; set; } = 100;
    [Networked] public bool IsAlive { get; set; }
    [Networked] public Team PlayerTeam { get; set; }

    public int MaxHealth = 500;
    public int MinHealth = 0;
    public Color DefaultColor = Color.blue;
    public MeshRenderer PlayerBodyRenderer; // YENİ: Kapsülün rengini değiştireceğimiz materyal
    public Crosshair PlayerCrosshair;

    // YENİ: Sadece Weapon nesnesi değil, sahnedeki ağ silahımız (Component)
    public PlayerWeapon EquippedWeapon;

    public BuffDebuff ActiveAugment { get; private set; }

    public void Awake()
    {
        EquippedWeapon = GetComponent<PlayerWeapon>();
        PlayerCrosshair = PlayerSaveManager.LoadCrosshair();
    }

    public override void Spawned()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddPlayer(this);
        }

        bool isLocal = Object.HasInputAuthority;

        if (Object.HasStateAuthority)
        {
            IsAlive = true;

            // YENİ: Patron (Player), doğrudan Silah Slotuna (PlayerWeapon) emri veriyor!
            if (EquippedWeapon != null)
            {
                EquippedWeapon.EquipWeapon(new AK47());
            }
        }

        if (!isLocal)
        {
            Camera playerLocalCamera = GetComponentInChildren<Camera>();
            if (playerLocalCamera != null) playerLocalCamera.enabled = false;

            AudioListener playerLocalAudioListener = GetComponentInChildren<AudioListener>();
            if (playerLocalAudioListener != null) playerLocalAudioListener.enabled = false;
        }
        else
        {
            if (PlayerHUD.Instance != null && PlayerHUD.Instance.HudCrosshair != null)
            {
                PlayerHUD.Instance.HudCrosshair.ApplyCrosshairSettings(PlayerCrosshair);
            }
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RemovePlayer(this);
        }
    }

    public void TakeDamage(float damage)
    {
        if (Object.HasStateAuthority && IsAlive)
        {
            Health -= damage;
            if (Health <= 0)
            {
                Health = 0;
                IsAlive = false;

                if (GameManager.Instance != null)
                {
                    GameManager.Instance.CheckWinCondition();
                }
            }
        }
    }

    public void UpdateLocalCrosshair(Crosshair newCrosshair)
    {
        if (!Object.HasInputAuthority) return;

        PlayerCrosshair = newCrosshair;

        if (PlayerHUD.Instance != null && PlayerHUD.Instance.HudCrosshair != null)
        {
            PlayerHUD.Instance.HudCrosshair.ApplyCrosshairSettings(PlayerCrosshair);
        }
    }

    public void RequestBuff(string buffName)
    {
        // Sunucuya "Bana bu buff'ı ver" diyoruz
        RPC_ApplyBuff(buffName);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_ApplyBuff(string buffName)
    {
        // SADECE SUNUCU BURAYA GİRER.
        BuffDebuff newAugment = null;

        // --- YENİ: KUSURSUZ FACTORY (FABRİKA) MİMARİSİ ---

        // 1. Gelen string ismini (Örn: "LowGravity") gerçek bir C# Türüne (Type) çeviriyoruz.
        System.Type buffType = System.Type.GetType(buffName);

        // 2. Güvenlik: Eğer böyle bir sınıf gerçekten varsa ve bizim BuffDebuff'tan türetilmişse...
        if (buffType != null && buffType.IsSubclassOf(typeof(BuffDebuff)))
        {
            // 3. O sınıftan yepyeni bir obje yarat! (Elle "new LowGravity()" yazmakla birebir aynıdır)
            newAugment = (BuffDebuff)System.Activator.CreateInstance(buffType);
        }
        else
        {
            Debug.LogError($"[Sunucu] HATA: '{buffName}' adında geçerli bir Buff/Debuff sınıfı bulunamadı!");
            return;
        }

        // --- UYGULAMA KISMI (Eskisiyle aynı) ---
        if (newAugment != null)
        {
            // Eğer eskinden kalan bir buff varsa temizle
            if (ActiveAugment != null)
            {
                ActiveAugment.RemoveAugment(this);
            }

            // Yeni buff'ı kaydet ve uygula
            ActiveAugment = newAugment;
            ActiveAugment.ApplyAugment(this);

            Debug.Log($"[Sunucu] {this.name} oyuncusuna {newAugment.Name} uygulandı!");
        }
    }

    // Round bittiğinde veya yeni round başladığında temizlemek için (GameManager'dan çağırabilirsin)
    public void ClearAugments()
    {
        if (ActiveAugment != null)
        {
            ActiveAugment.RemoveAugment(this);
            ActiveAugment = null;
        }
    }

    public override void Render()
    {
        // 1. RENGİ GÜNCELLE (Herkes Herkesi Kırmızı/Mavi Görsün Diye InputAuthority Sormuyoruz)
        if (PlayerBodyRenderer != null)
        {
            // Eğer takımı Blue ise mavi, Red ise kırmızı yap
            if (PlayerTeam == Team.Blue)
            {
                PlayerBodyRenderer.material.color = Color.blue;
                DefaultColor = Color.blue; // Başka scriptler okuyacaksa diye güncel tut
            }
            else if (PlayerTeam == Team.Red)
            {
                PlayerBodyRenderer.material.color = Color.red;
                DefaultColor = Color.red;
            }
        }

        // 2. ARAYÜZÜ GÜNCELLE (Sadece kendi ekranımızda canı ve mermiyi görelim diye)
        if (Object.HasInputAuthority && PlayerHUD.Instance != null)
        {
            int currentAmmo = EquippedWeapon != null ? EquippedWeapon.CurrentAmmo : 0;
            int totalMags = EquippedWeapon != null ? EquippedWeapon.CurrentMags : 0;

            PlayerHUD.Instance.ArayuzuGuncelle((int)Health, currentAmmo, totalMags);
        }
    }
}
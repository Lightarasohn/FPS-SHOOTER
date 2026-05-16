using Fusion;
using System.Collections.Generic;
using UnityEngine;
using static GlobalVariables;

public class Player : NetworkBehaviour
{
    [Networked] public float Health { get; set; } = 100;
    [Networked] public float Armor { get; set; } = 100;
    [Networked] public bool IsAlive { get; set; }
    [Networked] public Team PlayerTeam { get; set; }
    [Networked] public NetworkString<_32> PlayerName { get; set; } 
    [Networked] public int Kills { get; set; }
    [Networked] public int Deaths { get; set; }
    [Networked] public int Assists { get; set; }

    public int MaxHealth = 500;
    public int MinHealth = 0;
    public float ArmorEfectiveness = 2;
    public Color DefaultColor = Color.blue;
    public MeshRenderer PlayerBodyRenderer; // YENİ: Kapsülün rengini değiştireceğimiz materyal
    public Crosshair PlayerCrosshair;

    // YENİ: Sadece Weapon nesnesi değil, sahnedeki ağ silahımız (Component)
    public PlayerWeapon EquippedWeapon;

    public BuffDebuff ActiveAugment { get; private set; }
    private Dictionary<Player, float> _damageContributors = new Dictionary<Player, float>();

    public void Awake()
    {
        EquippedWeapon = GetComponent<PlayerWeapon>();
        PlayerCrosshair = PlayerSaveManager.LoadCrosshair();
    }

    public override void Spawned()
    {
        if (GameManager.Instance != null)
        {
            // Oyuncu listeye eklenir
            GameManager.Instance.AddPlayer(this);
        }

        bool isLocal = Object.HasInputAuthority;

        // [Networked] değişkenleri SADECE sunucu değiştirebilir!
        if (HasStateAuthority)
        {
            IsAlive = true;
            Health = 100;
            Armor = 100;
        }
        // --- YENİ: OTOMATİK İSİMLENDİRME SİSTEMİ ---
        if (GameManager.Instance != null)
        {
            // Listede kaç kişi varsa, o sayıyı alıp ismine ekler.
            // 1. giren için "Player 1", 2. giren için "Player 2" olur.
            PlayerName = $"Player {GameManager.Instance.ActivePlayers.Count}";
        }
        // -------------------------------------------


        // YENİ: Patron (Player), doğrudan Silah Slotuna (PlayerWeapon) emri veriyor!
        if (EquippedWeapon != null)
        {
            EquippedWeapon.EquipWeapon(new AK47());
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

    public void TakeDamage(float damage, Player attacker)
    {
        if (!HasStateAuthority || !IsAlive) return;

        float remainDamage = damage;
        float damageDealt = 0;

        if (Armor > 0)
        {
            if (Armor * ArmorEfectiveness >= remainDamage)
            {
                Armor -= remainDamage / ArmorEfectiveness;
                damageDealt += remainDamage / ArmorEfectiveness;
                remainDamage = 0;
            }
            else
            {
                remainDamage -= Armor * ArmorEfectiveness;
                damageDealt += Armor * ArmorEfectiveness;
                Armor = 0;

                Health -= remainDamage;
                damageDealt += remainDamage;
            }
        }
        else
        {
            Health -= remainDamage;
            damageDealt += remainDamage;
            remainDamage = 0;
        }

        // 1. HASAR VERENİ LİSTEYE EKLE / GÜNCELLE
        if (attacker != null && attacker != this)
        {
            if (_damageContributors.ContainsKey(attacker))
            {
                // Zaten vurmuştu, hasarını üstüne ekle
                _damageContributors[attacker] += damageDealt;
            }
            else
            {
                // İlk defa vurdu, listeye ekle
                _damageContributors.Add(attacker, damageDealt);
            }
        }

        // 2. ÖLÜM GERÇEKLEŞTİYSE
        if (Health <= 0)
        {
            Health = 0;
            IsAlive = false;

            AddDeath();

            if (attacker != null && attacker != this)
            {
                attacker.AddKill();
            }

            // 3. ASİSTLERİ HESAPLA
            CalculateAssists(killer: attacker);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.CheckWinCondition();
            }
        }
    }
    private void CalculateAssists(Player killer)
    {
        int assistThreshold = 40; // CS:GO tarzı: Asist almak için en az 40 hasar vermek gerekir

        foreach (var contributor in _damageContributors)
        {
            Player potentialAssister = contributor.Key;
            float damageDealt = contributor.Value;

            // Eğer listedeki kişi asıl katil değilse ve yeterli hasarı vurduysa asist say
            if (potentialAssister != killer && potentialAssister != null)
            {
                if (damageDealt >= assistThreshold)
                {
                    potentialAssister.AddAssist();
                    Debug.Log($"{potentialAssister.PlayerName}, {damageDealt} hasar vurarak asist yaptı!");
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

    public void UpdateMouseSettings(float newSensitivity)
    {
        if (!Object.HasInputAuthority) return;
        PlayerInputHandler inputHandler = GetComponent<PlayerInputHandler>();
        if (inputHandler != null)
        {
            inputHandler.MouseSensitivity = newSensitivity;
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

            PlayerHUD.Instance.ArayuzuGuncelle((int)Health, (int)Armor, currentAmmo, totalMags);
        }
    }
    public void AddKill() => Kills++;
    public void AddDeath() => Deaths++;
    public void AddAssist() => Assists++;
    public bool CanAct()
    {
        return IsAlive && GameManager.Instance != null;
    }
    public void ClearDamageHistory()
    {
        _damageContributors.Clear();
    }
}

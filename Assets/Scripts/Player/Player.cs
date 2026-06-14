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

    public int MaxHealth = 100; // Standart olarak 100 kalmalı, buff ile artmalı
    public int MinHealth = 0;
    public float ArmorEfectiveness = 2;
    public Color DefaultColor = Color.blue;
    public Crosshair PlayerCrosshair;
    public PlayerWeapon EquippedWeapon;

    [Header("Viewmodel ve Gövde Referansları")]
    public GameObject ThirdPersonBody;
    public GameObject ViewmodelRoot;

    public BuffDebuff ActiveAugment { get; private set; }
    private Dictionary<Player, float> _damageContributors = new Dictionary<Player, float>();

    private float _lastHealth;
    private float _lastArmor;
    private int _lastKills;
    private PlayerAudioHandler _playerAudioHandler;

    // RAGDOLL İÇİN YENİ DEĞİŞKENLER
    [Networked] public Vector3 FatalHitPoint { get; set; }
    [Networked] public Vector3 FatalHitDirection { get; set; }

    private Rigidbody[] _ragdollRigidbodies;
    private Collider[] _ragdollColliders;
    private Animator _bodyAnimator;
    public float RagdollImpactForce = 50f; // Vuruş şiddeti (Inspector'dan ayarlayabilirsin)


    // YENİ: Durum değişikliklerini izlemek için
    private ChangeDetector _changeDetector;

    public void Awake()
    {
        EquippedWeapon = GetComponent<PlayerWeapon>();
        PlayerCrosshair = PlayerSaveManager.LoadCrosshair();

        _bodyAnimator = GetComponent<Animator>();

        // RAGDOLL KEMİKLERİNİ VE ANİMATÖRÜ BUL
        if (ThirdPersonBody != null)
        {
            _ragdollRigidbodies = ThirdPersonBody.GetComponentsInChildren<Rigidbody>();
            _ragdollColliders = ThirdPersonBody.GetComponentsInChildren<Collider>(); // YENİ EKLENDİ

            // Oyun başlarken ragdoll kapalı olsun
            SetRagdollState(false);
        }
    }

    public override void Spawned()
    {
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

        if (GameManager.Instance != null)
            GameManager.Instance.AddPlayer(this);

        bool isLocal = Object.HasInputAuthority;

        if (HasStateAuthority)
        {
            // OYUNA SONRADAN KATILMA (LATE-JOIN) KONTROLÜ
            if (GameManager.Instance != null &&
               (GameManager.Instance.CurrentState == RoundState.Playing ||
                GameManager.Instance.CurrentState == RoundState.RoundEnd))
            {
                // Eğer maç oynanırken veya raunt biterken katıldıysa: ÖLÜ OLARAK BAŞLAT
                IsAlive = false;
                Health = 0;
                Armor = 0;
            }
            else
            {
                // Isınma (WaitingForPlayers) veya satın alma (PreRound) evresiyse: CANLI BAŞLAT
                IsAlive = true;
                Health = MaxHealth;
                Armor = 100;
            }
        }

        if (GameManager.Instance != null)
            PlayerName = $"Player {GameManager.Instance.ActivePlayers.Count}";

        if (isLocal)
        {
            SetLayerRecursively(ThirdPersonBody, LayerMask.NameToLayer("LocalPlayerBody"));
            if (ViewmodelRoot != null) ViewmodelRoot.SetActive(true);

            if (PlayerHUD.Instance != null && PlayerHUD.Instance.HudCrosshair != null)
                PlayerHUD.Instance.HudCrosshair.ApplyCrosshairSettings(PlayerCrosshair);
        }
        else
        {
            Camera playerLocalCamera = GetComponentInChildren<Camera>();
            if (playerLocalCamera != null) playerLocalCamera.enabled = false;

            AudioListener playerLocalAudioListener = GetComponentInChildren<AudioListener>();
            if (playerLocalAudioListener != null) playerLocalAudioListener.enabled = false;

            if (ViewmodelRoot != null) ViewmodelRoot.SetActive(false);
        }

        _playerAudioHandler = GetComponent<PlayerAudioHandler>();
        _lastHealth = Health;
        _lastArmor = Armor;
        _lastKills = Kills;

        // Doğduğunda görünürlük durumunu eşitle (Ölü doğduysa anında görünmez olur)
        TogglePlayerVisibility(IsAlive);
    }

    private void SetLayerRecursively(GameObject obj, int newLayer)
    {
        if (obj == null) return;

        // YENİ: EĞER obje IgnoreRaycast katmanındaysa (ragdoll kemikleri), ona hiç dokunma!
        if (obj.layer != LayerMask.NameToLayer("IgnoreRaycast"))
        {
            obj.layer = newLayer;
        }

        foreach (Transform child in obj.transform)
        {
            if (child == null) continue;
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (GameManager.Instance != null)
            GameManager.Instance.RemovePlayer(this);
    }

    public void TakeDamage(float damage, Player attacker, Vector3 hitPoint = default, Vector3 hitDirection = default)
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

        if (attacker != null && attacker != this)
        {
            if (_damageContributors.ContainsKey(attacker))
                _damageContributors[attacker] += damageDealt;
            else
                _damageContributors.Add(attacker, damageDealt);
        }

        if (Health <= 0)
        {
            Health = 0;

            // YENİ: Ölüm vuruşunun geldiği yönü ve noktayı ağa kaydet
            FatalHitPoint = hitPoint;
            FatalHitDirection = hitDirection;

            // YENİ: Silahı yere atma işlemi IsAlive false olmadan hemen önce yapılmalı
            if (EquippedWeapon != null) EquippedWeapon.DropCurrentWeapon();

            IsAlive = false;
            AddDeath();

            if (attacker != null && attacker != this)
                attacker.AddKill();

            CalculateAssists(killer: attacker);

            if (GameManager.Instance != null)
                GameManager.Instance.CheckWinCondition();
        }
    }

    private void CalculateAssists(Player killer)
    {
        int assistThreshold = 40;
        foreach (var contributor in _damageContributors)
        {
            Player potentialAssister = contributor.Key;
            float damageDealt = contributor.Value;

            if (potentialAssister != killer && potentialAssister != null)
            {
                if (damageDealt >= assistThreshold)
                    potentialAssister.AddAssist();
            }
        }
    }

    public void UpdateLocalCrosshair(Crosshair newCrosshair)
    {
        if (!Object.HasInputAuthority) return;
        PlayerCrosshair = newCrosshair;
        if (PlayerHUD.Instance != null && PlayerHUD.Instance.HudCrosshair != null)
            PlayerHUD.Instance.HudCrosshair.ApplyCrosshairSettings(PlayerCrosshair);
    }

    public void UpdateMouseSettings(float newSensitivity)
    {
        if (!Object.HasInputAuthority) return;
        PlayerInputHandler inputHandler = GetComponent<PlayerInputHandler>();
        if (inputHandler != null)
            inputHandler.MouseSensitivity = newSensitivity;
    }

    public void RequestWeapon(string weaponIDStr)
    {
        RPC_ApplyWeapon(weaponIDStr);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_ApplyWeapon(string weaponIDStr)
    {
        if (!System.Enum.TryParse(weaponIDStr, out WeaponID weaponID)) return;
        if (EquippedWeapon == null) return;

        Weapon selectedWeapon = EquippedWeapon.GetWeaponClassFromID(weaponID);
        if (selectedWeapon == null) return;

        EquippedWeapon.EquipWeapon(selectedWeapon);
    }

    public void RequestBuff(string buffName)
    {
        RPC_ApplyBuff(buffName);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_ApplyBuff(string buffName)
    {
        BuffDebuff newAugment = null;
        System.Type buffType = System.Type.GetType(buffName);

        if (buffType != null && buffType.IsSubclassOf(typeof(BuffDebuff)))
            newAugment = (BuffDebuff)System.Activator.CreateInstance(buffType);
        else return;

        if (newAugment != null)
        {
            if (ActiveAugment != null)
                ActiveAugment.RemoveAugment(this);

            ActiveAugment = newAugment;
            ActiveAugment.ApplyAugment(this);
        }
    }

    public void ClearAugments()
    {
        if (ActiveAugment != null)
        {
            ActiveAugment.RemoveAugment(this);
            ActiveAugment = null;
        }
    }

    // YENİ: Oyuncuyu tamamen gizleyen ve fiziksel olarak yok eden metod
    private void TogglePlayerVisibility(bool isVisible)
    {
        if (Object.HasInputAuthority)
        {
            // 1. Şahıs kollarını duruma göre aç/kapat
            if (ViewmodelRoot != null)
                ViewmodelRoot.SetActive(isVisible);

            // YENİ: Hayattayken kendi vücudunu kameradan gizle, öldüğünde ragdoll'unu görmek için Default katmanına al
            int targetLayer = isVisible ? LayerMask.NameToLayer("LocalPlayerBody") : LayerMask.NameToLayer("Default");
            SetLayerRecursively(ThirdPersonBody, targetLayer);
        }

        // Vurulabilme kutularını (Hitbox) kapatıyoruz ki yerdeki cesede mermi sıkılmasın
        HitboxRoot hitboxRoot = GetComponent<HitboxRoot>();
        if (hitboxRoot != null)
        {
            hitboxRoot.HitboxRootActive = isVisible;
        }
    }

    private void SetRagdollState(bool isRagdollActive, Vector3 hitPoint = default, Vector3 hitDirection = default)
    {
        // 2. DÜZELTME: Ragdoll aktifse Animatör'ü kapat ki kemikleri serbest bıraksın!
        if (_bodyAnimator != null)
            _bodyAnimator.enabled = !isRagdollActive;

        // 3. DÜZELTME: Animation Rigging kullanıyorsun, Rig Builder'ı da kapatmalıyız ki IK kemikleri kilitlemesin
        UnityEngine.Animations.Rigging.RigBuilder rigBuilder = GetComponent<UnityEngine.Animations.Rigging.RigBuilder>();
        if (rigBuilder != null)
            rigBuilder.enabled = !isRagdollActive;

        // YENİ: Fiziksel Collider Çakışmalarını Önleme
        if (_ragdollColliders != null)
        {
            foreach (var col in _ragdollColliders)
            {
                // Karakter hayattayken kemiklerin collider'ları kapalı kalır, sadece ölünce açılır.
                // Bu sayede yerdeki zemine sürtünüp yürümeyi (flickering) bozmaz.
                col.enabled = isRagdollActive;
            }
        }

        if (_ragdollRigidbodies == null) return;

        Rigidbody closestBone = null;
        float minDistance = float.MaxValue;

        foreach (var rb in _ragdollRigidbodies)
        {
            // isKinematic true ise kemik animatörü takip eder, false ise yerçekimine yenik düşer
            rb.isKinematic = !isRagdollActive;

            // Eğer karaktere kuvvet uygulanacaksa (ölüm anı) en yakın kemiği bul
            if (isRagdollActive && hitPoint != default)
            {
                float dist = Vector3.Distance(rb.position, hitPoint);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    closestBone = rb;
                }
            }
        }

        // Eğer en yakın kemik bulunduysa, ona mermi yönünde bir itme gücü (Impulse) uygula
        if (isRagdollActive && closestBone != null && hitDirection != default)
        {
            closestBone.AddForceAtPosition(hitDirection * RagdollImpactForce, hitPoint, ForceMode.Impulse);
        }
    }

    public override void Render()
    {
        // YENİ: Ağ üzerinden IsAlive değiştiğinde herkesin ekranında karakteri gizle/göster
        foreach (var change in _changeDetector.DetectChanges(this))
        {
            switch (change)
            {
                case nameof(IsAlive):
                    TogglePlayerVisibility(IsAlive);

                    // YENİ: IsAlive false ise ragdoll açılır, true ise (yeni el başladıysa) ragdoll kapanır ve animatör düzelir.
                    SetRagdollState(!IsAlive, FatalHitPoint, FatalHitDirection);
                    break;

                case nameof(Armor):
                case nameof(Health): // Hem canı hem zırhı aynı case yapısına bağlıyoruz
                    if (Object.HasInputAuthority)
                    {
                        // Can VEYA Zırh azaldıysa (hasar yemişiz demektir)
                        if ((Health < _lastHealth) || (Armor < _lastArmor))
                        {
                            if (_playerAudioHandler != null)
                            {
                                _playerAudioHandler.PlayTakeDamage();
                            }
                        }
                    }
                    break;

                case nameof(Kills):
                    // KRİTİK KONTROL: Sadece skoru artan karakter BİZİM karakterimizse ses çal
                    if (Object.HasInputAuthority && Kills > _lastKills && _playerAudioHandler != null)
                    {
                        _playerAudioHandler.PlayScoreSound();
                    }
                    break;
            }
        }

        _lastHealth = Health;
        _lastArmor = Armor;
        _lastKills = Kills;

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
    public bool CanAct() => IsAlive && GameManager.Instance != null;
    public void ClearDamageHistory() => _damageContributors.Clear();
}
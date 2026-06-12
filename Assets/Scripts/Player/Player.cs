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

    // YENİ: Durum değişikliklerini izlemek için
    private ChangeDetector _changeDetector;

    public void Awake()
    {
        EquippedWeapon = GetComponent<PlayerWeapon>();
        PlayerCrosshair = PlayerSaveManager.LoadCrosshair();
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

        // Doğduğunda görünürlük durumunu eşitle (Ölü doğduysa anında görünmez olur)
        TogglePlayerVisibility(IsAlive);
    }

    private void SetLayerRecursively(GameObject obj, int newLayer)
    {
        if (obj == null) return;
        obj.layer = newLayer;
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
        if (ThirdPersonBody != null)
            ThirdPersonBody.SetActive(isVisible);

        if (Object.HasInputAuthority && ViewmodelRoot != null)
            ViewmodelRoot.SetActive(isVisible);

        // Mermilerin içinden geçmesi için kapsülü kapat
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = isVisible;
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
                    break;
            }
        }

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
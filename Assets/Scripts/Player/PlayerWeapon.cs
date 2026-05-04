using Fusion;
using UnityEngine;
using static GlobalVariables;

public class PlayerWeapon : NetworkBehaviour
{
    public Transform firePoint;
    [Networked] public bool spawnedProjectile { get; set; }

    [Networked] public TickTimer FireCooldown { get; set; }
    [Networked] public NetworkButtons PreviousButtons { get; set; }
    [Networked] public byte BurstShotsLeft { get; set; }

    [Networked] public int CurrentBulletIndex { get; set; }
    [Networked] public TickTimer RecoilResetTimer { get; set; }

    // --- YENİ: AĞ DEĞİŞKENLERİ (Mermiler artık burada yaşıyor) ---
    [Networked] public int CurrentAmmo { get; set; }
    [Networked] public int CurrentMags { get; set; }

    public Vector2 CurrentShotRecoil;

    private ChangeDetector _changeDetector;
    private Material _material;

    // Silahın kalıcı/sabit özelliklerini tutan model
    public Weapon WeaponData { get; private set; }

    private Color _playerDefaultColor;
    private PlayerCamera _playerCamera;

    private float _gizmoHideTime;
    private bool _lastShotHit;
    private Vector3 _lastShootDirection;

    private void Awake()
    {
        _material = GetComponentInChildren<MeshRenderer>().material;
        _playerDefaultColor = GetComponent<Player>().DefaultColor;
        _playerCamera = GetComponent<PlayerCamera>();
    }

    // YENİ: Silahı ilk ele aldığımızda çalışacak inisiyalizasyon
    public void EquipWeapon(Weapon newWeaponModel)
    {
        WeaponData = newWeaponModel;

        if (Object != null && Object.HasStateAuthority)
        {
            CurrentAmmo = WeaponData.MagCapacity;
            CurrentMags = WeaponData.MagAmount;
        }
    }

    public override void Spawned()
    {
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
    }

    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInput input))
        {
            bool firePressed = input.Buttons.WasPressed(PreviousButtons, PlayerAction.Fire);
            bool fireHeld = input.Buttons.IsSet(PlayerAction.Fire);
            bool reloadPressed = input.Buttons.WasPressed(PreviousButtons, PlayerAction.Reload);

            // --- RELOAD İŞLEMİ (Mevcut, sağlam kodun) ---
            if (reloadPressed && Object.HasStateAuthority)
            {
                if (CurrentMags > 0 && CurrentAmmo < WeaponData.MagCapacity)
                {
                    CurrentAmmo = WeaponData.MagCapacity;
                    CurrentMags--;
                }
            }

            bool shouldShoot = false;

            if (RecoilResetTimer.Expired(Runner))
            {
                CurrentBulletIndex = 0;
                RecoilResetTimer = TickTimer.None;
            }

            if (FireCooldown.ExpiredOrNotRunning(Runner))
            {
                switch (WeaponData.WeaponFireType)
                {
                    case WeaponFireType.Single:
                        if (firePressed) shouldShoot = true;
                        break;
                    case WeaponFireType.Auto:
                        if (fireHeld) shouldShoot = true;
                        break;
                    case WeaponFireType.Triple:
                        if (firePressed && BurstShotsLeft == 0) BurstShotsLeft = 3;
                        if (BurstShotsLeft > 0)
                        {
                            shouldShoot = true;
                            BurstShotsLeft--;
                        }
                        break;
                }

                // --- GÜNCELLENEN KISIM: ATIŞ İŞLEMİ VE CS:GO RECOIL ---
                if (shouldShoot && Object.HasStateAuthority && CurrentAmmo > 0)
                {
                    if (WeaponData.RecoilData != null && WeaponData.RecoilData.Length > 0)
                    {
                        CurrentShotRecoil = WeaponData.RecoilData[CurrentBulletIndex];

                        // KAMERAYI SARS (ApplyRecoil içinde RecoilScale ile çarpılıyor)
                        if (_playerCamera != null && Object.HasInputAuthority)
                        {
                            _playerCamera.ApplyRecoil(CurrentShotRecoil);
                        }

                        if (CurrentBulletIndex < WeaponData.RecoilData.Length - 1)
                            CurrentBulletIndex++;
                    }

                    // CS:GO MANTIĞI: MERMİ YÖNÜNÜ KAMERADAN İSTE
                    // Artık Quaterion.Euler ile zorla döndürmüyoruz. 
                    // Kameranın "Gerçek Sekme" açısını alıyoruz.
                    Vector3 shootDirection = firePoint.forward; // Varsayılan
                    if (_playerCamera != null)
                    {
                        shootDirection = _playerCamera.GetShootDirection(transform);
                    }

                    // Mermiyi Eksilt!
                    CurrentAmmo--;

                    // Ateş Et!
                    bool hit = WeaponData.Shoot(Runner, Object.InputAuthority, firePoint.position, shootDirection);
                    _lastShootDirection = shootDirection;

                    RecoilResetTimer = TickTimer.CreateFromSeconds(Runner, WeaponData.RecoilResetTime);
                    spawnedProjectile = !spawnedProjectile;
                    FireCooldown = TickTimer.CreateFromSeconds(Runner, WeaponData.FireRate);

                    _gizmoHideTime = Time.time + 0.1f;
                    _lastShotHit = hit;
                }
            }

            PreviousButtons = input.Buttons;
        }
    }

    public override void Render()
    {
        foreach (var change in _changeDetector.DetectChanges(this))
        {
            switch (change)
            {
                case nameof(spawnedProjectile):
                    _material.color = Color.white;
                    break;
            }
        }
        _material.color = Color.Lerp(_material.color, _playerDefaultColor, Time.deltaTime);
    }

    public void OnDrawGizmos()
    {
        if (firePoint == null) return;

        float range = WeaponData != null ? WeaponData.FireRange : 100f;
        Vector3 direction = _lastShootDirection == Vector3.zero ? firePoint.forward : _lastShootDirection;

        if (Time.time < _gizmoHideTime)
        {
            Gizmos.color = _lastShotHit ? Color.green : Color.yellow;
            Gizmos.DrawLine(firePoint.position, firePoint.position + (direction * range));
        }
        else
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(firePoint.position, firePoint.position + (direction * range));
        }
    }
}
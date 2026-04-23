using Fusion;
using UnityEngine;
using static GlobalVariables;

public class PlayerWeapon : NetworkBehaviour
{
    public Transform firePoint;
    [Networked] public bool spawnedProjectile { get; set; }

    // --- YENİ NETWORKED DEĞİŞKENLER (Sunucu Otoritesi İçin) ---
    [Networked] public TickTimer FireCooldown { get; set; } // Atışlar arası bekleme süresi
    [Networked] public NetworkButtons PreviousButtons { get; set; } // Bir önceki karenin tuşları (Single atış için)
    [Networked] public byte BurstShotsLeft { get; set; } // Triple (Burst) atış sayacı

    [Networked] public int CurrentBulletIndex { get; set; }
    [Networked] public TickTimer RecoilResetTimer { get; set; }
    public Vector2 CurrentShotRecoil;

    private ChangeDetector _changeDetector;
    private Material _material;
    private Weapon _playerWeapon;
    private Color _playerDefaultColor;
    private PlayerCamera _playerCamera;

    // --- GIZMO İÇİN YEREL DEĞİŞKENLER (Ağa gitmez) ---
    private float _gizmoHideTime;
    private bool _lastShotHit;
    private Vector3 _lastShootDirection;

    private void Awake()
    {
        _material = GetComponentInChildren<MeshRenderer>().material;
        _playerWeapon = GetComponent<Player>().PlayerWeapon;
        _playerDefaultColor = GetComponent<Player>().DefaultColor;
        _playerCamera = GetComponent<PlayerCamera>();
    }

    public override void Spawned()
    {
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
    }

    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInput input))
        {
            // O anki butona basılma durumları
            bool firePressed = input.Buttons.WasPressed(PreviousButtons, PlayerAction.Fire); // Tıklandı mı?
            bool fireHeld = input.Buttons.IsSet(PlayerAction.Fire); // Basılı mı tutuluyor?

            // --- YENİ: RELOAD (ŞARJÖR DEĞİŞTİRME) KONTROLÜ ---
            bool reloadPressed = input.Buttons.WasPressed(PreviousButtons, PlayerAction.Reload);

            // Eğer R tuşuna basıldıysa, yetkimiz varsa, yedek şarjörümüz varsa ve mermi ful değilse şarjör değiştir
            if (reloadPressed && Object.HasStateAuthority)
            {
                if (_playerWeapon.MagAmount > 0 && _playerWeapon.BulletInMag < _playerWeapon.MagCapacity)
                {
                    _playerWeapon.Reload();
                }
            }
            // ------------------------------------------------

            bool shouldShoot = false;

            if (RecoilResetTimer.Expired(Runner))
            {
                CurrentBulletIndex = 0;
                RecoilResetTimer = TickTimer.None;
            }

            // 1. COOLDOWN KONTROLÜ: Süre dolduysa veya hiç başlamadıysa atışa izin ver
            if (FireCooldown.ExpiredOrNotRunning(Runner))
            {
                // 2. SİLAH TÜRÜNE GÖRE ATIŞ İZNİ
                switch (_playerWeapon.WeaponFireType)
                {
                    case WeaponFireType.Single:
                        if (firePressed) shouldShoot = true; // Sadece ilk tıklandığında atar
                        break;

                    case WeaponFireType.Auto:
                        if (fireHeld) shouldShoot = true; // Basılı tutulduğu sürece atar
                        break;

                    case WeaponFireType.Triple:
                        // Eğer butona basıldıysa ve sayaç sıfırsa, 3 mermilik burst başlat
                        if (firePressed && BurstShotsLeft == 0)
                        {
                            BurstShotsLeft = 3;
                        }

                        // Sayaç 0'dan büyükse, butona basılmasa bile otomatik at
                        if (BurstShotsLeft > 0)
                        {
                            shouldShoot = true;
                            BurstShotsLeft--; // Mermiyi eksilt
                        }
                        break;
                }

                Vector3 shootDirection = firePoint.forward;

                // 3. ATIŞ İŞLEMİ
                if (shouldShoot && Object.HasStateAuthority && _playerWeapon.CanShoot())
                {
                    // 1. ÖNCE RECOIL'I HESAPLA (Merminin yönünü etkileyeceği için)
                    if (_playerWeapon.RecoilData != null && _playerWeapon.RecoilData.Length > 0)
                    {
                        CurrentShotRecoil = _playerWeapon.RecoilData[CurrentBulletIndex];

                        // Yukarı (pitch) ve sağ-sol (yaw) recoil uygula
                        Quaternion recoilRotation = Quaternion.Euler(CurrentShotRecoil.y, CurrentShotRecoil.x, 0);

                        shootDirection = recoilRotation * shootDirection;

                        if (CurrentBulletIndex < _playerWeapon.RecoilData.Length - 1)
                            CurrentBulletIndex++;
                    }

                    // 2. YENİ: MERMİ YÖNÜNÜ KAMERADAN İSTE
                    // Artık dümdüz firePoint.forward atmıyoruz, kameranın sekme dahil yönünü alıyoruz.
                    if (_playerCamera != null && Object.HasInputAuthority)
                    {
                        _playerCamera.AddRecoil(CurrentShotRecoil);
                    }

                    // 3. ATEŞ ET
                    bool hit = _playerWeapon.Shoot(Runner, Object.InputAuthority, firePoint.position, shootDirection);
                    _lastShootDirection = shootDirection;

                    RecoilResetTimer = TickTimer.CreateFromSeconds(Runner, _playerWeapon.RecoilResetTime);

                    // Efektleri tetikle
                    spawnedProjectile = !spawnedProjectile;

                    // TickTimer'ı baştan kur (FireRate bekleme süresi saniye cinsindendir)
                    FireCooldown = TickTimer.CreateFromSeconds(Runner, _playerWeapon.FireRate);

                    // --- YEREL GIZMO AYARLARI ---
                    _gizmoHideTime = Time.time + 0.1f; // Çizgi ekranda 0.1 saniye kalsın
                    _lastShotHit = hit; // Çizginin rengi için sonucu kaydet
                }
            }

            // Bir sonraki karede kullanmak üzere güncel tuşları "Geçmiş Tuşlar" olarak kaydet
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

        float range = _playerWeapon != null ? _playerWeapon.FireRange : 100f;

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
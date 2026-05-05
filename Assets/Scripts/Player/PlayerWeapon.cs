using Fusion;
using System.Collections;
using UnityEngine;
using static GlobalVariables;

public class PlayerWeapon : NetworkBehaviour
{
    [Networked] public bool spawnedProjectile { get; set; }

    [Networked] public TickTimer FireCooldown { get; set; }
    [Networked] public NetworkButtons PreviousButtons { get; set; }
    [Networked] public byte BurstShotsLeft { get; set; }

    [Networked] public int CurrentBulletIndex { get; set; }
    [Networked] public TickTimer RecoilResetTimer { get; set; }

    // --- YENİ: AĞ DEĞİŞKENLERİ (Mermiler artık burada yaşıyor) ---
    [Networked] public int CurrentAmmo { get; set; }
    [Networked] public int CurrentMags { get; set; }

    // Çarpışma bilgisini ağda taşımak için struct
    [Networked] public Vector3 LastHitPosition { get; set; }
    [Networked] public Vector3 LastHitNormal { get; set; }
    [Networked] public bool LastShotDidHit { get; set; }

    // Silahın kalıcı/sabit özelliklerini tutan model
    public Weapon WeaponData { get; private set; }

    [Header("Gerekli Referanslar (Inspector'dan Sürükle!)")]
    public Transform firePoint;
    public PlayerCamera playerCamera;     // YENİ: Awake'te aramak yerine Inspector'dan ver
    public PlayerMovement playerMovement; // YENİ: Awake'te aramak yerine Inspector'dan ver

    public Vector2 CurrentShotRecoil;

    [Header("Görsel Efektler (VFX)")]
    public TrailRenderer BulletTrailPrefab;       // Mermi izi prefabı
    public ParticleSystem ImpactParticlePrefab;   // Duvara çarpınca çıkacak toz/kıvılcım
    public ParticleSystem MuzzleFlashParticle;    // Namlu ucu alevi (Opsiyonel)
    public float BulletTrailSpeed = 100f;         // Mermi izinin gidiş hızı

    private ChangeDetector _changeDetector;
    private Material _material;

    private Color _playerDefaultColor;
    private PlayerCamera _playerCamera;
    private PlayerMovement _playerMovement;

    private float _gizmoHideTime;
    private bool _lastShotHit;
    private Vector3 _lastShootDirection;

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

        // GÜVENLİK AĞI: Eğer Inspector'dan sürüklemeyi unutursan, SADECE DOĞDUĞUNDA 1 KERE ara.
        if (playerCamera == null) playerCamera = GetComponent<PlayerCamera>();
        if (playerMovement == null) playerMovement = GetComponent<PlayerMovement>();
    }

    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInput input))
        {
            bool firePressed = input.Buttons.WasPressed(PreviousButtons, PlayerAction.Fire);
            bool fireHeld = input.Buttons.IsSet(PlayerAction.Fire);
            bool reloadPressed = input.Buttons.WasPressed(PreviousButtons, PlayerAction.Reload);

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
                if (WeaponData != null)
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
                }

                if (shouldShoot && Object.HasStateAuthority && CurrentAmmo > 0)
                {
                    if (WeaponData.RecoilData != null && WeaponData.RecoilData.Length > 0)
                    {
                        CurrentShotRecoil = WeaponData.RecoilData[CurrentBulletIndex];

                        if (playerCamera != null && Object.HasInputAuthority)
                        {
                            playerCamera.ApplyRecoil(CurrentShotRecoil);
                        }

                        if (CurrentBulletIndex < WeaponData.RecoilData.Length - 1)
                            CurrentBulletIndex++;
                    }

                    Vector3 shootDirection = firePoint.forward;
                    if (playerCamera != null)
                    {
                        shootDirection = playerCamera.GetShootDirection(transform);
                    }

                    float currentSpeed = playerMovement != null ? playerMovement.Velocity.magnitude : 0f;
                    float currentSpread = WeaponData.BaseSpread + (currentSpeed * WeaponData.MovementSpreadMultiplier);
                    currentSpread = Mathf.Clamp(currentSpread, WeaponData.BaseSpread, WeaponData.MaxSpread);

                    if (currentSpread > 0f)
                    {
                        Vector3 randomSpreadOffset = Random.insideUnitSphere * currentSpread;
                        shootDirection += randomSpreadOffset;
                        shootDirection.Normalize();
                    }

                    CurrentAmmo--;

                    bool hit = false;
                    Vector3 hitPosition = firePoint.position + (shootDirection * WeaponData.FireRange);
                    Vector3 hitNormal = Vector3.up;

                    if (Runner.LagCompensation.Raycast(
                        firePoint.position,
                        shootDirection,
                        WeaponData.FireRange,
                        Object.InputAuthority,
                        out var hitResult,
                        LayerMask.GetMask("Player", "Default", "Ground")))
                    {
                        hit = true;
                        hitPosition = hitResult.Point;
                        hitNormal = hitResult.Normal;

                        var playerScript = hitResult.Hitbox != null ? hitResult.Hitbox.GetComponent<Player>() : null;
                        if (playerScript != null)
                        {
                            playerScript.TakeDamage(WeaponData.Damage);
                        }
                    }

                    LastHitPosition = hitPosition;
                    LastHitNormal = hitNormal;
                    LastShotDidHit = hit;

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
        // ESKİDEN BURADA OLAN GEREKSİZ RENG DEĞİŞTİRME KODLARINI SİLDİK (FPS KATİLİ)
        foreach (var change in _changeDetector.DetectChanges(this))
        {
            switch (change)
            {
                case nameof(spawnedProjectile):
                    PlayVisualEffects();
                    break;
            }
        }
    }

    // YENİ: Efekt Oynatma Fonksiyonu
    private void PlayVisualEffects()
    {
        if (MuzzleFlashParticle != null)
        {
            if (MuzzleFlashParticle.gameObject.scene.name == null)
            {
                ParticleSystem flash = Instantiate(MuzzleFlashParticle, firePoint.position, firePoint.rotation, firePoint);
                flash.Play();
                Destroy(flash.gameObject, 1f);
            }
            else
            {
                MuzzleFlashParticle.Play();
            }
        }

        if (BulletTrailPrefab != null && firePoint != null)
        {
            TrailRenderer trail = Instantiate(BulletTrailPrefab, firePoint.position, Quaternion.identity);
            StartCoroutine(SpawnTrailRoutine(trail, LastHitPosition, LastHitNormal, LastShotDidHit));
        }
    }

    // YENİ: Videodaki Coroutine'in Güvenli (Performanslı) hali
    private IEnumerator SpawnTrailRoutine(TrailRenderer trail, Vector3 hitPoint, Vector3 hitNormal, bool madeImpact)
    {
        Vector3 startPosition = trail.transform.position;
        float distance = Vector3.Distance(startPosition, hitPoint);

        if (distance < 0.1f) distance = 0.1f;

        float remainingDistance = distance;

        while (remainingDistance > 0)
        {
            if (trail == null) yield break; // GÜVENLİK

            trail.transform.position = Vector3.Lerp(startPosition, hitPoint, 1 - (remainingDistance / distance));
            remainingDistance -= BulletTrailSpeed * Time.deltaTime;
            yield return null;
        }

        if (trail != null) trail.transform.position = hitPoint;

        if (madeImpact && ImpactParticlePrefab != null)
        {
            ParticleSystem impact = Instantiate(ImpactParticlePrefab, hitPoint, Quaternion.LookRotation(hitNormal));
            Destroy(impact.gameObject, 2f);
        }

        if (trail != null) Destroy(trail.gameObject, trail.time);
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
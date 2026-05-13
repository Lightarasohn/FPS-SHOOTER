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
    [Networked] public bool IsAiming { get; set; }

    // Çarpışma bilgisini ağda taşımak için struct
    [Networked] public Vector3 LastHitPosition { get; set; }
    [Networked] public Vector3 LastHitNormal { get; set; }
    [Networked] public bool LastShotDidHit { get; set; }

    // Silahın kalıcı/sabit özelliklerini tutan model
    public Weapon WeaponData { get; private set; }

    [Header("Gerekli Referanslar (Inspector'dan Sürükle!)")]
    public Transform weaponPoint; // Silahın modeli, görsel efektler için referans noktası
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

    public Player Owner { get; set; }


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
    public void ResetAmmo()
    {
        if (WeaponData == null) return;

        CurrentAmmo = WeaponData.MagCapacity;
        CurrentMags = WeaponData.MagAmount;
    }

    public override void Spawned()
    {
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

        // GÜVENLİK AĞI: Eğer Inspector'dan sürüklemeyi unutursan, SADECE DOĞDUĞUNDA 1 KERE ara.
        if (playerCamera == null) playerCamera = GetComponent<PlayerCamera>();
        if (playerMovement == null) playerMovement = GetComponent<PlayerMovement>();
        Owner= GetComponent<Player>();
    }

    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInput input))
        {
            if (Owner != null && !Owner.IsAlive)
            {
                PreviousButtons = input.Buttons;
                return;
            }

            if (GameManager.Instance == null  || GameManager.Instance.CurrentState == RoundState.PreRound)
            {
                PreviousButtons = input.Buttons;
                return;
            }
            
            bool firePressed = input.Buttons.WasPressed(PreviousButtons, PlayerAction.Fire);
            bool fireHeld = input.Buttons.IsSet(PlayerAction.Fire);
            bool reloadPressed = input.Buttons.WasPressed(PreviousButtons, PlayerAction.Reload);

            // YENİ: Nişan alma (Sağ tık) inputunu oku
            IsAiming = input.Buttons.IsSet(PlayerAction.Aim);

            // Kameraya nişan alıp almadığımızı bildir (Sadece lokal oyuncuda kamerayı hareket ettir)
            if (HasInputAuthority && playerCamera != null)
            {
                playerCamera.HandleADS(IsAiming);
            }

            // DÜZELTME 1: Reload işleminden Object.HasStateAuthority kısıtlamasını kaldırdık.
            // Artık Client şarjör değiştirdiğinde sunucuyu beklemeden anında mermisi dolacak (Prediction).
            if (reloadPressed)
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

                // DÜZELTME 2: Object.HasStateAuthority kısıtlamasını SİLDİK!
                // Artık Client'lar ateş ettiğinde kendi bilgisayarlarında da bu bloğa girecekler.
                if (shouldShoot && CurrentAmmo > 0)
                {
                    if (WeaponData.RecoilData != null && WeaponData.RecoilData.Length > 0)
                    {
                        CurrentShotRecoil = WeaponData.RecoilData[CurrentBulletIndex];

                        // --- YENİ 1: NİŞAN ALIRKEN KAMERANIN SEKMESİNİ (RECOIL) AZALT ---
                        if (IsAiming)
                        {
                            CurrentShotRecoil *= 0.5f; // Nişan alırken kamera %50 daha az seker
                        }
                        // ----------------------------------------------------------------

                        // DOĞRU KULLANIM: Host da bu matematiği hesaplamalı ki merminin nereye sektiğini bilsin!
                        if (playerCamera != null)
                        {
                            playerCamera.ApplyRecoil(CurrentShotRecoil);
                        }

                        if (CurrentBulletIndex < WeaponData.RecoilData.Length - 1)
                            CurrentBulletIndex++;
                    }

                    // --- PARALAKS ÇÖZÜMÜ BAŞLANGICI ---

                    Vector3 shootDirection = firePoint.forward;
                    Vector3 raycastOrigin = firePoint.position;

                    if (playerCamera != null)
                    {
                        shootDirection = playerCamera.GetShootDirection(transform);

                        // YENİ VE KUSURSUZ ÇÖZÜM:
                        // Görsel (Render) pozisyonuna güvenmek yerine, oyuncunun o anki
                        // OLMASI GEREKEN boyunu matematiksel olarak buluyoruz.
                        float targetCamHeight = playerCamera.StandingCameraHeight;

                        if (playerMovement != null && playerMovement.IsCrouching)
                        {
                            targetCamHeight = playerCamera.CrouchingCameraHeight;
                        }

                        // PlayerWeapon.cs içerisindeki o satırı şu şekilde değiştir:
                        Vector3 exactLocalPos = new Vector3(0f, targetCamHeight, 0f);

                        // Elde ettiğimiz bu lokal pozisyonu, dünyadaki (World Space) gerçek yerine çeviriyoruz.
                        raycastOrigin = transform.TransformPoint(exactLocalPos);
                    }

                    float currentSpeed = playerMovement != null ? playerMovement.Velocity.magnitude : 0f;

                    float currentSpread = WeaponData.BaseSpread + (currentSpeed * WeaponData.MovementSpreadMultiplier);
                    currentSpread = Mathf.Clamp(currentSpread, WeaponData.BaseSpread, WeaponData.MaxSpread);

                    // --- YENİ 2: NİŞAN ALIRKEN RASTGELE DAĞILIMI (SPREAD) SIFIRLA VEYA ÇOK DÜŞÜR ---
                    if (IsAiming)
                    {
                        // %60 daha az spread
                        currentSpread *= 0.4f;
                    }

                    if (currentSpread > 0f)
                    {
                        Vector3 randomSpreadOffset = Random.insideUnitSphere * currentSpread;
                        shootDirection += randomSpreadOffset;
                        shootDirection.Normalize();
                    }

                    CurrentAmmo--;

                    bool hit = false;

                    Vector3 hitPosition = raycastOrigin + (shootDirection * WeaponData.FireRange);
                    Vector3 hitNormal = Vector3.up;

                    // HitOptions parametresi ile normal duvarlara (PhysX) çarpmasını garanti altına alıyoruz
                    if (Runner.LagCompensation.Raycast(
                        raycastOrigin,
                        shootDirection,
                        WeaponData.FireRange,
                        Object.InputAuthority, // Kendi kendimizi vurmamızı engeller
                        out var hitResult,
                        LayerMask.GetMask("Player", "Default", "Ground", "Environment"),
                        HitOptions.IncludePhysX | HitOptions.IgnoreInputAuthority))
                    {
                        hit = true;
                        hitPosition = hitResult.Point;
                        hitNormal = hitResult.Normal;

                        // Eğer vurduğumuz şey bir Fusion Hitbox ise
                        if (hitResult.Hitbox != null)
                        {
                            // DİKKAT: Hitbox'ın bulunduğu objeden değil, onun bağlı olduğu KÖK (Root) objeden Player'ı arıyoruz!
                            var playerScript = hitResult.Hitbox.Root.GetComponent<Player>();

                            if (playerScript != null && Owner != null)
                            {
                                if(playerScript.PlayerTeam != Owner.PlayerTeam)
                                {
                                    playerScript.TakeDamage(WeaponData.Damage, Owner); // (Eğer fonksiyona Owner da eklediyseniz onu da yazabilirsiniz)
                                    Debug.Log("adama çarpıldı: ");
                                }
                                
                            }
                        }
                        // Eğer vurduğumuz şey Hitbox değil de normal bir duvarsa (PhysX)
                        else if (hitResult.Collider != null)
                        {
                            // Buraya duvara mermi izi (decal) veya kıvılcım efekti çıkaran kodlarını yazabilirsin
                            Debug.Log("Duvara veya çevreye çarpıldı: " + hitResult.Collider.name);
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
                ParticleSystem flash = Instantiate(MuzzleFlashParticle, weaponPoint.position, weaponPoint.rotation, weaponPoint);
                flash.Play();
                Destroy(flash.gameObject, 1f);
            }
            else
            {
                MuzzleFlashParticle.Play();
            }
        }

        if (BulletTrailPrefab != null && weaponPoint != null)
        {
            TrailRenderer trail = Instantiate(BulletTrailPrefab, weaponPoint.position, Quaternion.identity);
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
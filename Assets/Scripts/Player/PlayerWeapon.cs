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

    [Networked] public int CurrentAmmo { get; set; }
    [Networked] public int CurrentMags { get; set; }
    [Networked] public bool IsAiming { get; set; }

    [Networked] public Vector3 LastHitPosition { get; set; }
    [Networked] public Vector3 LastHitNormal { get; set; }
    [Networked] public bool LastShotDidHit { get; set; }

    [Networked] public TickTimer ReloadTimer { get; set; }
    [Networked] public bool IsReloading { get; set; }
    [Networked] public byte ReloadTriggered { get; set; } // Ağ üzerinden animasyonu tetiklemek için
                                                          // Networked property ekle (diğer [Networked]'lerin yanına)
    [Networked] public WeaponID EquippedWeaponID { get; set; }

    public Weapon WeaponData { get; private set; }

    [Header("Gerekli Referanslar")]
    public Transform weaponPoint;
    public Transform firePoint;
    public PlayerCamera playerCamera;
    public PlayerMovement playerMovement;

    public Vector2 CurrentShotRecoil;

    [Header("Görsel Efektler (VFX)")]
    public TrailRenderer BulletTrailPrefab;
    public ParticleSystem ImpactParticlePrefab;
    public ParticleSystem MuzzleFlashParticle;
    public float BulletTrailSpeed = 100f;

    [Header("Hasarlar")]
    public float HeadShotMultiplier = 3.0f;
    public float BodyShotMultiplier = 1.0f;
    public float BodyPartShotMultiplier = 0.7f;

    [System.Serializable]
    public struct WeaponMapping
    {
        public WeaponID WeaponID;
        public GameObject ViewmodelObject;   // 1. Şahıs Kollar (Eski sistem)
        public Transform ViewmodelMuzzlePoint;
        public GameObject ThirdPersonPrefab; // 3. Şahıs Fiziksel Prefab (PhysicalWeaponInfo içeren obje)
    }

    [Header("Silah ve Görsel Referansları")]
    public WeaponMapping[] WeaponMappings;
    public Transform ThirdPersonWeaponAnchor; // 3P_WeaponPoint buraya sürüklenecek
    public Transform viewmodelWeaponPoint; // YENİ: 1P Muzzle
    private GameObject _current3PWeaponInstance; // Elimizde tuttuğumuz fiziksel modelin anlık kaydı
    private Animator _currentViewmodelAnimator;

    private ChangeDetector _changeDetector;
    private float _gizmoHideTime;
    private bool _lastShotHit;
    private Vector3 _lastShootDirection;

    public Player Owner { get; set; }

    // Silahın ID'sine bakarak senin C# sınıflarını (Data) üreten metod
    public Weapon GetWeaponClassFromID(WeaponID id)
    {
        switch (id)
        {
            case WeaponID.AK47: return new AK47();
            case WeaponID.M4A4: return new M4A4();
            case WeaponID.MP5: return new MP5();
            case WeaponID.MP9: return new MP9();
            case WeaponID.Baretta92: return new Baretta92();
            default: return null;
        }
    }

    // Silahı Yere Atma Metodu
    public void DropCurrentWeapon()
    {
        if (WeaponData == null || WeaponData.ID == WeaponID.None) return;

        foreach (var mapping in WeaponMappings)
        {
            if (mapping.WeaponID == WeaponData.ID && mapping.ThirdPersonPrefab != null)
            {
                // Karakterin kamerasına göre hafif ileri bir pozisyon belirle
                Vector3 dropPosition = playerCamera.CameraPivot.position + playerCamera.CameraPivot.forward * 1f;

                // Orijinal prefabı ağ üzerinde YERE fırlat!
                Runner.Spawn(mapping.ThirdPersonPrefab.GetComponent<NetworkObject>(), dropPosition, Quaternion.identity, Object.InputAuthority, (runner, obj) =>
                {
                    // Doğmadan saniyeler önce kendi mermilerimizi yerdeki silaha aktarıyoruz
                    PhysicalWeaponInfo dropScript = obj.GetComponent<PhysicalWeaponInfo>();
                    if (dropScript != null)
                    {
                        dropScript.weaponID = WeaponData.ID;
                        dropScript.DroppedAmmo = CurrentAmmo;
                        dropScript.DroppedMags = CurrentMags;
                    }
                });

                // Kendi elimizi tamamen boşalt
                WeaponData = null;
                ActivateWeaponVisuals(WeaponID.None);

                // Animasyonu da silahsız moda (Idle) döndür
                if (playerMovement != null && playerMovement.BodyAnimator != null)
                    playerMovement.BodyAnimator.SetBool("IsAiming", false);

                break;
            }
        }
    }


    // EquipWeapon metodunu güncelle
    public void EquipWeapon(Weapon newWeaponModel)
    {
        WeaponData = newWeaponModel;

        if (Object != null && Object.HasStateAuthority)
        {
            CurrentAmmo = WeaponData.MagCapacity;
            CurrentMags = WeaponData.MagAmount;

            // Tüm clientlara silah değişikliğini haber ver
            EquippedWeaponID = WeaponData.ID;
        }

        ActivateWeaponVisuals(WeaponData.ID);
    }

    private void ActivateWeaponVisuals(WeaponID targetID)
    {
        // 1. Eski 3P silahı (varsa) elden sil
        if (_current3PWeaponInstance != null)
        {
            Destroy(_current3PWeaponInstance);
        }

        foreach (var mapping in WeaponMappings)
        {
            bool isEquipped = (mapping.WeaponID == targetID);

            // 2. 1P Görseli (Kolları) sadece bizdeyse (InputAuthority) yönet
            if (mapping.ViewmodelObject != null && HasInputAuthority)
            {
                mapping.ViewmodelObject.SetActive(isEquipped);
                if (isEquipped)
                {
                    _currentViewmodelAnimator = mapping.ViewmodelObject.GetComponent<Animator>();
                    if (_currentViewmodelAnimator != null)
                        _currentViewmodelAnimator.SetTrigger("Draw");

                    // YENİ EKLENTİ: Viewmodel namlusunu kaydet
                    viewmodelWeaponPoint = mapping.ViewmodelMuzzlePoint;
                }
            }

            // 3. 3P Görselini (Fiziksel Silahı) Herkeste Üret ve Ele (Anchor'a) Sabitle
            if (isEquipped && mapping.ThirdPersonPrefab != null && ThirdPersonWeaponAnchor != null)
            {
                // Silahı sağ ele kopyala
                _current3PWeaponInstance = Instantiate(mapping.ThirdPersonPrefab, ThirdPersonWeaponAnchor);

                // --- CRITICAL FIX: FİZİĞİ ANINDA FELÇ ET (Gecikmeyi engeller) ---
                Rigidbody rb = _current3PWeaponInstance.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = true;  // Anında fiziği kapat, animasyona boyun eğsin
                    rb.useGravity = false;  // Yerçekimini iptal et
                }

                BoxCollider col = _current3PWeaponInstance.GetComponent<BoxCollider>();
                if (col != null)
                {
                    col.enabled = false;    // Karakterin kapsülüyle çarpışmayı ANINDA kes
                }

                // Şimdi arka planda temizlenmeleri için güvenle emir verebiliriz
                Destroy(rb);
                Destroy(col);
                Destroy(_current3PWeaponInstance.GetComponent<NetworkObject>());

                // 4. IK ve VFX Referanslarını Oku ve Karaktere Kilitle
                PhysicalWeaponInfo pwi = _current3PWeaponInstance.GetComponent<PhysicalWeaponInfo>();
                if (pwi != null)
                {
                    weaponPoint = pwi.MuzzlePoint;

                    if (playerMovement != null)
                        playerMovement.CurrentWeaponLeftGrip = pwi.LeftGripPoint;
                }
            }
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
        if (playerCamera == null) playerCamera = GetComponent<PlayerCamera>();
        if (playerMovement == null) playerMovement = GetComponent<PlayerMovement>();
        Owner = GetComponent<Player>();
    }

    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInput input))
        {
            if (Owner != null && !Owner.IsAlive) return;
            if (GameManager.Instance == null || GameManager.Instance.CurrentState == RoundState.PreRound) return;

            bool interactPressed = input.Buttons.WasPressed(PreviousButtons, PlayerAction.Interact);
            bool dropPressed = input.Buttons.WasPressed(PreviousButtons, PlayerAction.DropWeapon);

            // --- 1. MANUEL SİLAH ATMA (G TUŞU) ---
            if (dropPressed && !IsReloading && WeaponData != null)
            {
                DropCurrentWeapon();
            }

            // --- 2. YERDEN SİLAH ALMA (E TUŞU) ---
            if (interactPressed && !IsReloading)
            {
                // Kameradan 3 metre ileriye etkileşim ışını yolla
                if (Runner.GetPhysicsScene().Raycast(playerCamera.CameraPivot.position, playerCamera.CameraPivot.forward, out RaycastHit interactHit, 3f, ~LayerMask.GetMask("Player")))
                {
                    PhysicalWeaponInfo dropObj = interactHit.collider.GetComponentInParent<PhysicalWeaponInfo>();

                    if (dropObj != null && dropObj.PickupDelay.ExpiredOrNotRunning(Runner))
                    {
                        // Elimizde zaten silah varsa, yeni silahı almadan önce eskisini yere fırlat
                        if (WeaponData != null)
                        {
                            DropCurrentWeapon();
                        }

                        // Yerdeki silahın ID'sini okuyup yeni C# veri sınıfını üret ve donan
                        Weapon newWeapon = GetWeaponClassFromID(dropObj.weaponID);
                        if (newWeapon != null)
                        {
                            EquipWeapon(newWeapon);

                            // Yerdeki mermi verilerini kendi üstümüze yaz
                            CurrentAmmo = dropObj.DroppedAmmo;
                            CurrentMags = dropObj.DroppedMags;

                            // İşimiz biten yerdeki modeli ağdan tamamen sil
                            Runner.Despawn(dropObj.Object);
                        }
                    }
                }
            }

            // GÜVENLİK DUVARI: Elimizde silah yoksa, ateş etme ve reload kodlarını hiç okuma!
            if (WeaponData == null)
            {
                IsAiming = false;
                if (playerCamera != null) playerCamera.HandleADS(false);
                PreviousButtons = input.Buttons;
                return;
            }

            // --- 3. RELOAD MANTIĞI ---
            if (IsReloading)
            {
                if (ReloadTimer.Expired(Runner))
                {
                    // Süre doldu, mermileri doldur
                    CurrentAmmo = WeaponData.MagCapacity;
                    CurrentMags--;
                    IsReloading = false;
                    ReloadTimer = TickTimer.None;
                }
                else
                {
                    // Reload yaparken ateş etme ve nişan alma yapılamaz
                    PreviousButtons = input.Buttons;
                    return;
                }
            }

            bool reloadPressed = input.Buttons.WasPressed(PreviousButtons, PlayerAction.Reload);

            if (reloadPressed && !IsReloading && CurrentMags > 0 && CurrentAmmo < WeaponData.MagCapacity)
            {
                IsReloading = true;
                ReloadTimer = TickTimer.CreateFromSeconds(Runner, WeaponData.ReloadTime);
                ReloadTriggered++;

                IsAiming = false;
                if (playerCamera != null) playerCamera.HandleADS(false);

                if (HasInputAuthority && _currentViewmodelAnimator != null)
                {
                    _currentViewmodelAnimator.SetTrigger("Reload");
                }

                PreviousButtons = input.Buttons;
                return;
            }

            // --- 4. NİŞAN ALMA VE ATEŞ MANTIĞI ---
            bool firePressed = input.Buttons.WasPressed(PreviousButtons, PlayerAction.Fire);
            bool fireHeld = input.Buttons.IsSet(PlayerAction.Fire);

            IsAiming = input.Buttons.IsSet(PlayerAction.Aim);

            if (HasInputAuthority && playerCamera != null)
            {
                playerCamera.HandleADS(IsAiming);
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

                if (shouldShoot && CurrentAmmo > 0)
                {
                    if (WeaponData.RecoilData != null && WeaponData.RecoilData.Length > 0)
                    {
                        CurrentShotRecoil = WeaponData.RecoilData[CurrentBulletIndex];

                        if (IsAiming) CurrentShotRecoil *= 0.5f;

                        if (playerCamera != null) playerCamera.ApplyRecoil(CurrentShotRecoil);

                        if (CurrentBulletIndex < WeaponData.RecoilData.Length - 1)
                            CurrentBulletIndex++;
                    }

                    Vector3 shootDirection = firePoint.forward;
                    Vector3 raycastOrigin = firePoint.position;

                    if (playerCamera != null)
                    {
                        shootDirection = playerCamera.GetShootDirection(transform);
                        float targetCamHeight = playerCamera.GetCurrentTargetHeight();
                        Vector3 exactLocalPos = new Vector3(0f, targetCamHeight, 0f);
                        raycastOrigin = transform.TransformPoint(exactLocalPos);
                    }

                    float currentSpeed = playerMovement != null ? playerMovement.Velocity.magnitude : 0f;
                    float currentSpread = WeaponData.BaseSpread + (currentSpeed * WeaponData.MovementSpreadMultiplier);
                    currentSpread = Mathf.Clamp(currentSpread, WeaponData.BaseSpread, WeaponData.MaxSpread);

                    if (IsAiming) currentSpread *= 0.4f;

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

                    if (Runner.LagCompensation.Raycast(
                        raycastOrigin,
                        shootDirection,
                        WeaponData.FireRange,
                        Object.InputAuthority,
                        out var hitResult,
                        LayerMask.GetMask("Player", "Default", "Ground", "Environment"),
                        HitOptions.IncludePhysX | HitOptions.IgnoreInputAuthority))
                    {
                        hit = true;
                        hitPosition = hitResult.Point;
                        hitNormal = hitResult.Normal;

                        if (hitResult.Hitbox != null)
                        {
                            var playerScript = hitResult.Hitbox.Root.GetComponent<Player>();

                            if (playerScript != null && Owner != null)
                            {
                                if (playerScript.PlayerTeam != Owner.PlayerTeam)
                                {
                                    float finalDamage = WeaponData.Damage;
                                    HitZoneType hitTag = hitResult.Hitbox.gameObject.GetComponent<HitboxProperties>().zone;

                                    switch (hitTag)
                                    {
                                        case HitZoneType.Head:
                                            finalDamage *= HeadShotMultiplier;
                                            break;
                                        case HitZoneType.BodyPart:
                                            finalDamage *= BodyPartShotMultiplier;
                                            break;
                                        case HitZoneType.Body:
                                            finalDamage *= BodyShotMultiplier;
                                            break;
                                        default:
                                            finalDamage *= BodyShotMultiplier;
                                            break;
                                    }

                                    playerScript.TakeDamage(finalDamage, Owner);
                                }
                            }
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

    // Render içine EquippedWeaponID değişikliğini dinle
    public override void Render()
    {
        foreach (var change in _changeDetector.DetectChanges(this))
        {
            switch (change)
            {
                case nameof(spawnedProjectile):
                    PlayVisualEffects();
                    break;

                case nameof(ReloadTriggered):
                    if (playerMovement != null && playerMovement.BodyAnimator != null)
                        playerMovement.BodyAnimator.SetTrigger("Reload");
                    break;

                // YENİ: Silah değişikliğini tüm clientlarda yakala
                case nameof(EquippedWeaponID):
                    // StateAuthority zaten EquipWeapon içinde ActivateWeaponVisuals'ı çağırdı,
                    // tekrar çağırmasın
                    if (!HasStateAuthority)
                    {
                        Weapon remoteWeapon = GetWeaponClassFromID(EquippedWeaponID);
                        if (remoteWeapon != null)
                        {
                            WeaponData = remoteWeapon;
                            ActivateWeaponVisuals(EquippedWeaponID);
                        }
                    }
                    break;
            }
        }
    }

    private void PlayVisualEffects()
    {
        // --- SİHİRLİ KISIM ---
        // Eğer karakter benimse Viewmodel namlusunu kullan, başkasınınsa 3P namlusunu kullan!
        Transform activeMuzzle = HasInputAuthority ? viewmodelWeaponPoint : weaponPoint;

        // Namlu bulunamadıysa hata vermemesi için güvenlik duvarı
        if (activeMuzzle == null) return;

        if (MuzzleFlashParticle != null)
        {
            if (MuzzleFlashParticle.gameObject.scene.name == null)
            {
                ParticleSystem flash = Instantiate(MuzzleFlashParticle, activeMuzzle.position, activeMuzzle.rotation, activeMuzzle);
                flash.Play();
                Destroy(flash.gameObject, 1f);
            }
            else
            {
                MuzzleFlashParticle.transform.position = activeMuzzle.position;
                MuzzleFlashParticle.transform.rotation = activeMuzzle.rotation;
                MuzzleFlashParticle.Play();
            }
        }

        if (BulletTrailPrefab != null)
        {
            TrailRenderer trail = Instantiate(BulletTrailPrefab, activeMuzzle.position, Quaternion.identity);
            StartCoroutine(SpawnTrailRoutine(trail, LastHitPosition, LastHitNormal, LastShotDidHit));
        }
    }

    private IEnumerator SpawnTrailRoutine(TrailRenderer trail, Vector3 hitPoint, Vector3 hitNormal, bool madeImpact)
    {
        Vector3 startPosition = trail.transform.position;
        float distance = Vector3.Distance(startPosition, hitPoint);

        if (distance < 0.1f) distance = 0.1f;

        float remainingDistance = distance;

        while (remainingDistance > 0)
        {
            if (trail == null) yield break;

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
using Fusion;
using UnityEngine;

public class PlayerCamera : NetworkBehaviour
{
    [Header("Kamera Sallanma (Head Bob)")]
    public float BobSpeed = 14f;
    public float BobAmount = 0.08f;
    private float _bobTimer = 0f;
    private float _baseCameraHeight;
    public float StandingCameraHeight = 1.6f;
    public float CrouchingCameraHeight = 0.8f;

    [Header("Referanslar")]
    public Transform CameraPivot;
    public PlayerMovement PlayerMovementScript;

    private float _currentPitch;

    [Header("Recoil (Sekme) Ayarları")]
    public float RecoilSmoothness = 15f;

    // YENİDEN EKLENDİ: Kameranın sarsıntıyı hissetme oranı. 
    // Örneğin 0.3 yaparsan mermi çok sapar ama kamera az oynar (CS:GO tarzı)
    public float RecoilScale = 2f;

    // Asıl ulaşmamız gereken sekme (Ağdan/Weapon'dan gelecek)
    private Vector2 _targetRecoil;
    // Ekranda anlık olarak görünen (Lerp edilen) yumuşak sekme
    private Vector2 _visualRecoil;

    public Player PlayerScript;

    public override void Spawned()
    {
        _baseCameraHeight = StandingCameraHeight;
        PlayerScript = GetComponentInParent<Player>();
    }

    // YENİDEN EKLENDİ: PlayerWeapon ateş ettiğinde kameraya sekmeyi bildirir
    public void ApplyRecoil(Vector2 recoilOffset)
    {
        // Kameraya sadece Scale kadarını (Örn: %30'unu) hissettir.
        _targetRecoil += recoilOffset * RecoilScale;
    }

    public override void FixedUpdateNetwork()
    {
        if (PlayerScript != null && !PlayerScript.IsAlive) return;
        if (GetInput(out NetworkInput input))
        {
            _currentPitch = input.LookPitch;
        }

        // Sekmeyi zamanla sıfıra çek
        _targetRecoil = Vector2.Lerp(_targetRecoil, Vector2.zero, Runner.DeltaTime * 5f);
    }

    public override void Render()
    {
        if (!HasInputAuthority || CameraPivot == null) return;

        // 1. RECOIL'İ YUMUŞAT
        _visualRecoil = Vector2.Lerp(_visualRecoil, _targetRecoil, Time.deltaTime * RecoilSmoothness);

        // 2. AÇILARI BİRLEŞTİR VE KAMERAYI DÖNDÜR
        float finalPitch = _currentPitch - _visualRecoil.y;

        CameraPivot.localRotation = Quaternion.Euler(finalPitch, _visualRecoil.x, 0);

        // 3. EĞİLME YÜKSEKLİĞİ
        float targetCamHeight = PlayerMovementScript.IsCrouching ? CrouchingCameraHeight : StandingCameraHeight;
        _baseCameraHeight = Mathf.Lerp(_baseCameraHeight, targetCamHeight, Time.deltaTime * PlayerMovementScript.CrouchTransitionSpeed);

        // 4. HEAD BOB
        float bobOffset = 0f;
        float currentSpeed = new Vector3(PlayerMovementScript.Velocity.x, 0, PlayerMovementScript.Velocity.z).magnitude;

        if (PlayerMovementScript.IsGrounded && PlayerMovementScript.IsSprinting && !PlayerMovementScript.IsCrouching && currentSpeed > 0.5f)
        {
            _bobTimer += Time.deltaTime * BobSpeed;
            bobOffset = Mathf.Sin(_bobTimer) * BobAmount;
        }
        else
        {
            _bobTimer = 0f;
        }

        // 5. POZİSYON UYGULAMA
        Vector3 camPos = CameraPivot.localPosition;
        camPos.y = Mathf.Lerp(camPos.y, _baseCameraHeight + bobOffset, Time.deltaTime * 15f);
        CameraPivot.localPosition = camPos;
    }

    // YENİDEN EKLENDİ: Merminin TAM olarak nereye (Crosshair'in üstüne vs.) gideceğini hesaplar.
    // Bu metodda RecoilScale kullanılmaz, çünkü mermiler desenin saf (raw) noktalarına gitmelidir.
    public Vector3 GetShootDirection(Transform characterTransform)
    {
        // Farenin pitch'i + hedeflenen recoil.
        float finalPitch = _currentPitch - _targetRecoil.y;

        // Karakterin Y eksenindeki dönüşü (sağ/sol) + Recoil'in X ekseni
        float finalYaw = characterTransform.eulerAngles.y + _targetRecoil.x;

        // Euler açılarını bir yön vektörüne (forward) çevir
        return Quaternion.Euler(finalPitch, finalYaw, 0) * Vector3.forward;
    }
}
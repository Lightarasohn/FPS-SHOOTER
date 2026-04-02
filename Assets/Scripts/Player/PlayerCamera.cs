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
    public float RecoilScale = 2f;

    // Asıl ulaşmamız gereken sekme (Ağdan/Weapon'dan gelecek)
    private Vector2 _targetRecoil;
    // Ekranda anlık olarak görünen (Lerp edilen) yumuşak sekme
    private Vector2 _visualRecoil;

    public override void Spawned()
    {
        // Artık Camera kapatma işlemleri yok, PlayerInputHandler zaten Camera.main'i bize bağlıyor.
        _baseCameraHeight = StandingCameraHeight;
    }

    // PlayerWeapon ateş ettiğinde bu metodu çağıracak
    public void ApplyRecoil(Vector2 recoilOffset)
    {
        _targetRecoil += recoilOffset * RecoilScale;
    }

    // BACKEND (Saniyede 64 kez - Ağdan gelen güncel pitch (yukarı/aşağı) bilgisini al)
    public override void FixedUpdateNetwork()
    {
        // Not: Otorite kontrolüne gerek yok, GetInput sadece Input Authority'ye sahipsen true döner.
        if (GetInput(out NetworkInput input))
        {
            _currentPitch = input.LookPitch;
        }

        // --- RECOIL SIFIRLANMASI (Sadece ateş edilmediğinde yavaşça merkeze dön) ---
        // Not: Bu sıfırlama hızını (5f) silahın özelliğine göre değiştirebilirsin
        _targetRecoil = Vector2.Lerp(_targetRecoil, Vector2.zero, Runner.DeltaTime * 5f);
    }

    // FRONTEND (Monitörün Hz hızında, örn: Saniyede 144 kez - Pürüzsüz görsel işlemler)
    public override void Render()
    {
        // Kameramız yoksa veya bizim karakterimiz değilse animasyonları boşuna hesaplama
        if (!HasInputAuthority || CameraPivot == null) return;

        // 1. ROTASYON (Pürüzsüz dönüş - Ağdan alınan LookPitch değeriyle)
        // 1. RECOIL'İ YUMUŞAT
        _visualRecoil = Vector2.Lerp(_visualRecoil, _targetRecoil, Time.deltaTime * RecoilSmoothness);

        // 2. AÇILARI BİRLEŞTİR VE KAMERAYI DÖNDÜR
        // Farenin saf açısına, görsel sekmenin Y eksenini (Yukarı tepme) ekliyoruz.
        // Eksi (-) kullanıyoruz çünkü Pitch'te eksi değerler yukarı bakmayı sağlar.
        float finalPitch = _currentPitch - _visualRecoil.y;

        CameraPivot.localRotation = Quaternion.Euler(finalPitch, _visualRecoil.x, 0);

        // 2. EĞİLME YÜKSEKLİĞİ
        float targetCamHeight = PlayerMovementScript.IsCrouching ? CrouchingCameraHeight : StandingCameraHeight;
        _baseCameraHeight = Mathf.Lerp(_baseCameraHeight, targetCamHeight, Time.deltaTime * PlayerMovementScript.CrouchTransitionSpeed);

        // 3. HEAD BOB (Sallanma animasyonu)
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

        // 4. POZİSYON UYGULAMA
        Vector3 camPos = CameraPivot.localPosition;
        camPos.y = Mathf.Lerp(camPos.y, _baseCameraHeight + bobOffset, Time.deltaTime * 15f);
        CameraPivot.localPosition = camPos;
    }

    // YENİ: PlayerWeapon mermiyi fırlatırken kameranın TAM olarak nereye baktığını bilmek ister.
    // Raycast için gerekli olan "Sekme Dahil" yönü hesaplar.
    public Vector3 GetShootDirection(Transform characterTransform)
    {
        // Farenin pitch'i + hedeflenen recoil.
        // Burada görsel (visual) recoil değil, target recoil kullanıyoruz ki sunucu gecikmesiz hesaplayabilsin.
        float finalPitch = _currentPitch - _targetRecoil.y;

        // Karakterin Y eksenindeki dönüşü (sağ/sol) + Recoil'in X ekseni
        float finalYaw = characterTransform.eulerAngles.y + _targetRecoil.x;

        // Bu Euler açılarını bir yön vektörüne (forward) çevir
        return Quaternion.Euler(finalPitch, finalYaw, 0) * Vector3.forward;
    }
}
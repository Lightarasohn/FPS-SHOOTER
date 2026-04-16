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

    // Asıl ulaşmamız gereken sekme (Ağdan/Weapon'dan gelecek)
    private Vector2 _targetRecoil;
    // Ekranda anlık olarak görünen (Lerp edilen) yumuşak sekme
    private Vector2 _visualRecoil;

    public override void Spawned()
    {
        // Artık Camera kapatma işlemleri yok, PlayerInputHandler zaten Camera.main'i bize bağlıyor.
        _baseCameraHeight = StandingCameraHeight;
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
        _visualRecoil = Vector2.Lerp(_visualRecoil, _targetRecoil, Time.deltaTime * 15f);
        float noiseStrength = 0.4f; // ANA KONTROL NOKTASI

        float noiseX = (Mathf.PerlinNoise(Time.time * 10f, 0f) - 0.5f) * _visualRecoil.magnitude * noiseStrength;
        float noiseY = (Mathf.PerlinNoise(0f, Time.time * 10f) - 0.5f) * _visualRecoil.magnitude * noiseStrength;
        // 2. AÇILARI BİRLEŞTİR VE KAMERAYI DÖNDÜR
        // Farenin saf açısına, görsel sekmenin Y eksenini (Yukarı tepme) ekliyoruz.
        // Eksi (-) kullanıyoruz çünkü Pitch'te eksi değerler yukarı bakmayı sağlar.
        float finalPitch = _currentPitch;

        float shakeX = _visualRecoil.y * 0.2f; // küçük dikey titreme
        float shakeY = _visualRecoil.x;        // sağ-sol

        CameraPivot.localRotation = Quaternion.Euler(finalPitch + noiseX,noiseY,0);

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
    public void AddRecoil(Vector2 recoil)
    {
        // Yukarı recoil'i azalt, daha çok jitter yap
        recoil.y *= 0.3f;

        _targetRecoil += recoil;
    }
}
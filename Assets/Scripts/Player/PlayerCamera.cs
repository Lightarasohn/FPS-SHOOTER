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
    }

    // FRONTEND (Monitörün Hz hızında, örn: Saniyede 144 kez - Pürüzsüz görsel işlemler)
    public override void Render()
    {
        // Kameramız yoksa veya bizim karakterimiz değilse animasyonları boşuna hesaplama
        if (!HasInputAuthority || CameraPivot == null) return;

        // 1. ROTASYON (Pürüzsüz dönüş - Ağdan alınan LookPitch değeriyle)
        CameraPivot.localRotation = Quaternion.Euler(_currentPitch, 0, 0);

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
}
using Fusion;
using UnityEngine;

public class PlayerCamera : NetworkBehaviour
{
    [Header("Kamera Sallanma (Head Bob)")]
    public float BobSpeed = 14f;
    public float BobAmount = 0.08f;
    private float _bobTimer = 0f;
    private float _baseCameraHeight;

    private float _standingCameraHeight;
    private float _crouchingCameraHeight;

    [Header("Referans Noktaları")]
    public Transform CameraPivot;
    public Transform CrouchingCamPoint;
    public PlayerMovement PlayerMovementScript;

    [Header("ADS (Nişan Alma) Ayarları")]
    public float NormalFOV = 60f;
    public float AimFOV = 40f;
    public float ADSSpeed = 15f;

    private Camera _cam;
    private bool _isAiming;
    private float _currentPitch;

    [Header("Recoil (Sekme) Ayarları")]
    public float RecoilSmoothness = 15f;
    public float RecoilScale = 2f;

    [Networked] private Vector2 _targetRecoil { get; set; }
    private Vector2 _visualRecoil;
    public Player PlayerScript;

    public override void Spawned()
    {
        PlayerScript = GetComponentInParent<Player>();
        _cam = GetComponentInChildren<Camera>();
        if (_cam != null) NormalFOV = _cam.fieldOfView;

        if (CameraPivot != null) _standingCameraHeight = CameraPivot.localPosition.y;
        if (CrouchingCamPoint != null) _crouchingCameraHeight = CrouchingCamPoint.localPosition.y;

        _baseCameraHeight = _standingCameraHeight;
    }

    public void HandleADS(bool isAiming)
    {
        _isAiming = isAiming;
    }

    public void ApplyRecoil(Vector2 recoilOffset)
    {
        _targetRecoil += recoilOffset * RecoilScale;
    }

    public override void FixedUpdateNetwork()
    {
        if (PlayerScript != null && !PlayerScript.IsAlive) return;

        if (GetInput(out NetworkInput input))
        {
            _currentPitch = input.LookPitch;
        }
        _targetRecoil = Vector2.Lerp(_targetRecoil, Vector2.zero, Runner.DeltaTime * 5f);
    }

    public override void Render()
    {
        if (!HasInputAuthority || CameraPivot == null) return;

        _visualRecoil = Vector2.Lerp(_visualRecoil, _targetRecoil, Time.deltaTime * RecoilSmoothness);
        float finalPitch = _currentPitch - _visualRecoil.y;
        CameraPivot.localRotation = Quaternion.Euler(finalPitch, _visualRecoil.x, 0);

        float targetCamHeight = PlayerMovementScript.IsCrouching ? _crouchingCameraHeight : _standingCameraHeight;
        _baseCameraHeight = Mathf.Lerp(_baseCameraHeight, targetCamHeight, Time.deltaTime * PlayerMovementScript.CrouchTransitionSpeed);

        float bobOffset = 0f;
        float currentSpeed = new Vector3(PlayerMovementScript.Velocity.x, 0, PlayerMovementScript.Velocity.z).magnitude;

        if (PlayerMovementScript.IsGrounded && !PlayerMovementScript.IsCrouching && currentSpeed > 0.5f)
        {
            // Hıza göre sallanma hızını dinamik yapabilirsin (İsteğe bağlı)
            // Koşarken daha hızlı, yürürken daha yavaş sallanır
            float dynamicBobSpeed = PlayerMovementScript.IsSprinting ? BobSpeed * 1.5f : BobSpeed;

            _bobTimer += Time.deltaTime * dynamicBobSpeed;
            bobOffset = Mathf.Sin(_bobTimer) * BobAmount;
        }
        else
        {
            _bobTimer = 0f;
        }

        Vector3 camPos = CameraPivot.localPosition;
        camPos.y = Mathf.Lerp(camPos.y, _baseCameraHeight + bobOffset, Time.deltaTime * 15f);
        CameraPivot.localPosition = camPos;

        if (_cam != null)
        {
            float targetFOV = _isAiming ? AimFOV : NormalFOV;
            _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, targetFOV, Time.deltaTime * ADSSpeed);
        }
    }

    public Vector3 GetShootDirection(Transform characterTransform)
    {
        float finalPitch = _currentPitch - _targetRecoil.y;
        float finalYaw = characterTransform.eulerAngles.y + _targetRecoil.x;
        return Quaternion.Euler(finalPitch, finalYaw, 0) * Vector3.forward;
    }

    public float GetCurrentTargetHeight()
    {
        return PlayerMovementScript.IsCrouching ? _crouchingCameraHeight : _standingCameraHeight;
    }
}
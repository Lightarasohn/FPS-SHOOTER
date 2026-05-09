using Fusion;
using UnityEngine;
using static GlobalVariables;

public class PlayerMovement : NetworkBehaviour
{
    [Header("Hareket Ayarları (CS:GO Strafe Değerleri)")]
    public float MaxGroundSpeed = 5f;
    public float MaxAirSpeed = 5f; // Havada yerdeki kadar hızlı gidebilsin
    public float AirAcceleration = 5f; // ESKİSİ 5'Tİ. Şimdi havada anında yön değiştirecek!
    public float MaxFallingSpeed = -32f;
    public float GroundAcceleration = 10f;
    [Networked] public float Friction { get; set; } = 8f;
    [Networked] public float Gravity { get; set; } = 18f;
    public float JumpForce = 7.8f;

    [Header("Eğilme (Crouch) Ayarları")]
    public float StandingHeight = 2f;
    public float CrouchHeight = 1.5f;
    public float CrouchSpeedMultiplier = 0.5f;
    public float CrouchTransitionSpeed = 10f;

    // Koşma Ayarları
    [Header("Koşma (Sprint) Ayarları")]
    public float SprintSpeedMultiplier = 1.5f;
    public bool IsSprinting = false;

    // Kayma Ayarları (Slide)
    [Header("Kayma (Slide) Ayarları")]
    public float SlideDuration = 2f; // MADDE 3: Yerde maksimum 2 saniye kalsın
    public float SlideSpeedMultiplier = 2f;
    public float SlideCooldownTime = 0.5f;

    [Header("Referanslar")]
    public Transform PlayerPivot;

    // Kapsülün dış görünüşünü temsil eden obje
    public Transform PlayerVisualBody;

    [Networked] public Vector3 Velocity { get; set; }
    [Networked] public bool IsGrounded { get; set; }
    [Networked] public bool IsCrouching { get; set; }

    // Ağ üzerinden senkronize edilecek kayma değişkenleri
    [Networked] public bool IsSliding { get; set; }
    [Networked] public TickTimer SlideTimer { get; set; }
    [Networked] public TickTimer SlideCooldown { get; set; }
    [Networked] public Vector3 SlideDirection { get; set; }

    // Kapsül boyutunu artık sabit değil, dinamik yapıyoruz
    private float _capsuleHeight;
    private float _capsuleRadius = 0.35f;

    public override void Spawned()
    {
        _capsuleHeight = StandingHeight;
    }

    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInput input))
        {
            // --- KAMERA VE DÖNÜŞ (Look) ---
            transform.rotation = Quaternion.Euler(0, input.LookYaw, 0);

            // --- OYUN DURUMU KONTROLÜ (Hareket edebilir miyiz?) ---
            bool canMove = true;
            if (GameManager.Instance != null && GameManager.Instance.CurrentState == RoundState.PreRound)
            {
                canMove = false;
            }

            // Girdileri al
            bool wantsToCrouch = canMove && input.Buttons.IsSet(PlayerAction.Crouch);
            IsSprinting = canMove && input.Buttons.IsSet(PlayerAction.sprint);

            // Sadece ileri doğru (Y ekseninde) gidiyorsa kayabilsin
            bool isMovingForward = input.MoveDirection.y > 0;

            // --- MADDE 1: İLERİ KOŞARKEN EĞİLDİĞİ DURUMDA KAYMA BAŞLAT ---
            if (canMove && IsGrounded && IsSprinting && isMovingForward && wantsToCrouch && !IsCrouching && !IsSliding && SlideCooldown.ExpiredOrNotRunning(Runner))
            {
                IsSliding = true;
                SlideTimer = TickTimer.CreateFromSeconds(Runner, SlideDuration); // 2 saniyelik süreci başlat

                Vector3 currentMoveDir = new Vector3(Velocity.x, 0, Velocity.z);
                if (currentMoveDir.magnitude > 0.1f)
                    SlideDirection = currentMoveDir.normalized;
                else
                    SlideDirection = transform.forward;
            }

            // --- MADDE 3: ZIPLAMAZSA YERDE MAX 2 SANİYE KAYAR ---
            if (IsSliding)
            {
                // Artık oyuncu elini C tuşundan çekse bile kayma İPTAL OLMAYACAK! Sadece süre dolarsa bitecek.
                if (SlideTimer.Expired(Runner))
                {
                    IsSliding = false;
                    SlideTimer = TickTimer.None;
                    SlideCooldown = TickTimer.CreateFromSeconds(Runner, SlideCooldownTime);
                }
            }

            // Kayma iptal olmadıysa ve devam ediyorsa, oyuncu tuşu bıraksa bile kod onu zorla eğik (crouch) tutar
            if (IsSliding)
            {
                wantsToCrouch = true;
            }

            // --- TAVAN KONTROLÜ ---
            if (!wantsToCrouch && IsCrouching)
            {
                if (CheckCeiling()) wantsToCrouch = true;
            }

            IsCrouching = wantsToCrouch;

            // --- KAPSÜL BOYUNU AYARLA ---
            float targetHeight = IsCrouching ? CrouchHeight : StandingHeight;
            _capsuleHeight = Mathf.Lerp(_capsuleHeight, targetHeight, Runner.DeltaTime * CrouchTransitionSpeed);

            // --- GÖRSELİ KÜÇÜLT, HİZALA VE KAYARKEN EĞ ---
            if (PlayerVisualBody != null)
            {
                float targetScaleY = _capsuleHeight / StandingHeight;
                PlayerVisualBody.localScale = new Vector3(1f, targetScaleY, 1f);

                float yOffset = (_capsuleHeight - StandingHeight) / 2f;
                PlayerVisualBody.localPosition = new Vector3(0f, yOffset, 0f);

                if (IsSliding)
                {
                    Quaternion targetRotation = Quaternion.Euler(60f, 0f, 0f);
                    PlayerVisualBody.localRotation = Quaternion.Lerp(PlayerVisualBody.localRotation, targetRotation, Runner.DeltaTime * 10f);
                }
                else
                {
                    PlayerVisualBody.localRotation = Quaternion.Lerp(PlayerVisualBody.localRotation, Quaternion.identity, Runner.DeltaTime * 10f);
                }
            }

            // --- FİZİK VE HAREKET HESAPLAMASI ---
            Vector3 currentVelocity = Velocity;
            CheckGrounded(ref currentVelocity);

            // Uçurumdan düşersek kaymayı anında iptal et ve bekleme süresi koy
            if (!IsGrounded && IsSliding)
            {
                IsSliding = false;
                SlideTimer = TickTimer.None;
                SlideCooldown = TickTimer.CreateFromSeconds(Runner, SlideCooldownTime);
            }

            Vector3 rawInputDirection = Vector3.zero;
            if (canMove)
            {
                rawInputDirection = transform.forward * input.MoveDirection.y + transform.right * input.MoveDirection.x;
                rawInputDirection.Normalize();
            }

            Vector3 wishDir = rawInputDirection;

            if (IsSliding)
            {
                wishDir = SlideDirection;
            }

            if (IsGrounded)
            {
                ApplyFriction(ref currentVelocity, Runner.DeltaTime);

                float currentMaxSpeed = MaxGroundSpeed;

                if (IsSliding) currentMaxSpeed = MaxGroundSpeed * SlideSpeedMultiplier;
                else if (IsCrouching) currentMaxSpeed = MaxGroundSpeed * CrouchSpeedMultiplier;
                else if (IsSprinting) currentMaxSpeed = MaxGroundSpeed * SprintSpeedMultiplier;

                Accelerate(ref currentVelocity, wishDir, currentMaxSpeed, GroundAcceleration, Runner.DeltaTime);

                // --- MADDE 2: SADECE ZIPLADIĞINDA EĞİLME İPTALİ ---
                if (canMove && input.Buttons.IsSet(PlayerAction.Jump))
                {
                    if (IsSliding)
                    {
                        IsSliding = false;           // Kaymayı iptal et
                        wantsToCrouch = false;       // Eğilme isteğini anında kes
                        IsCrouching = false;         // Karakterin dikilmesini sağla

                        SlideTimer = TickTimer.None;
                        SlideCooldown = TickTimer.CreateFromSeconds(Runner, SlideCooldownTime);

                        // Kaymanın o güzel hızını zıplamaya (Momentum) aktar!
                        if (rawInputDirection.magnitude > 0.1f)
                        {
                            float slideSpeed = new Vector3(currentVelocity.x, 0, currentVelocity.z).magnitude;
                            currentVelocity.x = rawInputDirection.x * slideSpeed;
                            currentVelocity.z = rawInputDirection.z * slideSpeed;
                        }
                    }

                    currentVelocity.y = JumpForce;
                    IsGrounded = false;
                }
            }
            else
            {
                Accelerate(ref currentVelocity, wishDir, MaxAirSpeed, AirAcceleration, Runner.DeltaTime);

                if (currentVelocity.y <= MaxFallingSpeed)
                    currentVelocity.y = MaxFallingSpeed;
                else
                    currentVelocity.y -= Gravity * Runner.DeltaTime;
            }

            // --- ÇARPIŞMA VE POZİSYON GÜNCELLEMESİ ---
            Vector3 motion = currentVelocity * Runner.DeltaTime;
            Vector3 newPosition = transform.position + motion;

            newPosition = ResolveCollisions(transform.position, newPosition, ref currentVelocity);

            transform.position = newPosition;
            Velocity = currentVelocity;
        }
    }

    private bool CheckCeiling()
    {
        Vector3 origin = PlayerPivot.position + Vector3.up * _capsuleHeight;
        float distanceToStand = StandingHeight - _capsuleHeight;

        return Runner.GetPhysicsScene().SphereCast(origin, _capsuleRadius, Vector3.up, out _, distanceToStand, ~LayerMask.GetMask("Player"));
    }

    private void ApplyFriction(ref Vector3 velocity, float deltaTime)
    {
        float speed = new Vector3(velocity.x, 0, velocity.z).magnitude;
        if (speed < 0.1f)
        {
            velocity.x = 0;
            velocity.z = 0;
            return;
        }

        float drop = speed * Friction * deltaTime;
        float newSpeed = speed - drop;
        if (newSpeed < 0) newSpeed = 0;

        newSpeed /= speed;
        velocity.x *= newSpeed;
        velocity.z *= newSpeed;
    }

    private void Accelerate(ref Vector3 velocity, Vector3 wishDir, float wishSpeed, float accel, float deltaTime)
    {
        float currentSpeed = Vector3.Dot(new Vector3(velocity.x, 0, velocity.z), wishDir);
        float addSpeed = wishSpeed - currentSpeed;

        if (addSpeed <= 0) return;

        float accelSpeed = accel * deltaTime * wishSpeed;
        if (accelSpeed > addSpeed) accelSpeed = addSpeed;

        velocity.x += accelSpeed * wishDir.x;
        velocity.z += accelSpeed * wishDir.z;
    }

    private void CheckGrounded(ref Vector3 currentVel)
    {
        Vector3 origin = PlayerPivot.position + (Vector3.up * (_capsuleRadius + 0.05f));
        float checkRadius = _capsuleRadius + 0.02f;

        IsGrounded = Runner.GetPhysicsScene().SphereCast(origin, checkRadius, Vector3.down, out RaycastHit hitInfo, (_capsuleRadius + 0.1f), ~LayerMask.GetMask("Player"));

        if (IsGrounded)
        {
            if (hitInfo.normal.y > 0.5f)
            {
                if (currentVel.y < 0)
                {
                    currentVel.y = 0;
                }
            }
            else
            {
                IsGrounded = false;
            }
        }
    }

    private Vector3 ResolveCollisions(Vector3 startPos, Vector3 targetPos, ref Vector3 currentVelocity)
    {
        Vector3 currentPos = startPos;
        int maxBounces = 3;
        float skinWidth = 0.015f;
        Vector3 originalVelocity = currentVelocity;

        for (int i = 0; i < maxBounces; i++)
        {
            Vector3 direction = targetPos - currentPos;
            float distance = direction.magnitude;

            if (distance < 0.001f) break;

            Vector3 p1 = currentPos + Vector3.up * _capsuleRadius;
            Vector3 p2 = currentPos + Vector3.up * (_capsuleHeight - _capsuleRadius);
            float castRadius = _capsuleRadius - 0.01f;

            if (Runner.GetPhysicsScene().CapsuleCast(p1, p2, castRadius, direction.normalized, out RaycastHit hit, distance + skinWidth, ~LayerMask.GetMask("Player")))
            {
                float safeDistance = Mathf.Max(0f, hit.distance - skinWidth);
                currentPos += direction.normalized * safeDistance;

                Vector3 remainingDirection = direction.normalized * (distance - safeDistance);
                Vector3 slideVector = Vector3.ProjectOnPlane(remainingDirection, hit.normal);

                targetPos = currentPos + slideVector;

                Vector3 newVelocity = Vector3.ProjectOnPlane(currentVelocity, hit.normal);

                if (!IsGrounded && newVelocity.y > originalVelocity.y)
                {
                    newVelocity.y = originalVelocity.y;
                }

                currentVelocity = newVelocity;
            }
            else
            {
                currentPos = targetPos;
                break;
            }
        }

        return currentPos;
    }

    private void OnDrawGizmos()
    {
        if (PlayerPivot == null) return;

        bool grounded = false;
        if (Application.isPlaying && Object != null && Object.IsInSimulation)
        {
            grounded = IsGrounded;
        }

        if (grounded) Gizmos.color = Color.green;
        else Gizmos.color = Color.red;

        Vector3 origin = PlayerPivot.position + (Vector3.up * (_capsuleRadius + 0.01f));
        Gizmos.DrawWireSphere(origin, _capsuleRadius);
        Gizmos.DrawLine(origin, origin + Vector3.down * (_capsuleRadius + 0.05f));

        Gizmos.color = Color.blue;
        Vector3 p1 = PlayerPivot.position + Vector3.up * _capsuleRadius;
        Vector3 p2 = PlayerPivot.position + Vector3.up * (_capsuleHeight - _capsuleRadius);
        Gizmos.DrawWireSphere(p1, _capsuleRadius);
        Gizmos.DrawWireSphere(p2, _capsuleRadius);
        Gizmos.DrawLine(p1 + Vector3.left * _capsuleRadius, p2 + Vector3.left * _capsuleRadius);
        Gizmos.DrawLine(p1 + Vector3.right * _capsuleRadius, p2 + Vector3.right * _capsuleRadius);
    }
}
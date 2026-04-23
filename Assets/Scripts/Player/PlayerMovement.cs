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
    public float Friction = 5f;
    public float Gravity = 20f;
    public float JumpForce = 6f;

    [Header("Eğilme (Crouch) Ayarları")]
    public float StandingHeight = 2f;
    public float CrouchHeight = 1f;
    public float CrouchSpeedMultiplier = 0.5f;
    public float CrouchTransitionSpeed = 10f;

    // Koşma Ayarları
    [Header("Koşma (Sprint) Ayarları")]
    public float SprintSpeedMultiplier = 1.5f;
    public bool IsSprinting = false;

    // Kayma Ayarları (Slide)
    [Header("Kayma (Slide) Ayarları")]
    public float SlideDuration = 1f;
    public float SlideSpeedMultiplier = 2f;

    [Header("Referanslar")]
    public Transform PlayerPivot;

    [Networked] public Vector3 Velocity { get; set; }
    [Networked] public bool IsGrounded { get; set; }
    [Networked] public bool IsCrouching { get; set; }

    // Ağ üzerinden senkronize edilecek kayma değişkenleri
    [Networked] public bool IsSliding { get; set; }
    [Networked] public TickTimer SlideTimer { get; set; }
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

            // --- GİRDİLER ---
            bool wantsToCrouch = input.Buttons.IsSet(PlayerAction.Crouch);
            IsSprinting = input.Buttons.IsSet(PlayerAction.sprint);

            // --- KAYMA (SLIDE) BAŞLATMA MANTIĞI ---
            if (IsGrounded && IsSprinting && wantsToCrouch && !IsCrouching && !IsSliding && SlideTimer.ExpiredOrNotRunning(Runner))
            {
                IsSliding = true;
                SlideTimer = TickTimer.CreateFromSeconds(Runner, SlideDuration);

                Vector3 currentMoveDir = new Vector3(Velocity.x, 0, Velocity.z);
                if (currentMoveDir.magnitude > 0.1f)
                    SlideDirection = currentMoveDir.normalized;
                else
                    SlideDirection = transform.forward;
            }

            if (IsSliding && SlideTimer.Expired(Runner))
            {
                IsSliding = false;
            }

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

            // --- FİZİK VE HAREKET HESAPLAMASI ---
            Vector3 currentVelocity = Velocity;
            CheckGrounded(ref currentVelocity);

            // YENİ HATA ÇÖZÜMÜ: Eğer kayarken uçurumdan düşersek veya zıplarsak havada yön kilidini kır!
            if (!IsGrounded && IsSliding)
            {
                IsSliding = false;
                SlideTimer = TickTimer.None;
            }

            // Ham yön bilgisini (oyuncunun bastığı tuşları) alıyoruz
            Vector3 rawInputDirection = transform.forward * input.MoveDirection.y + transform.right * input.MoveDirection.x;
            rawInputDirection.Normalize();

            Vector3 wishDir = rawInputDirection;

            // Kayarken normal yönü kilitliyoruz (Artık sadece yerdeysek çalışacak)
            if (IsSliding)
            {
                wishDir = SlideDirection;
            }

            if (IsGrounded)
            {
                ApplyFriction(ref currentVelocity, Runner.DeltaTime);

                float currentMaxSpeed = MaxGroundSpeed;

                if (IsSliding)
                {
                    currentMaxSpeed = MaxGroundSpeed * SlideSpeedMultiplier;
                }
                else if (IsCrouching)
                {
                    currentMaxSpeed = MaxGroundSpeed * CrouchSpeedMultiplier;
                }
                else if (IsSprinting)
                {
                    currentMaxSpeed = MaxGroundSpeed * SprintSpeedMultiplier;
                }

                Accelerate(ref currentVelocity, wishDir, currentMaxSpeed, GroundAcceleration, Runner.DeltaTime);

                // Zıplama ve Momentum Yönlendirmesi
                if (input.Buttons.IsSet(PlayerAction.Jump))
                {
                    if (IsSliding)
                    {
                        IsSliding = false;
                        SlideTimer = TickTimer.None;

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
                // Havadayken (Artık yüksek MaxAirSpeed ve AirAcceleration sayesinde çok daha rahat kontrol edilecek)
                Accelerate(ref currentVelocity, wishDir, MaxAirSpeed, AirAcceleration, Runner.DeltaTime);

                // Yerçekimi
                if (currentVelocity.y <= MaxFallingSpeed)
                    currentVelocity.y = MaxFallingSpeed;
                else
                    currentVelocity.y -= Gravity * Runner.DeltaTime;
            }

            // --- ÇARPIŞMA (COLLISION) VE POZİSYON GÜNCELLEMESİ ---
            Vector3 motion = currentVelocity * Runner.DeltaTime;
            Vector3 newPosition = transform.position + motion;

            newPosition = ResolveCollisions(transform.position, newPosition, ref currentVelocity);

            transform.position = newPosition;
            Velocity = currentVelocity;
        }
    }

    // --- TAVAN KONTROLÜ (Kafayı vurmamak için) ---
    private bool CheckCeiling()
    {
        Vector3 origin = PlayerPivot.position + Vector3.up * _capsuleHeight;
        float distanceToStand = StandingHeight - _capsuleHeight;

        return Runner.GetPhysicsScene().SphereCast(origin, _capsuleRadius, Vector3.up, out _, distanceToStand, ~LayerMask.GetMask("Player"));
    }

    // --- QUAKE/SOURCE MOTORU HAREKET MATEMATİĞİ ---
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

    // --- ÖZEL ÇARPIŞMA SİSTEMİ (CUSTOM COLLISION) ---
    private void CheckGrounded(ref Vector3 currentVel)
    {
        Vector3 origin = PlayerPivot.position + (Vector3.up * (_capsuleRadius + 0.01f));
        IsGrounded = Runner.GetPhysicsScene().SphereCast(origin, _capsuleRadius, Vector3.down, out _, (_capsuleRadius + 0.05f), ~LayerMask.GetMask("Player"));

        if (IsGrounded && currentVel.y < 0)
        {
            currentVel.y = 0;
        }
    }

    private Vector3 ResolveCollisions(Vector3 startPos, Vector3 endPos, ref Vector3 currentVelocity)
    {
        Vector3 direction = endPos - startPos;
        float distance = direction.magnitude;

        if (distance < 0.001f) return startPos;

        Vector3 p1 = PlayerPivot.position + Vector3.up * _capsuleRadius;
        Vector3 p2 = PlayerPivot.position + Vector3.up * (_capsuleHeight - _capsuleRadius);

        if (Runner.GetPhysicsScene().CapsuleCast(p1, p2, _capsuleRadius, direction.normalized, out RaycastHit hit, distance, ~LayerMask.GetMask("Player")))
        {
            Vector3 safePos = startPos + direction.normalized * (hit.distance - 0.001f);
            Vector3 remainingDistance = direction.normalized * (distance - hit.distance);
            Vector3 slideDirection = Vector3.ProjectOnPlane(remainingDistance, hit.normal);
            currentVelocity = Vector3.ProjectOnPlane(currentVelocity, hit.normal);

            return safePos + slideDirection;
        }

        return endPos;
    }

    private void OnDrawGizmos()
    {
        if (PlayerPivot == null) return;

        bool grounded = false;
        if (Application.isPlaying && Object != null && Object.IsInSimulation)
        {
            grounded = IsGrounded;
        }

        if (grounded)
            Gizmos.color = Color.green;
        else
            Gizmos.color = Color.red;

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
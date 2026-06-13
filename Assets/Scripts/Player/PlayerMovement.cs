using Fusion;
using UnityEngine;
using static GlobalVariables;

public class PlayerMovement : NetworkBehaviour
{
    [Header("Hareket Ayarları")]
    public float MaxGroundSpeed = 5f;
    public float MaxAirSpeed = 5f;
    public float AirAcceleration = 5f;
    public float MaxFallingSpeed = -32f;
    public float GroundAcceleration = 10f;
    [Networked] public float Friction { get; set; } = 8f;
    [Networked] public float Gravity { get; set; } = 18f;
    public float JumpForce = 7.8f;

    [Header("Eğilme (Crouch) Ayarları")]
    public float CrouchSpeedMultiplier = 0.5f;
    public float CrouchTransitionSpeed = 10f;

    [Header("Koşma (Sprint) Ayarları")]
    public float SprintSpeedMultiplier = 1.5f;
    public bool IsSprinting = false;

    [Header("Nişan Alma (ADS) Ayarları")]
    public float ADSSpeedMultiplier = 0.5f;

    [Header("Kayma (Slide) Ayarları")]
    public float SlideDuration = 1f;
    public float SlideSpeedMultiplier = 2f;
    public float SlideCooldownTime = 0.5f;

    [Header("Kusursuz Referans Noktaları")]
    public Transform CharFootPoint;
    public Transform CharHeadPoint;
    public Transform CrouchingHeadPoint;

    [Header("Animasyon")]
    public Animator BodyAnimator;

    [Header("Rigging & IK (Nişan ve Sol El)")]
    public Transform AimTarget;
    public float AimDistance = 50f;
    public Transform LeftHandIK_Target;
    public Transform CurrentWeaponLeftGrip;

    [Header("SFX")]
    public PlayerAudioHandler AudioHandler;

    [Networked] public float NetworkPitch { get; set; }

    [Networked] public Vector3 Velocity { get; set; }
    [Networked] public bool IsGrounded { get; set; }
    [Networked] public bool IsCrouching { get; set; }
    [Networked] public bool IsSliding { get; set; }
    [Networked] public TickTimer SlideTimer { get; set; }
    [Networked] public TickTimer SlideCooldown { get; set; }
    [Networked] public Vector3 SlideDirection { get; set; }

    [Networked] public byte JumpTriggered { get; set; }
    [Networked] public byte LandTriggered { get; set; } // YENİ: Yere düşme tetikleyicisi

    private float _standingHeight;
    private float _crouchHeight;
    private float _capsuleHeight;
    private float _capsuleRadius = 0.35f;
    private Player _playerScript;
    private ChangeDetector _animChangeDetector;

    private float _footstepTimer;
    private float _baseStepInterval = 0.45f;

    private void Awake()
    {
        if (CharHeadPoint != null && CharFootPoint != null && CrouchingHeadPoint != null)
        {
            _standingHeight = CharHeadPoint.localPosition.y - CharFootPoint.localPosition.y;
            _crouchHeight = CrouchingHeadPoint.localPosition.y - CharFootPoint.localPosition.y;
            _capsuleHeight = _standingHeight;
        }
    }

    public override void Spawned()
    {
        _animChangeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
        _playerScript = GetComponent<Player>();

        if (CharHeadPoint == null || CharFootPoint == null || CrouchingHeadPoint == null)
        {
            Debug.LogError("[PlayerMovement] Referans noktaları atanmamış!");
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInput input))
        {
            transform.rotation = Quaternion.Euler(0, input.LookYaw, 0);

            NetworkPitch = input.LookPitch;

            bool canMove = _playerScript.IsAlive;
            if (GameManager.Instance != null && GameManager.Instance.CurrentState == RoundState.PreRound)
            {
                canMove = false;
            }

            bool isAiming = canMove && input.Buttons.IsSet(PlayerAction.Aim);
            bool wantsToCrouch = canMove && input.Buttons.IsSet(PlayerAction.Crouch);
            IsSprinting = canMove && input.Buttons.IsSet(PlayerAction.sprint) && !isAiming;
            bool isMovingForward = input.MoveDirection.y > 0;

            if (canMove && IsGrounded && IsSprinting && isMovingForward && wantsToCrouch && !IsCrouching && !IsSliding && SlideCooldown.ExpiredOrNotRunning(Runner))
            {
                IsSliding = true;
                SlideTimer = TickTimer.CreateFromSeconds(Runner, SlideDuration);

                Vector3 currentMoveDir = new Vector3(Velocity.x, 0, Velocity.z);
                if (currentMoveDir.magnitude > 0.1f)
                    SlideDirection = currentMoveDir.normalized;
                else
                    SlideDirection = transform.forward;
            }

            if (IsSliding)
            {
                if (SlideTimer.Expired(Runner))
                {
                    IsSliding = false;
                    SlideTimer = TickTimer.None;
                    SlideCooldown = TickTimer.CreateFromSeconds(Runner, SlideCooldownTime);
                }
            }

            if (IsSliding) wantsToCrouch = true;

            if (!wantsToCrouch && IsCrouching)
            {
                if (CheckCeiling()) wantsToCrouch = true;
            }

            IsCrouching = wantsToCrouch;

            float targetHeight = IsCrouching ? _crouchHeight : _standingHeight;
            _capsuleHeight = Mathf.Lerp(_capsuleHeight, targetHeight, Runner.DeltaTime * CrouchTransitionSpeed);

            // --- YENİ EKLENEN DÜŞME KONTROLÜ ---
            bool wasGrounded = IsGrounded; // Kontrol öncesi durumu kaydet
            Vector3 currentVelocity = Velocity;

            CheckGrounded(ref currentVelocity); // Yere değip değmediğini hesapla

            // Eğer az önce havadaydıysa ve şimdi yere değdiyse (Düştü!)
            if (!wasGrounded && IsGrounded)
            {
                LandTriggered++; // Yere çarpma sesini tüm ağa gönder
            }
            // ------------------------------------

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
            if (IsSliding) wishDir = SlideDirection;

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
                    if (isAiming) currentMaxSpeed *= ADSSpeedMultiplier;
                }
                else if (IsSprinting)
                {
                    currentMaxSpeed = MaxGroundSpeed * SprintSpeedMultiplier;
                }
                else if (isAiming)
                {
                    currentMaxSpeed *= ADSSpeedMultiplier;
                }

                Accelerate(ref currentVelocity, wishDir, currentMaxSpeed, GroundAcceleration, Runner.DeltaTime);

                if (canMove && input.Buttons.IsSet(PlayerAction.Jump))
                {
                    if (IsSliding)
                    {
                        IsSliding = false;
                        wantsToCrouch = false;
                        IsCrouching = false;
                        SlideTimer = TickTimer.None;
                        SlideCooldown = TickTimer.CreateFromSeconds(Runner, SlideCooldownTime);

                        if (rawInputDirection.magnitude > 0.1f)
                        {
                            float slideSpeed = new Vector3(currentVelocity.x, 0, currentVelocity.z).magnitude;
                            currentVelocity.x = rawInputDirection.x * slideSpeed;
                            currentVelocity.z = rawInputDirection.z * slideSpeed;
                        }
                    }

                    currentVelocity.y = JumpForce;
                    IsGrounded = false;
                    JumpTriggered++;
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

            Vector3 motion = currentVelocity * Runner.DeltaTime;
            Vector3 newPosition = transform.position + motion;

            newPosition = ResolveCollisions(transform.position, newPosition, ref currentVelocity);

            transform.position = newPosition;
            Velocity = currentVelocity;
        }
    }

    private bool CheckCeiling()
    {
        Vector3 origin = CharFootPoint.position + Vector3.up * _capsuleHeight;
        float distanceToStand = _standingHeight - _capsuleHeight;

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
        Vector3 origin = CharFootPoint.position + (Vector3.up * (_capsuleRadius + 0.05f));
        float checkRadius = _capsuleRadius + 0.02f;

        IsGrounded = Runner.GetPhysicsScene().SphereCast(origin, checkRadius, Vector3.down, out RaycastHit hitInfo, (_capsuleRadius + 0.1f), ~LayerMask.GetMask("Player", "Weapon"));

        if (IsGrounded)
        {
            if (hitInfo.normal.y > 0.5f)
            {
                if (currentVel.y < 0) currentVel.y = 0;
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

        Vector3 pivotOffset = CharFootPoint.position - transform.position;

        for (int i = 0; i < maxBounces; i++)
        {
            Vector3 direction = targetPos - currentPos;
            float distance = direction.magnitude;

            if (distance < 0.001f) break;

            Vector3 basePos = currentPos + pivotOffset;
            Vector3 p1 = basePos + Vector3.up * _capsuleRadius;
            Vector3 p2 = basePos + Vector3.up * (_capsuleHeight - _capsuleRadius);
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

    public override void Render()
    {
        if (BodyAnimator == null) return;

        foreach (var change in _animChangeDetector.DetectChanges(this))
        {
            switch (change)
            {
                case nameof(IsSliding):
                    if (IsSliding)
                    {
                        BodyAnimator.SetTrigger("Slide");
                        // YENİ: Kaymaya başladığı an sesi oynat
                        if (AudioHandler != null) AudioHandler.PlaySlide();
                    }
                    break;

                case nameof(JumpTriggered):
                    if (AudioHandler != null) AudioHandler.PlayJump();
                    break;

                // YENİ: Yere düştüğünü ağdan algıla ve sesi oynat
                case nameof(LandTriggered):
                    if (AudioHandler != null) AudioHandler.PlayLand();
                    break;
            }
        }

        Vector3 localVelocity = transform.InverseTransformDirection(Velocity);
        float maxSpeed = MaxGroundSpeed * SprintSpeedMultiplier;

        float targetMoveX = localVelocity.x / maxSpeed;
        float targetMoveY = localVelocity.z / maxSpeed;

        float currentMoveX = BodyAnimator.GetFloat("MoveX");
        float currentMoveY = BodyAnimator.GetFloat("MoveY");

        BodyAnimator.SetFloat("MoveX", Mathf.Lerp(currentMoveX, targetMoveX, Time.deltaTime * 15f));
        BodyAnimator.SetFloat("MoveY", Mathf.Lerp(currentMoveY, targetMoveY, Time.deltaTime * 15f));

        BodyAnimator.SetBool("IsCrouching", IsCrouching);
        BodyAnimator.SetBool("IsGrounded", IsGrounded);

        if (_playerScript != null && _playerScript.EquippedWeapon != null)
        {
            BodyAnimator.SetBool("IsAiming", _playerScript.EquippedWeapon.IsAiming);
        }

        if (AimTarget != null && CharHeadPoint != null)
        {
            Quaternion aimRotation = Quaternion.Euler(NetworkPitch, transform.eulerAngles.y, 0);
            AimTarget.position = CharHeadPoint.position + aimRotation * Vector3.forward * AimDistance;
        }

        if (LeftHandIK_Target != null && CurrentWeaponLeftGrip != null)
        {
            LeftHandIK_Target.position = CurrentWeaponLeftGrip.position;
            LeftHandIK_Target.rotation = CurrentWeaponLeftGrip.rotation;
        }

        // Ayak Sesi
        if (IsGrounded && !IsSliding)
        {
            float currentSpeed = new Vector3(Velocity.x, 0, Velocity.z).magnitude;

            if (currentSpeed > 0.5f)
            {
                _footstepTimer -= Time.deltaTime;

                if (_footstepTimer <= 0)
                {
                    if (AudioHandler != null) AudioHandler.PlayFootstep();

                    if (IsSprinting)
                        _footstepTimer = _baseStepInterval * 0.7f;
                    else if (IsCrouching)
                        _footstepTimer = _baseStepInterval * 1.5f;
                    else
                        _footstepTimer = _baseStepInterval;
                }
            }
            else
            {
                _footstepTimer = 0f;
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (CharFootPoint == null || CharHeadPoint == null) return;

        bool grounded = false;
        if (Application.isPlaying && Object != null && Object.IsInSimulation) grounded = IsGrounded;

        Gizmos.color = grounded ? Color.green : Color.red;

        Vector3 origin = CharFootPoint.position + (Vector3.up * (_capsuleRadius + 0.01f));
        Gizmos.DrawWireSphere(origin, _capsuleRadius);
        Gizmos.DrawLine(origin, origin + Vector3.down * (_capsuleRadius + 0.05f));

        Gizmos.color = Color.blue;
        float tempHeight = Application.isPlaying ? _capsuleHeight : (CharHeadPoint.localPosition.y - CharFootPoint.localPosition.y);

        Vector3 p1 = CharFootPoint.position + Vector3.up * _capsuleRadius;
        Vector3 p2 = CharFootPoint.position + Vector3.up * (tempHeight - _capsuleRadius);
        Gizmos.DrawWireSphere(p1, _capsuleRadius);
        Gizmos.DrawWireSphere(p2, _capsuleRadius);
        Gizmos.DrawLine(p1 + Vector3.left * _capsuleRadius, p2 + Vector3.left * _capsuleRadius);
        Gizmos.DrawLine(p1 + Vector3.right * _capsuleRadius, p2 + Vector3.right * _capsuleRadius);
    }
}
using Fusion;
using UnityEngine;

public class PlayerMovement : NetworkBehaviour
{
    [Header("Hareket Ayarları (CS:GO Değerleri)")]
    public float MaxGroundSpeed = 5f;
    public float MaxAirSpeed = 0.5f; // Havada çok hızlı yön değiştirmeyi engeller (Air Strafing)
    public float GroundAcceleration = 10f;
    public float AirAcceleration = 2f;
    public float Friction = 5f;
    public float Gravity = 20f;
    public float JumpForce = 6f;

    [Header("Referanslar")]
    public Transform CameraPivot; // Kameranın yukarı/aşağı bakması için boyun noktası

    // Networked değişkenler: Sunucu bunları geçmişe sarıp (rewind) tekrar hesaplayabilir!
    [Networked] public Vector3 Velocity { get; set; }
    [Networked] public bool IsGrounded { get; set; }

    // Çarpışma (Collision) tespiti için kapsül boyutları
    private float _capsuleHeight = 2f;
    private float _capsuleRadius = 0.35f;

    public override void FixedUpdateNetwork()
    {
        // 1. İstemciden gelen "O anki" veya "Geçmişteki" girdiyi güvenli bir şekilde al
        if (GetInput(out NetworkInput input))
        {
            // 2. KAMERA VE DÖNÜŞ (Look)
            // Karakterin tamamını sağa/sola döndür
            transform.rotation = Quaternion.Euler(0, input.LookYaw, 0);

            // Sadece kamerayı (boynu) yukarı/aşağı döndür
            if (CameraPivot != null)
            {
                CameraPivot.localRotation = Quaternion.Euler(input.LookPitch, 0, 0);
            }

            // 3. FİZİK VE HAREKET HESAPLAMASI (Quake/Source Matematiği)
            Vector3 currentVelocity = Velocity;

            // Yerde miyiz kontrolü
            CheckGrounded();

            // Girdi vektörünü dünyanın 3D yönüne çevir
            Vector3 wishDir = transform.forward * input.MoveDirection.y + transform.right * input.MoveDirection.x;
            wishDir.Normalize();

            if (IsGrounded)
            {
                // Yerdeyken sürtünme uygula (Counter-strafing hissiyatı için anında durmayı sağlar)
                ApplyFriction(ref currentVelocity, Runner.DeltaTime);

                // Yerde hızlanma
                Accelerate(ref currentVelocity, wishDir, MaxGroundSpeed, GroundAcceleration, Runner.DeltaTime);

                // Zıplama
                if (input.Buttons.IsSet(PlayerAction.Jump))
                {
                    currentVelocity.y = JumpForce;
                    IsGrounded = false;
                }
            }
            else
            {
                // Havadayken sürtünme yok, air-strafing için düşük ivmelenme var
                Accelerate(ref currentVelocity, wishDir, MaxAirSpeed, AirAcceleration, Runner.DeltaTime);

                // Yerçekimi
                currentVelocity.y -= Gravity * Runner.DeltaTime;
            }

            // 4. ÇARPIŞMA (COLLISION) VE POZİSYON GÜNCELLEMESİ
            Vector3 motion = currentVelocity * Runner.DeltaTime;
            Vector3 newPosition = transform.position + motion;

            // Kendi yazdığımız basit Duvar/Zemin kayma (Slide) mekaniği
            newPosition = ResolveCollisions(transform.position, newPosition, ref currentVelocity);

            // Sonuçları uygula ve ağa bildir
            transform.position = newPosition;
            Velocity = currentVelocity;
        }
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

    private void CheckGrounded()
    {
        // Karakterin ayak hizasından aşağıya küçük bir küre yollayarak yeri kontrol et
        Vector3 origin = transform.position + (Vector3.up * 0.1f);
        IsGrounded = Physics.SphereCast(origin, _capsuleRadius, Vector3.down, out _, 0.15f, ~LayerMask.GetMask("Player"));

        if (IsGrounded && Velocity.y < 0)
        {
            Vector3 vel = Velocity;
            vel.y = 0; // Yere değdiğimizde düşüş hızını sıfırla
            Velocity = vel;
        }
    }

    private Vector3 ResolveCollisions(Vector3 startPos, Vector3 endPos, ref Vector3 currentVelocity)
    {
        // İlerleyeceğimiz yöne kapsül fırlat (Sweep Test). Eğer duvar varsa boylu boyunca kay (Slide).
        Vector3 p1 = startPos + Vector3.up * _capsuleRadius;
        Vector3 p2 = startPos + Vector3.up * (_capsuleHeight - _capsuleRadius);
        Vector3 direction = endPos - startPos;
        float distance = direction.magnitude;

        if (Physics.CapsuleCast(p1, p2, _capsuleRadius, direction.normalized, out RaycastHit hit, distance, ~LayerMask.GetMask("Player")))
        {
            // Duvara çarptık. Duvarın normaline (yüzey yönüne) göre hızımızı kırp (Slide)
            Vector3 slideDirection = Vector3.ProjectOnPlane(direction, hit.normal);

            // Hız vektörünü de duvara göre düzelt ki duvara takılı kalmayalım
            currentVelocity = Vector3.ProjectOnPlane(currentVelocity, hit.normal);

            return startPos + slideDirection;
        }

        return endPos; // Çarpışma yoksa gitmek istediğimiz yere git
    }
}
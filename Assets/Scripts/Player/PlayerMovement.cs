using Fusion;
using UnityEngine;

public class PlayerMovement : NetworkBehaviour
{
    [Header("Hareket Ayarları (CS:GO Değerleri)")]
    public float MaxGroundSpeed = 5f;
    public float MaxAirSpeed = 0.5f; // Havada çok hızlı yön değiştirmeyi engeller (Air Strafing)
    public float MaxFallingSpeed = -32f;
    public float GroundAcceleration = 10f;
    public float AirAcceleration = 2f;
    public float Friction = 5f;
    public float Gravity = 20f;
    public float JumpForce = 6f;

    [Header("Referanslar")]
    public Transform CameraPivot; // Kameranın yukarı/aşağı bakması için boyun noktası
    public Transform PlayerPivot;

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
            CheckGrounded(ref currentVelocity);

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
                if (currentVelocity.y <= MaxFallingSpeed)
                    currentVelocity.y = MaxFallingSpeed;
                else
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

    private void CheckGrounded(ref Vector3 currentVel)
    {
        // DÜZELTME 1 (Tünellemeye Karşı): Tarama başlangıcını daha yukarı (0.2f) alıyoruz.
        Vector3 origin = PlayerPivot.position + (Vector3.up * (_capsuleRadius + 0.01f));

        // DÜZELTME 2 (Tünellemeye Karşı): Aşağıya doğru çok daha derin (0.5f) tarama yapıyoruz.
        // Böylece karakter tek bir karede hızlı düşse bile zemini kaçırmaz.
        IsGrounded = Runner.GetPhysicsScene().SphereCast(origin, _capsuleRadius, Vector3.down, out _, (_capsuleRadius + 0.05f), ~LayerMask.GetMask("Player"));

        if (IsGrounded && currentVel.y < 0)
        {
            currentVel.y = 0; // Hızı referans üzerinden sıfırla
        }
    }

    private Vector3 ResolveCollisions(Vector3 startPos, Vector3 endPos, ref Vector3 currentVelocity)
    {
        Vector3 direction = endPos - startPos;
        float distance = direction.magnitude;

        // HATA 2 ÇÖZÜMÜ: Hareket çok küçükse veya yoksa çarpışma hesaplama
        if (distance < 0.001f) return startPos;

        // DÜZELTİLEN KISIM BURASI: Kapsülü belden değil, PlayerPivot'tan (ayaklardan) çizmeye başlıyoruz!
        Vector3 p1 = PlayerPivot.position + Vector3.up * _capsuleRadius;
        Vector3 p2 = PlayerPivot.position + Vector3.up * (_capsuleHeight - _capsuleRadius);

        if (Runner.GetPhysicsScene().CapsuleCast(p1, p2, _capsuleRadius, direction.normalized, out RaycastHit hit, distance, ~LayerMask.GetMask("Player")))
        {
            // Önce çarptığımız yüzeyin hemen dibine kadar güvenli bir şekilde git
            // Not: Buradaki startPos kalmalı, çünkü hareket ettirdiğimiz ana obje o.
            Vector3 safePos = startPos + direction.normalized * (hit.distance - 0.001f);

            // Sonra kalan hareket mesafesini yüzeyin normaline göre kaydır (Slide)
            Vector3 remainingDistance = direction.normalized * (distance - hit.distance);
            Vector3 slideDirection = Vector3.ProjectOnPlane(remainingDistance, hit.normal);

            // Hız vektörünü de duvara göre düzelt
            currentVelocity = Vector3.ProjectOnPlane(currentVelocity, hit.normal);

            return safePos + slideDirection;
        }

        return endPos;
    }

    private void OnDrawGizmos()
    {
        if (PlayerPivot == null) return;

        // Networked property'lere yalnızca obje spawnlandıysa eriş
        bool grounded = false;
        if (Application.isPlaying && Object != null && Object.IsInSimulation)
        {
            grounded = IsGrounded;
        }

        // --- DÜZELTME 3 (Görsel Geri Bildirim): Zemindeyken Yeşil, Havadayken Kırmızı Çiz ---
        if (grounded)
            Gizmos.color = Color.green; // Zemini bulduysak yeşil yap
        else
            Gizmos.color = Color.red; // Havadaysak kırmızı yap



        // Yeri kontrol eden küreyi çiz
        Vector3 origin = PlayerPivot.position + (Vector3.up * (_capsuleRadius + 0.01f)); // Kodla aynı yapıyoruz
        Gizmos.DrawWireSphere(origin, _capsuleRadius);
        Gizmos.DrawLine(origin, origin + Vector3.down * (_capsuleRadius + 0.05f)); // Yere attığı ışın derinliğini de kodla aynı yapıyoruz

        // Çarpışma Kapsülünü her zaman Mavi çiz
        Gizmos.color = Color.blue;
        Vector3 p1 = PlayerPivot.position + Vector3.up * _capsuleRadius;
        Vector3 p2 = PlayerPivot.position + Vector3.up * (_capsuleHeight - _capsuleRadius);
        Gizmos.DrawWireSphere(p1, _capsuleRadius);
        Gizmos.DrawWireSphere(p2, _capsuleRadius);
        Gizmos.DrawLine(p1 + Vector3.left * _capsuleRadius, p2 + Vector3.left * _capsuleRadius);
        Gizmos.DrawLine(p1 + Vector3.right * _capsuleRadius, p2 + Vector3.right * _capsuleRadius);
    }
}
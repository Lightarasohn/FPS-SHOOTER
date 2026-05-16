using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;
using static GlobalVariables;

public class PlayerInputHandler : NetworkBehaviour
{
    [Header("Kamera Ayarları")]
    public float MouseSensitivity = 0.1f;

    [Header("Gelişmiş Fare (ADS) Ayarları")]
    public bool EnableSmoothness;
    // Yumuşatma hızı: Değer ne kadar yüksekse fare o kadar keskin/tepkisel olur. 
    // Düşük değerler fareyi buzda kayıyormuş gibi hissettirir. (Genelde 15-30 arası iyidir)
    public float SmoothnessSpeed;
    private Vector2 _smoothedMouseDelta;

    public bool EnableAcceleration;
    // İvmelenmenin devreye gireceği minimum fare hızı (Çok yavaş hareketlerde ivme olmasın diye)
    public float AccelerationThreshold;
    public float AccelerationMultiplier;
    public float MaxAcceleration; // Hassasiyetin en fazla kaç katına çıkabileceği sınırı

    public NetworkInput CurrentInput;

    public void OnInput(NetworkInput input, NetworkRunner runner)
    {
        input.Set(CurrentInput);
    }

    public override void Spawned()
    {
        if (HasInputAuthority)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            MouseSettings settings = PlayerSaveManager.LoadMouseSettings();
            if (settings != null) {
                UpdateMouseSettings(settings);
            }
        }
    }

    public void UpdateMouseSettings(MouseSettings newMouseSettings)
    {
        MouseSensitivity = newMouseSettings.MouseSensitivity;
        EnableSmoothness = newMouseSettings.EnableSmoothness;
        SmoothnessSpeed = newMouseSettings.SmoothnessSpeed;
        EnableAcceleration = newMouseSettings.EnableAcceleration;
        AccelerationThreshold = newMouseSettings.AccelerationThreshold;
        AccelerationMultiplier = newMouseSettings.AccelerationMultiplier;
        MaxAcceleration = newMouseSettings.MaxAcceleration;
    }

    private void Update()
    {
        if (HasInputAuthority == false) return;

        if (Keyboard.current == null || Mouse.current == null) return;

        if (Cursor.lockState != CursorLockMode.Locked)
        {
            CurrentInput.MoveDirection = Vector2.zero;
            CurrentInput.Buttons = default;
            return;
        }

        // 1. Yön Tuşları
        Vector2 move = Vector2.zero;
        if (Keyboard.current.wKey.isPressed) move.y += 1;
        if (Keyboard.current.sKey.isPressed) move.y -= 1;
        if (Keyboard.current.dKey.isPressed) move.x += 1;
        if (Keyboard.current.aKey.isPressed) move.x -= 1;

        CurrentInput.MoveDirection = move.normalized;

        // 2. Kamera Açıları (GELİŞMİŞ FARE HESAPLAMALARI)
        Vector2 rawMouseDelta = Mouse.current.delta.ReadValue();
        Vector2 processedDelta = rawMouseDelta;

        // --- İVMELENME (ACCELERATION) ---
        if (EnableAcceleration)
        {
            float speed = rawMouseDelta.magnitude;

            // Eğer fareyi belirli bir hızın üzerinde çevirdiyse
            if (speed > AccelerationThreshold)
            {
                // Ne kadar hızlı çevirdiyse o kadar ekstra çarpan ekle
                float extraAccel = (speed - AccelerationThreshold) * AccelerationMultiplier;

                // Çarpanı maksimum sınırda tut (fareyi ışık hızında çevirirse oyun bozulmasın diye)
                float finalMultiplier = Mathf.Clamp(1f + extraAccel, 1f, MaxAcceleration);

                // Fare verisini ivme ile çarp
                processedDelta *= finalMultiplier;
            }
        }

        // --- YUMUŞATMA (SMOOTHNESS) ---
        if (EnableSmoothness)
        {
            // Eski fare verisi ile yeni fare verisi arasında zamana bağlı geçiş yap
            _smoothedMouseDelta = Vector2.Lerp(_smoothedMouseDelta, processedDelta, Time.deltaTime * SmoothnessSpeed);
            processedDelta = _smoothedMouseDelta;
        }
        else
        {
            _smoothedMouseDelta = processedDelta;
        }

        // --- SONUCU UYGULA ---
        CurrentInput.LookYaw += processedDelta.x * MouseSensitivity;
        CurrentInput.LookPitch -= processedDelta.y * MouseSensitivity;

        CurrentInput.LookPitch = Mathf.Clamp(CurrentInput.LookPitch, -89f, 89f);

        // 3. Butonlar
        CurrentInput.Buttons.Set(PlayerAction.Jump, Keyboard.current.spaceKey.isPressed);
        CurrentInput.Buttons.Set(PlayerAction.Crouch, Keyboard.current.leftCtrlKey.isPressed);
        CurrentInput.Buttons.Set(PlayerAction.sprint, Keyboard.current.leftShiftKey.isPressed);
        CurrentInput.Buttons.Set(PlayerAction.Fire, Mouse.current.leftButton.isPressed);
        CurrentInput.Buttons.Set(PlayerAction.Reload, Keyboard.current.rKey.isPressed);
        CurrentInput.Buttons.Set(PlayerAction.Aim, Mouse.current.rightButton.isPressed);
    }
}
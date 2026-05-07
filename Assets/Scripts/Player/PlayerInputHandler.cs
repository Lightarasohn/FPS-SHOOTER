using ExitGames.Client.Photon.StructWrapping;
using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;
using static GlobalVariables;

public class PlayerInputHandler : NetworkBehaviour
{
    [Header("Kamera Ayarları")]
    // DİKKAT: Yeni sistemde fare hareketi ham piksel olarak gelir, bu yüzden eskiye göre çok daha düşük bir değer verdik (Örn: 2.0 yerine 0.1)
    public float MouseSensitivity = 0.1f;

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
        }
    }

    private void Update()
    {
        if (HasInputAuthority == false) return;

        // GÜVENLİK: Eğer bilgisayara klavye veya fare takılı değilse kodun çökmesini engeller
        if (Keyboard.current == null || Mouse.current == null) return;

        // --- YENİ EKLENEN KISIM: MENÜ AÇIKKEN KARAKTERİ DONDURMA SÜZGECİ ---
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            // Menüdeyken karakterin yürümesini ve ateş etmesini engelle
            CurrentInput.MoveDirection = Vector2.zero;
            CurrentInput.Buttons = default;

            // Update döngüsünü burada kes, aşağıdaki tuş okuma işlemlerine geçme!
            return;
        }
        // -------------------------------------------------------------------

        // 1. Yön Tuşları (Yeni Sistem: Doğrudan tuşların donanım durumunu okuyoruz)
        Vector2 move = Vector2.zero;
        if (Keyboard.current.wKey.isPressed) move.y += 1;
        if (Keyboard.current.sKey.isPressed) move.y -= 1;
        if (Keyboard.current.dKey.isPressed) move.x += 1;
        if (Keyboard.current.aKey.isPressed) move.x -= 1;

        // Çapraz gidişlerde hızı dengelemek için normalize ediyoruz
        CurrentInput.MoveDirection = move.normalized;

        // 2. Kamera Açıları (Yeni Sistem: Farenin donanımsal delta/fark hareketini okuyoruz)
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        CurrentInput.LookYaw += mouseDelta.x * MouseSensitivity;
        CurrentInput.LookPitch -= mouseDelta.y * MouseSensitivity;

        // Boyun kırma engeli
        CurrentInput.LookPitch = Mathf.Clamp(CurrentInput.LookPitch, -89f, 89f);

        // 3. Butonlar (Yeni Sistem: Doğrudan tuş atamaları)
        CurrentInput.Buttons.Set(PlayerAction.Jump, Keyboard.current.spaceKey.isPressed);
        CurrentInput.Buttons.Set(PlayerAction.Crouch, Keyboard.current.leftCtrlKey.isPressed);
        CurrentInput.Buttons.Set(PlayerAction.sprint, Keyboard.current.leftShiftKey.isPressed);
        CurrentInput.Buttons.Set(PlayerAction.Fire, Mouse.current.leftButton.isPressed);
        CurrentInput.Buttons.Set(PlayerAction.Reload, Keyboard.current.rKey.isPressed);
    }
}
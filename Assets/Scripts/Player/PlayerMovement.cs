using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : NetworkBehaviour
{
    // PlayerPrefab İÇİNDEKİ "NETWORK TRANSFORM" KULLANILACAK, SERVER İLE HABERLEŞEBİLMESİ İÇİN.
    /*
     * Yürüyüşe başlama keskin
     * Yürüyüş sonunda ters yöne basılmamışsa hızlıca yavaşlayarak durma
     * Yürüyüş yapılırken ters yöne basılmışsa 
       -AYNI ANDA İKİ YÖNE DE BASILABİLİR, BİR YÖNE GİDERKEN GİTMEYİ BIRAKIP TERS YÖNE DE BASILABİLİR-
       anında durma (Counter Strafing)
     * Shift tuşuna koşma 
     *  space Zıplama
     * Yer ve Hava süratleri farklı (sv_accelerate ve sv_airaccelerate)
     * Air strafe
     * shift+ctrl kayma 
     * CTRL = EĞİLME 
     * akla geldikçe eklenebilir.
    */

    [Header("Movement")]
    public float moveSpeed = 6f;

    public Transform orientation;

    float horizontalInput;
    float verticalInput;

    Vector3 moveDirection;
    Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
    }

    private void Update()
    {
        //if (!HasInputAuthority) return; Karakter spawn edilirken açılcak

        MyInput();
    }

    private void FixedUpdate()
    {
        //if (!HasInputAuthority) return;

        MovePlayer();
    }

    private void MyInput()
    {
        Vector2 input = Keyboard.current != null ?
            new Vector2(
                (Keyboard.current.dKey.isPressed ? 1 : 0) - (Keyboard.current.aKey.isPressed ? 1 : 0),
                (Keyboard.current.wKey.isPressed ? 1 : 0) - (Keyboard.current.sKey.isPressed ? 1 : 0)
            ) : Vector2.zero;

        horizontalInput = input.x;
        verticalInput = input.y;
    }

    private void MovePlayer()
    {
        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;

        rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);
    }
}
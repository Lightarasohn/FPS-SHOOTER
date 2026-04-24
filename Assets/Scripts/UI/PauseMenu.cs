using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    [Header("UI Referansları")]
    public GameObject pauseMenuPanel;
    public Button disconnectButton;

    [Header("Ayarlar")]
    public int mainMenuSceneIndex = 0; // Disconnect olunca dönülecek sahnenin Build Index'i

    private bool _isMenuOpen = false;

    void Start()
    {   
        pauseMenuPanel.SetActive(false);
 
        disconnectButton.onClick.AddListener(DisconnectFromGame);
    }

    void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ToggleMenu();
        }
    }

    void ToggleMenu()
    {
        _isMenuOpen = !_isMenuOpen;
        pauseMenuPanel.SetActive(_isMenuOpen);

        if (_isMenuOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void DisconnectFromGame()
    {
        // 1. Sahnede aktif olan NetworkRunner'ı bul
        NetworkRunner runner = FindFirstObjectByType<NetworkRunner>();

        if (runner != null)
        {
            // 2. Fusion bağlantısını kapat (Host isen odayı kapatır, Client isen odadan çıkar)
            runner.Shutdown();
        }

        // 3. Fareyi normal ayarlarına döndür ki ana menüde kullanabilelim
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 4. Ana menü sahnesini yükle (Normal Unity sahne yükleme işlemi)
        SceneManager.LoadScene(mainMenuSceneIndex);
    }
}
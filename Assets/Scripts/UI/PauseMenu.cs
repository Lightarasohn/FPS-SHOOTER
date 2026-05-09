using Fusion;
using System.Threading.Tasks; // async/await kullanabilmek için gerekli
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static GlobalVariables;

public class PauseMenu : MonoBehaviour
{
    [Header("UI Referansları")]
    public GameObject pauseMenuPanel;
    public Button disconnectButton;
    public TMP_Dropdown typeDropdown;
    public Slider widthSlider;
    public Slider lengthSlider;
    public Slider spaceSlider;
    public Slider scaleSlider;
    public CrosshairManager crosshairTemplateManager;

    [Header("Ayarlar")]
    public int mainMenuSceneIndex = 0;

    private bool _isMenuOpen = false;

    void Start()
    {
        pauseMenuPanel.SetActive(false);

        disconnectButton.onClick.AddListener(DisconnectFromGame);
        Crosshair currentSettings = PlayerSaveManager.LoadCrosshair();

        typeDropdown.value = (int)currentSettings.CrosshairType;
        widthSlider.value = currentSettings.Width;
        lengthSlider.value = currentSettings.Length;
        spaceSlider.value = currentSettings.Space;
        scaleSlider.value = currentSettings.Scale / 100f; // Scale'i kurucuda 100 ile çarptığın için burada bölüyoruz
        crosshairTemplateManager.ApplyCrosshairSettings(currentSettings);
    }

    public void ChangeCrosshairOnCrosshairSettingsChange()
    {
        Crosshair newSettings = new Crosshair(
                (CrosshairType)typeDropdown.value,
                lengthSlider.value,
                widthSlider.value,
                spaceSlider.value,
                scaleSlider.value
            );

        crosshairTemplateManager.ApplyCrosshairSettings(newSettings);
    }

    public void SaveSettingsFromUI()
    {
        try
        {
            Crosshair newSettings = new Crosshair(
                (CrosshairType)typeDropdown.value,
                lengthSlider.value,
                widthSlider.value,
                spaceSlider.value,
                scaleSlider.value
            );

            // 1. Cihaza kaydet
            PlayerSaveManager.SaveCrosshair(newSettings);

            // 2. Pause menüsündeki önizlemeyi (template) güncelle
            crosshairTemplateManager.ApplyCrosshairSettings(newSettings);

            // 3. YENİ EKLENEN: Sahnede kendi karakterimizi bul ve oyun içi nişangahı anında güncelle
            Player[] allPlayers = FindObjectsByType<Player>(FindObjectsSortMode.None);
            foreach (Player p in allPlayers)
            {
                // Eğer oyuncunun objesi varsa ve "InputAuthority" bizdeyse (yani bizim karakterimizse)
                if (p.Object != null && p.HasInputAuthority)
                {
                    p.UpdateLocalCrosshair(newSettings);
                    break; // Kendi karakterimizi bulduğumuz için döngüyü sonlandır
                }
            }

            NotificationScript.Instance.ShowNotification("Ayarlar kaydedildi");
            Debug.Log("Nişangah ayarları kaydedildi ve anında uygulandı!");
        }
        catch
        {
            NotificationScript.Instance.ShowNotification("Ayarlar kaydedilirken bir hata oluştu");
        }
    }

    void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ToggleMenu();
        }
    }

    public void ToggleMenu()
    {
        _isMenuOpen = !_isMenuOpen;
        pauseMenuPanel.SetActive(_isMenuOpen);

        // YENİ: Menü açıksa HUD'u gizle, menü kapalıysa HUD'u göster
        if (PlayerHUD.Instance != null) PlayerHUD.Instance.SetVisible(!_isMenuOpen);
        if(GameHUD.Instance != null) GameHUD.Instance.SetVisible(!_isMenuOpen);

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

    // Metodu 'async void' yaparak bekletme (await) özelliği kazandırıyoruz
    async void DisconnectFromGame()
    {
        // Butona art arda basılmasını engellemek için menüyü gizle veya butonu deaktif et
        pauseMenuPanel.SetActive(false);
        disconnectButton.interactable = false;

        NetworkRunner runner = FindFirstObjectByType<NetworkRunner>();

        if (runner != null)
        {
            // Fusion'ın tüm ağ işlemlerini ve objeleri güvenle temizlemesini bekle
            await runner.Shutdown();
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Fusion tamamen kapandıktan sonra temiz bir şekilde ana menüye dön
        SceneManager.LoadScene(mainMenuSceneIndex);
    }
}
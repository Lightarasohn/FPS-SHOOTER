using Assets.Scripts.Player.PlayerSettings; // Ses ayarları (SoundSettings) sınıfı için gerekli
using Fusion;
using System.Threading.Tasks; // async/await kullanabilmek için gerekli
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static GlobalVariables;

public class PauseMenu : MonoBehaviour
{
    [Header("UI Elementleri")]
    public GameObject pauseMenuPanel;
    public Button disconnectButton;

    [Header("Crosshair Elementleri")]
    public TMP_Dropdown typeDropdown;
    public Slider widthSlider;
    public Slider lengthSlider;
    public Slider spaceSlider;
    public Slider scaleSlider;
    public CrosshairManager crosshairTemplateManager;

    [Header("Mouse Elementleri")]
    public Slider sensitivitySlider;
    public Toggle smoothnessEnabled;
    public Slider smoothnessSpeed;
    public Toggle accelerationEnabled;
    public Slider accelerationThreshold;

    [Header("Ses Elementleri")]
    public AudioMixer mainAudioMixer; // YENİ EKLENDİ: Unity Editöründen MainMixer'ı buraya sürükle
    public Slider mainVolume;
    public Slider sfxVolume; // YENİ EKLENDİ: Ayak sesleri, silah vb.
    public Slider uiVolume;
    // -------------------------------------------

    [Header("Ayarlar")]
    public int mainMenuSceneIndex = 0;

    private bool _isMenuOpen = false;

    void Start()
    {
        pauseMenuPanel.SetActive(false);

        disconnectButton.onClick.AddListener(DisconnectFromGame);
        Crosshair currentSettings = PlayerSaveManager.LoadCrosshair();
        MouseSettings currentMouseSettings = PlayerSaveManager.LoadMouseSettings();

        // --- YENİ: SES AYARINI YÜKLE VE UYGULA ---
        SoundSettings currentSoundSettings = PlayerSaveManager.LoadSoundSettings();

        // --- SES AYARINI UYGULA ---
        mainVolume.value = currentSoundSettings.MainVolume;
        sfxVolume.value = currentSoundSettings.SfxVolume;
        uiVolume.value = currentSoundSettings.UiVolume;

        ApplyVolumeToMixer("MasterVolume", currentSoundSettings.MainVolume);
        ApplyVolumeToMixer("SFXVolume", currentSoundSettings.SfxVolume);
        ApplyVolumeToMixer("UIVolume", currentSoundSettings.UiVolume);
        // --------------------------

        typeDropdown.value = (int)currentSettings.CrosshairType;
        widthSlider.value = currentSettings.Width;
        lengthSlider.value = currentSettings.Length;
        spaceSlider.value = currentSettings.Space;
        scaleSlider.value = currentSettings.Scale / 100f; // Scale'i kurucuda 100 ile çarptığın için burada bölüyoruz
        crosshairTemplateManager.ApplyCrosshairSettings(currentSettings);

        // Mouse
        sensitivitySlider.value = currentMouseSettings.MouseSensitivity;
        smoothnessEnabled.isOn = currentMouseSettings.EnableSmoothness;
        smoothnessSpeed.value = currentMouseSettings.SmoothnessSpeed;
        accelerationEnabled.isOn = currentMouseSettings.EnableAcceleration;
        accelerationThreshold.value = currentMouseSettings.AccelerationThreshold;

        if (!smoothnessEnabled.isOn)
        {
            smoothnessSpeed.interactable = false;
        }
        if (!accelerationEnabled.isOn)
        {
            accelerationThreshold.interactable = false;
        }
    }

    private void ApplyVolumeToMixer(string parameterName, float sliderValue)
    {
        if (mainAudioMixer == null) return;

        // Slider değeri 0.001'den küçükse sesi tamamen kapat (-80 dB)
        float db = sliderValue > 0.001f ? Mathf.Log10(sliderValue) * 20f : -80f;
        mainAudioMixer.SetFloat(parameterName, db);
    }

    public void OnSmoothnessToggleChanged()
    {
        smoothnessSpeed.interactable = smoothnessEnabled.isOn;
    }

    public void OnAccelerationToggleChanged()
    {
        accelerationThreshold.interactable = accelerationEnabled.isOn;
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

            MouseSettings newMouseSettings =
                new MouseSettings(
                    sensitivitySlider.value,
                    smoothnessEnabled.isOn,
                    smoothnessSpeed.value,
                    accelerationEnabled.isOn,
                    accelerationThreshold.value,
                    0.5f,
                    3f);

            // Mouse
            PlayerSaveManager.SaveMouseSettings(newMouseSettings);

            // --- YENİ SES KAYIT KISMI ---
            float selectedMain = mainVolume.value;
            float selectedSfx = sfxVolume.value;
            float selectedUi = uiVolume.value;

            SoundSettings newSoundSettings = new SoundSettings(selectedMain, selectedSfx, selectedUi);
            PlayerSaveManager.SaveSoundSettings(newSoundSettings);

            // Ayarları anında Mixer'a uygula
            ApplyVolumeToMixer("MasterVolume", selectedMain);
            ApplyVolumeToMixer("SFXVolume", selectedSfx);
            ApplyVolumeToMixer("UIVolume", selectedUi);
            // ----------------------------

            // 3. Sahnede kendi karakterimizi bul ve oyun içi nişangahı/mouse'u anında güncelle
            Player[] allPlayers = FindObjectsByType<Player>(FindObjectsSortMode.None);
            foreach (Player p in allPlayers)
            {
                // Eğer oyuncunun objesi varsa ve "InputAuthority" bizdeyse (yani bizim karakterimizse)
                if (p.Object != null && p.HasInputAuthority)
                {
                    p.UpdateLocalCrosshair(newSettings);
                    p.GetComponent<PlayerInputHandler>().UpdateMouseSettings(newMouseSettings);
                    break; // Kendi karakterimizi bulduğumuz için döngüyü sonlandır
                }
            }

            NotificationScript.Instance.ShowNotification("Ayarlar kaydedildi");
            Debug.Log("Ayarlar kaydedildi ve anında uygulandı!");
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
        if (GameHUD.Instance != null) GameHUD.Instance.SetVisible(!_isMenuOpen);

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
        try
        {
            // Butona art arda basılmasını engellemek için menüyü gizle veya butonu deaktif et
            pauseMenuPanel.SetActive(false);
            disconnectButton.interactable = false;

            NetworkRunner runner = FindFirstObjectByType<NetworkRunner>();

            if (runner != null)
            {
                // Fusion'ın tüm ağ işlemlerini temizlemesini bekle.
                // Kapanma bitince BasicSpawner.OnShutdown tetiklenecek ve sahneyi o değiştirecek.
                await runner.Shutdown();
            }
            else
            {
                // Eğer sahnede runner yoksa (güvenlik amaçlı) direkt ana menüye dön
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                SceneManager.LoadScene(mainMenuSceneIndex);
            }
        }
        catch
        {
            pauseMenuPanel.SetActive(true);
            disconnectButton.interactable = true;
        }
    }
}
using Assets.Scripts.Player.PlayerSettings;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using static GlobalVariables;

public class MainMenuSettings : MonoBehaviour
{

    [Header("UI Elementleri")]
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

    // Start is called once before the first execution of Updat e after the MonoBehaviour is created
    private void Start()
    {
        // Menü açıldığında mevcut ayarları UI'a yansıt
        Crosshair currentSettings = PlayerSaveManager.LoadCrosshair();
        MouseSettings currentMouseSettings = PlayerSaveManager.LoadMouseSettings();
        SoundSettings currentSoundSettings = PlayerSaveManager.LoadSoundSettings(); // SESİ YÜKLE

        // --- SES AYARINI UYGULA ---
        mainVolume.value = currentSoundSettings.MainVolume;
        sfxVolume.value = currentSoundSettings.SfxVolume;
        uiVolume.value = currentSoundSettings.UiVolume;

        ApplyVolumeToMixer("MasterVolume", currentSoundSettings.MainVolume);
        ApplyVolumeToMixer("SFXVolume", currentSoundSettings.SfxVolume);
        ApplyVolumeToMixer("UIVolume", currentSoundSettings.UiVolume);
        // --------------------------

        // Crosshair
        typeDropdown.value = (int)currentSettings.CrosshairType;
        widthSlider.value = currentSettings.Width;
        lengthSlider.value = currentSettings.Length;
        spaceSlider.value = currentSettings.Space;
        scaleSlider.value = currentSettings.Scale / 100f;
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

    // Bu fonksiyonu "Kaydet" butonuna veya Slider'ların "OnValueChanged" eventine bağlayabilirsin

    public void SaveSettingsFromUI()
    {
        try
        {
            // Crosshair (Senin yazdığın kod aynen duruyor)
            Crosshair newSettings = new Crosshair(
                (CrosshairType)typeDropdown.value,
                lengthSlider.value,
                widthSlider.value,
                spaceSlider.value,
                scaleSlider.value
            );

            PlayerSaveManager.SaveCrosshair(newSettings);
            crosshairTemplateManager.ApplyCrosshairSettings(newSettings);

            // Mouse (Senin yazdığın kod aynen duruyor)
            MouseSettings newMouseSettings =
                new MouseSettings(
                    sensitivitySlider.value,
                    smoothnessEnabled.isOn,
                    smoothnessSpeed.value,
                    accelerationEnabled.isOn,
                    accelerationThreshold.value,
                    0.5f,
                    3f);

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

            NotificationScript.Instance.ShowNotification("Ayarlar kaydedildi");
            Debug.Log("Ayarlar kaydedildi!");
        }
        catch
        {
            NotificationScript.Instance.ShowNotification("Ayarlar kaydedilirken bir sorun oluştu");
        }
    }

    public void OnExit()
    {
        Application.Quit();
    }
}

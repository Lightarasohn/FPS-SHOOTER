using TMPro;
using UnityEngine;
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

    // Start is called once before the first execution of Updat e after the MonoBehaviour is created
    private void Start()
    {
        // Menü açıldığında mevcut ayarları UI'a yansıt
        Crosshair currentSettings = PlayerSaveManager.LoadCrosshair();
        MouseSettings currentMouseSettings = PlayerSaveManager.LoadMouseSettings();

        // Crosshair
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

        if(!smoothnessEnabled.isOn)
        {
            smoothnessSpeed.interactable = false;
        }
        if (!accelerationEnabled.isOn)
        {
            accelerationThreshold.interactable = false;
        }
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
            // Crosshair
            Crosshair newSettings = new Crosshair(
                (CrosshairType)typeDropdown.value,
                lengthSlider.value,
                widthSlider.value,
                spaceSlider.value,
                scaleSlider.value
            );

            PlayerSaveManager.SaveCrosshair(newSettings);
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

            // Bildirim
            NotificationScript.Instance.ShowNotification("Ayarlar kaydedildi");
            Debug.Log("Nişangah ayarları kaydedildi!");
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

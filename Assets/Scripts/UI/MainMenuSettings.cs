using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static GlobalVariables;

public class MainMenuSettings : MonoBehaviour
{

    [Header("UI Elementleri")]
    public TMP_Dropdown typeDropdown;
    public Slider widthSlider;
    public Slider lengthSlider;
    public Slider spaceSlider;
    public Slider scaleSlider;

    public CrosshairManager crosshairTemplateManager;
    // Start is called once before the first execution of Updat e after the MonoBehaviour is created
    private void Start()
    {
        // Menü açıldığında mevcut ayarları UI'a yansıt
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

    // Bu fonksiyonu "Kaydet" butonuna veya Slider'ların "OnValueChanged" eventine bağlayabilirsin

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

            PlayerSaveManager.SaveCrosshair(newSettings);
            crosshairTemplateManager.ApplyCrosshairSettings(newSettings);

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

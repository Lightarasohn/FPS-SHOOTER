using UnityEngine;
using static GlobalVariables;

public static class PlayerSaveManager
{
    // Veriyi PlayerPrefs içinde hangi isimle tutacağımızı belirliyoruz
    private const string SAVE_KEY_CROSSHAIR = "PlayerCrosshairSettings";
    private const string SAVE_KEY_MOUSE = "PlayerMouseSettings";

    public static void SaveMouseSettings(MouseSettings mouseSettings)
    {
        string mouseSettingsJson = JsonUtility.ToJson(mouseSettings);
        PlayerPrefs.SetString(SAVE_KEY_MOUSE, mouseSettingsJson);
        PlayerPrefs.Save();
    }

    public static MouseSettings LoadMouseSettings()
    {
        if (PlayerPrefs.HasKey(SAVE_KEY_MOUSE))
        {
            string mouseSettingsJson = PlayerPrefs.GetString(SAVE_KEY_MOUSE);
            MouseSettings loadedMouseSettings = JsonUtility.FromJson<MouseSettings>(mouseSettingsJson);
            return loadedMouseSettings;
        }
        // Eğer ilk defa oyuna giriliyorsa (kayıt yoksa) varsayılanları döndür
        return new MouseSettings(1, false, 25f, false, 2f, 0.5f, 3f);
    }

    // Ayarları Kaydetme Fonksiyonu
    public static void SaveCrosshair(Crosshair data)
    {
        // Sınıfı JSON formatında bir metne çevir
        string jsonString = JsonUtility.ToJson(data);

        // Metni cihaza kaydet
        PlayerPrefs.SetString(SAVE_KEY_CROSSHAIR, jsonString);
        PlayerPrefs.Save();
    }

    // Ayarları Okuma Fonksiyonu
    public static Crosshair LoadCrosshair()
    {
        // Eğer daha önce kaydedilmiş bir ayar varsa onu oku
        if (PlayerPrefs.HasKey(SAVE_KEY_CROSSHAIR))
        {
            string jsonString = PlayerPrefs.GetString(SAVE_KEY_CROSSHAIR);
            return JsonUtility.FromJson<Crosshair>(jsonString);
        }

        // Eğer ilk defa oyuna giriliyorsa (kayıt yoksa) varsayılan nişangahı döndür
        return new Crosshair(CrosshairType.X, 0.2f, 0.06f, 0.03f, 0.3f);
    }
}

using UnityEngine;
using static GlobalVariables;

public static class PlayerSaveManager
{
    // Veriyi PlayerPrefs içinde hangi isimle tutacağımızı belirliyoruz
    private const string SAVE_KEY = "PlayerCrosshairSettings";

    // Ayarları Kaydetme Fonksiyonu
    public static void SaveCrosshair(Crosshair data)
    {
        // Sınıfı JSON formatında bir metne çevir
        string jsonString = JsonUtility.ToJson(data);

        // Metni cihaza kaydet
        PlayerPrefs.SetString(SAVE_KEY, jsonString);
        PlayerPrefs.Save();
    }

    // Ayarları Okuma Fonksiyonu
    public static Crosshair LoadCrosshair()
    {
        // Eğer daha önce kaydedilmiş bir ayar varsa onu oku
        if (PlayerPrefs.HasKey(SAVE_KEY))
        {
            string jsonString = PlayerPrefs.GetString(SAVE_KEY);
            return JsonUtility.FromJson<Crosshair>(jsonString);
        }

        // Eğer ilk defa oyuna giriliyorsa (kayıt yoksa) varsayılan nişangahı döndür
        return new Crosshair(CrosshairType.X, 0.2f, 0.06f, 0.03f, 0.3f);
    }
}

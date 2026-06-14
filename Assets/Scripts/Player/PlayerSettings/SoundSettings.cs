using UnityEngine;

namespace Assets.Scripts.Player.PlayerSettings
{
    [System.Serializable]
    public class SoundSettings
    {
        public float MainVolume;
        public float SfxVolume; // YENİ
        public float UiVolume;  // YENİ

        // Varsayılan değerleri 1.0f (Maksimum) olarak belirliyoruz
        public SoundSettings(float mainVolume = 1.0f, float sfxVolume = 1.0f, float uiVolume = 1.0f)
        {
            MainVolume = mainVolume;
            SfxVolume = sfxVolume;
            UiVolume = uiVolume;
        }
    }
}
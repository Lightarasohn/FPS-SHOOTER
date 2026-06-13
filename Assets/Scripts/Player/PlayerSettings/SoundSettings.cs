using UnityEngine;

namespace Assets.Scripts.Player.PlayerSettings
{
    [System.Serializable] // JsonUtility'nin bu sınıfı okuyabilmesi için zorunlu
    public class SoundSettings
    {
        public float MainVolume; // { get; set; } KULLANMIYORUZ! Float yapıyoruz (0.0f - 1.0f arası)

        public SoundSettings(float mainVolume)
        {
            MainVolume = mainVolume;
        }
    }
}
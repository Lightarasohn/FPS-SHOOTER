using UnityEngine;
using UnityEngine.Audio;

[RequireComponent(typeof(AudioSource))]
public class WeaponAudioHandler : MonoBehaviour
{
    [Header("Mixer Kanalı")]
    public AudioMixerGroup sfxMixerGroup;

    [Header("Dinamik Ses Ayarları")]
    [SerializeField] private float minPitch = 0.95f;
    [SerializeField] private float maxPitch = 1.05f;
    [SerializeField] private float volume = 0.8f;

    private AudioSource audioSource;

    // Sesleri artık Inspector'dan değil, kod üzerinden alacağız
    private AudioClip currentSingleClip;
    private AudioClip currentAutoClip;
    private AudioClip currentDrawClip;
    private AudioClip currentReloadClip;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.spatialBlend = 1f;
        audioSource.playOnAwake = false;

        if (sfxMixerGroup != null)
        {
            audioSource.outputAudioMixerGroup = sfxMixerGroup;
        }
    }

    // YENİ: PlayerWeapon silahı kuşandığında sesleri bu namluya enjekte edecek
    public void SetupSounds(AudioClip single, AudioClip draw, AudioClip reload)
    {
        currentSingleClip = single;
        currentDrawClip = draw;
        currentReloadClip = reload;
    }

    public void PlaySingleFireSound()
    {
        if (currentSingleClip == null) return;
        audioSource.pitch = Random.Range(minPitch, maxPitch);
        audioSource.PlayOneShot(currentSingleClip, volume);
    }

    public void ToggleAutoFireSound(bool isFiring)
    {
        if (currentAutoClip == null) return;

        if (isFiring)
        {
            if (!audioSource.isPlaying || audioSource.clip != currentAutoClip)
            {
                audioSource.clip = currentAutoClip;
                audioSource.loop = true;
                audioSource.pitch = 1f;
                audioSource.Play();
            }
        }
        else
        {
            if (audioSource.isPlaying && audioSource.clip == currentAutoClip)
            {
                audioSource.Stop();
                audioSource.loop = false;
            }
        }
    }

    public void PlayDrawSound()
    {
        if (currentDrawClip == null) return;
        audioSource.pitch = Random.Range(0.98f, 1.02f);
        audioSource.PlayOneShot(currentDrawClip, volume * 0.7f);
    }

    public void PlayReloadSound()
    {
        if (currentReloadClip == null) return;
        audioSource.pitch = Random.Range(0.98f, 1.02f);
        audioSource.PlayOneShot(currentReloadClip, volume);
    }
    // Otomatik loop sesi atanmış mı atanmamış mı kontrolü
    public bool HasAutoClip()
    {
        return currentAutoClip != null;
    }
}
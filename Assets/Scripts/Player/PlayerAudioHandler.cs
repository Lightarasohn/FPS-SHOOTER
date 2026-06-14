using UnityEngine;
using UnityEngine.Audio;

[RequireComponent(typeof(AudioSource))]
public class PlayerAudioHandler : MonoBehaviour
{
    [Header("Mixer Kanalları")]
    public AudioMixerGroup sfxMixerGroup; // Inspector'dan 'SFX' grubunu sürükle
    public AudioMixerGroup uiMixerGroup;  // Inspector'dan 'UI' grubunu sürükle

    [Header("Ayak ve Fizik Sesleri (3D)")]
    public AudioClip[] footstepClips;
    public AudioClip jumpClip;
    public AudioClip landClip;
    public AudioClip slideClip;
    [SerializeField] private float physicsVolume = 0.5f;

    [Header("Aksiyon Sesleri (Lokal 2D)")]
    public AudioClip takeDamageClip;
    public AudioClip dealDamageClip;
    public AudioClip scoreClip;

    [Header("Aksiyon Sesleri Ayarları (Volume)")]
    [Range(0f, 1f)] public float takeDamageVolume = 0.7f;
    [Range(0f, 1f)] public float dealDamageVolume = 0.5f;
    [Range(0f, 1f)] public float scoreVolume = 1.0f;

    // İki ayrı AudioSource kullanacağız
    private AudioSource audioSource3D; // Ayak sesleri vs. (Mevcut olan)
    private AudioSource audioSource2D; // UI ve Hitmarker sesleri (Yeni eklenen)

    private void Awake()
    {
        AudioSource[] sources = GetComponents<AudioSource>();

        if (sources.Length < 2)
        {
            audioSource3D = sources[0];
            audioSource2D = gameObject.AddComponent<AudioSource>();
        }
        else
        {
            audioSource3D = sources[0];
            audioSource2D = sources[1];
        }

        // --- MİXER ATAMALARI ---
        if (sfxMixerGroup != null) audioSource3D.outputAudioMixerGroup = sfxMixerGroup;
        if (uiMixerGroup != null) audioSource2D.outputAudioMixerGroup = uiMixerGroup;

        // 3D Ayarları
        audioSource3D.spatialBlend = 1f;
        audioSource3D.playOnAwake = false;
        audioSource3D.minDistance = 3f;
        audioSource3D.maxDistance = 100f;

        // 2D Ayarları (Kulağımızın dibinde çalacak UI sesleri)
        audioSource2D.spatialBlend = 0f;
        audioSource2D.playOnAwake = false;
        audioSource2D.bypassEffects = true; // Yankı vs. gibi çevresel efektlerden etkilenmemesi için
    }

    public void PlayFootstep()
    {
        if (footstepClips == null || footstepClips.Length == 0) return;
        AudioClip randomStep = footstepClips[Random.Range(0, footstepClips.Length)];
        audioSource3D.pitch = Random.Range(0.9f, 1.1f);
        audioSource3D.PlayOneShot(randomStep, physicsVolume);
    }

    public void PlayJump()
    {
        if (jumpClip == null) return;
        audioSource3D.pitch = Random.Range(0.95f, 1.05f);
        audioSource3D.PlayOneShot(jumpClip, physicsVolume * 1.2f);
    }

    public void PlayLand()
    {
        if (landClip == null) return;
        audioSource3D.pitch = Random.Range(0.9f, 1.1f);
        audioSource3D.PlayOneShot(landClip, physicsVolume * 1.5f);
    }

    public void PlaySlide()
    {
        if (slideClip == null) return;
        audioSource3D.pitch = Random.Range(0.95f, 1.05f);
        audioSource3D.PlayOneShot(slideClip, physicsVolume);
    }

    // --- ARTIK 2D SESLER İÇİN İKİNCİ KAYNAĞI KULLANIYORUZ ---

    public void PlayTakeDamage()
    {
        if (takeDamageClip == null) return;
        audioSource2D.pitch = Random.Range(0.95f, 1.05f);
        audioSource2D.PlayOneShot(takeDamageClip, takeDamageVolume);
    }

    public void PlayDealDamage()
    {
        if (dealDamageClip == null) return;
        audioSource2D.pitch = Random.Range(0.98f, 1.02f);
        audioSource2D.PlayOneShot(dealDamageClip, dealDamageVolume);
    }

    public void PlayScoreSound()
    {
        if (scoreClip == null) return;
        audioSource2D.pitch = 1f; // Kill sesi sabit pitch kalsa daha tatmin edici olur
        audioSource2D.PlayOneShot(scoreClip, scoreVolume);
    }
}
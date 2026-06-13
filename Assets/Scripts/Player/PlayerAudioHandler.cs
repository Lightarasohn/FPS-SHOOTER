using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class PlayerAudioHandler : MonoBehaviour
{
    [Header("Ayak Sesleri")]
    public AudioClip[] footstepClips;

    [Header("Aksiyon Sesleri")]
    public AudioClip jumpClip;
    public AudioClip landClip;  // YENİ: Yere Düşme Sesi
    public AudioClip slideClip; // YENİ: Kayma Sesi

    [Header("Ses Ayarları")]
    [SerializeField] private float volume = 0.5f;

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.spatialBlend = 1f;
        audioSource.playOnAwake = false;
    }

    public void PlayFootstep()
    {
        if (footstepClips == null || footstepClips.Length == 0) return;

        AudioClip randomStep = footstepClips[Random.Range(0, footstepClips.Length)];
        audioSource.pitch = Random.Range(0.9f, 1.1f);
        audioSource.PlayOneShot(randomStep, volume);
    }

    public void PlayJump()
    {
        if (jumpClip == null) return;
        audioSource.pitch = Random.Range(0.95f, 1.05f);
        audioSource.PlayOneShot(jumpClip, volume * 1.2f);
    }

    // YENİ: Yere Çarpma Sesi
    public void PlayLand()
    {
        if (landClip == null) return;
        audioSource.pitch = Random.Range(0.9f, 1.1f);
        // Düşme sesi biraz tok ve yüksek olmalı
        audioSource.PlayOneShot(landClip, volume * 1.5f);
    }

    // YENİ: Kayma Sesi
    public void PlaySlide()
    {
        if (slideClip == null) return;
        audioSource.pitch = Random.Range(0.95f, 1.05f);
        audioSource.PlayOneShot(slideClip, volume);
    }
}
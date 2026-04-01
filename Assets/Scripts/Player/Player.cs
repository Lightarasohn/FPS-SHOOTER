using Fusion;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Windows;
using static Fusion.NetworkBehaviour;

public class Player : NetworkBehaviour
{
    [Networked] public int Health { get; set; } = 100;
    public int MaxHealth = 500;
    public int MinHealth = 0;
    public Color DefaultColor = Color.blue;
    public Weapon PlayerWeapon;

    public void Awake()
    {
        PlayerWeapon = new MG48();
    }

    public override void Spawned()
    {
        bool isLocal = Object.HasInputAuthority;

        if (!isLocal)
        {
            Camera playerLocalCamera = GetComponentInChildren<Camera>();
            if (playerLocalCamera != null)
                playerLocalCamera.enabled = false;

            AudioListener playerLocalAudioListener = GetComponentInChildren<AudioListener>();
            if (playerLocalAudioListener != null)
                playerLocalAudioListener.enabled = false;
        }
    }

    public void TakeDamage(int damage)
    {
        if (Object.HasStateAuthority)
        {
            Health -= damage;
        }
    }
}

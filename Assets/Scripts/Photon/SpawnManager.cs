using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    // Her yerden (PlayerState içinden) kolayca ulaşabilmek için Singleton yapıyoruz
    public static SpawnManager Instance;

    [Header("Spawn Noktaları")]
    public Transform redSpawnPoint;
    public Transform blueSpawnPoint;

    private void Awake()
    {
        // Oyun başladığında kendini her yerden ulaşılabilir yap
        Instance = this;
    }
}
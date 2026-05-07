using Fusion;
using System.Collections.Generic;
using UnityEngine;
using static GlobalVariables;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;

    [Networked] public RoundState CurrentState { get; set; }
    [Networked] public int TeamRedScore { get; set; } // Red Team
    [Networked] public int TeamBlueScore { get; set; } // Blue Team
    [Networked] public TickTimer RoundTimer { get; set; }

    // Oyundaki tüm oyuncuları tutacağımız liste
    private List<Player> _activePlayers = new List<Player>();

    private void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void AddPlayer(Player player)
    {
        _activePlayers.Add(player);

        // Eğer oyun henüz başlamadıysa (oyuncu bekleniyorsa) takımları kontrol et
        if (HasStateAuthority && CurrentState == RoundState.WaitingForPlayers)
        {
            bool hasRedTeamPlayer = false;
            bool hasBlueTeamPlayer = false;

            // Listedeki oyuncuların takımlarına bak
            foreach (var p in _activePlayers)
            {
                if (p.PlayerTeam == Team.Red) hasRedTeamPlayer = true;
                if (p.PlayerTeam == Team.Blue) hasBlueTeamPlayer = true;
                Debug.Log($"Red Team: {hasRedTeamPlayer}, Blue Team: {hasBlueTeamPlayer}");

                // İki takımda da adam bulduysak döngüyü boşuna devam ettirmeye gerek yok
                if (hasRedTeamPlayer && hasBlueTeamPlayer) break;
            }

            // Kırmızıda ve Mavide en az 1 kişi varsa maçı başlat
            if (hasRedTeamPlayer && hasBlueTeamPlayer)
            {
                Debug.Log("[GameManager] Her iki takımda da oyuncu var, Maç Başlıyor!");
                ResetForNewRound();
            }
        }
    }

    public void RemovePlayer(Player player)
    {
        _activePlayers.Remove(player);
        // Bir oyuncu oyundan çıkarsa (Alt+F4 vb.) karşı takım kazanmış olabilir, kontrol et
        CheckWinCondition();
    }

    // Bu fonksiyon her biri öldüğünde veya oyuncu çıktığında çağrılır
    public void CheckWinCondition()
    {
        // Sadece yetkili sunucu karar verebilir ve round oynanmıyorsa kontrol etme
        if (!HasStateAuthority || CurrentState != RoundState.Playing) return;

        int teamRedAliveCount = 0; // Red
        int teamBlueAliveCount = 0; // Blue

        foreach (var player in _activePlayers)
        {
            if (player.IsAlive) // Not: Önceki mesajlarda IsDead yaptıysan burayı !player.IsDead olarak değiştir
            {
                if (player.PlayerTeam == Team.Red) teamRedAliveCount++;
                else if (player.PlayerTeam == Team.Blue) teamBlueAliveCount++;
            }
        }

        // Eğer Kırmızı Takım'da kimse kalmadıysa, Mavi Takım kazanır
        if (teamRedAliveCount == 0 && teamBlueAliveCount > 0)
        {
            EndRound(Team.Blue);
        }
        // Eğer Mavi Takım'da kimse kalmadıysa, Kırmızı Takım kazanır
        else if (teamBlueAliveCount == 0 && teamRedAliveCount > 0)
        {
            EndRound(Team.Red);
        }
        // Eğer ikisi de aynı anda ölürse (el bombası vb.)
        else if (teamRedAliveCount == 0 && teamBlueAliveCount == 0)
        {
            EndRound(null); // Berabere
        }
    }

    private void EndRound(Team? winner)
    {
        if (winner == Team.Red) TeamRedScore++;
        else if (winner == Team.Blue) TeamBlueScore++;

        CurrentState = RoundState.RoundEnd;
        RoundTimer = TickTimer.CreateFromSeconds(Runner, 5f); // 5 saniye sonra yeni round başlar
    }

    // GameManager içindeki FixedUpdateNetwork - STATE MACHINE AKIŞI
    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        switch (CurrentState)
        {
            case RoundState.WaitingForPlayers:
                // Oyuncuların bağlanması AddPlayer metodu içinde kontrol ediliyor
                break;

            case RoundState.PreRound:
                // 15 Saniyelik Freeze Time bittiğinde asıl savaşı başlat
                if (RoundTimer.Expired(Runner))
                {
                    StartRound();
                }
                break;

            case RoundState.Playing:
                // Normal round süresi biterse (Örn: 2 dakika) oyunu berabere bitir
                if (RoundTimer.Expired(Runner))
                {
                    EndRound(null);
                }
                break;

            case RoundState.RoundEnd:
                // 5 saniyelik round bitiş ekranı süresi bittiğinde yeni round kur
                if (RoundTimer.Expired(Runner))
                {
                    ResetForNewRound();
                }
                break;
        }
    }

    private void StartRound()
    {
        CurrentState = RoundState.Playing;
        RoundTimer = TickTimer.CreateFromSeconds(Runner, 120f); // Asıl round süresi 2 dakika
    }

    private void ResetForNewRound()
    {
        CurrentState = RoundState.PreRound;
        RoundTimer = TickTimer.CreateFromSeconds(Runner, 15f); // 15 saniye freeze time

        foreach (var player in _activePlayers)
        {
            player.Health = 100;
            player.IsAlive = true;

            // oyuncunun augmentini kaldır
            player.ClearAugments();

            // 1. OYUNCUNUN MEVCUT HIZINI SIFIRLA (Kendi yazdığın script üzerinden)
            var customMovement = player.GetComponent<PlayerMovement>();
            if (customMovement != null)
            {
                Debug.Log($"[GameManager] {player.name} için hareket sıfırlanıyor.");
                customMovement.Velocity = Vector3.zero; // Kayma veya zıplama ivmesi varsa iptal et
                customMovement.IsSliding = false;       // Eski rounddan kalan kaymayı iptal et
            }
            // player.ResetWeaponAmmo();
            // 2. OYUNCUYU SPAWN NOKTASINA IŞINLA
            Transform spawnPoint = GetSpawnPointForTeam(player.PlayerTeam);
            if (spawnPoint != null)
            {
                player.transform.position = spawnPoint.position;

                // Rotasyonu da hedefin baktığı yöne çevir (İsteğe bağlı)
                player.transform.rotation = spawnPoint.rotation;
            }
        }
    }

    public Transform GetSpawnPointForTeam(Team team)
    {
        // Sahnede SpawnManager var mı diye kontrol ediyoruz
        if (SpawnManager.Instance != null)
        {
            // Takıma göre doğru noktayı döndür
            if (team == Team.Red)
            {
                return SpawnManager.Instance.redSpawnPoint;
            }
            else if (team == Team.Blue)
            {
                return SpawnManager.Instance.blueSpawnPoint;
            }
        }
        else
        {
            Debug.LogError("[GameManager] Sahnede SpawnManager bulunamadı! Karakterler yanlış yerde doğabilir.");
        }

        // Eğer SpawnManager yoksa veya hata olursa GameManager'ın olduğu yeri (0,0,0) ver
        return transform;
    }
}
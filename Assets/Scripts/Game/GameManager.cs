using Fusion;
using System.Collections.Generic;
using UnityEngine;
using static GlobalVariables;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;

    public bool IsReady { get; private set; }

    [Networked] public RoundState CurrentState { get; set; }
    [Networked] public int TeamRedScore { get; set; }
    [Networked] public int TeamBlueScore { get; set; }
    [Networked] public TickTimer RoundTimer { get; set; }

    private List<Player> _activePlayers = new List<Player>();
    public IReadOnlyList<Player> ActivePlayers => _activePlayers;

    private void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public override void Spawned()
    {
        IsReady = true;

        if (HasStateAuthority)
        {
            TeamRedScore = 0;
            TeamBlueScore = 0;
        }
    }

    public void AddPlayer(Player player)
    {
        _activePlayers.Add(player);

        if (HasStateAuthority && CurrentState == RoundState.WaitingForPlayers)
        {
            bool hasRedTeamPlayer = false;
            bool hasBlueTeamPlayer = false;

            foreach (var p in _activePlayers)
            {
                if (p.PlayerTeam == Team.Red) hasRedTeamPlayer = true;
                if (p.PlayerTeam == Team.Blue) hasBlueTeamPlayer = true;
                if (hasRedTeamPlayer && hasBlueTeamPlayer) break;
            }

            if (hasRedTeamPlayer && hasBlueTeamPlayer)
            {
                ResetForNewRound();
            }
        }
    }

    public void RemovePlayer(Player player)
    {
        _activePlayers.Remove(player);
        CheckWinCondition();
    }

    public void CheckWinCondition()
    {
        if (!HasStateAuthority || CurrentState != RoundState.Playing) return;

        int teamRedAliveCount = 0;
        int teamBlueAliveCount = 0;

        foreach (var player in _activePlayers)
        {
            if (player.IsAlive)
            {
                if (player.PlayerTeam == Team.Red) teamRedAliveCount++;
                else if (player.PlayerTeam == Team.Blue) teamBlueAliveCount++;
            }
        }

        if (teamRedAliveCount == 0 && teamBlueAliveCount > 0) EndRound(Team.Blue);
        else if (teamBlueAliveCount == 0 && teamRedAliveCount > 0) EndRound(Team.Red);
        else if (teamRedAliveCount == 0 && teamBlueAliveCount == 0) EndRound(null);
    }

    private void EndRound(Team? winner)
    {
        if (winner == Team.Red) TeamRedScore++;
        else if (winner == Team.Blue) TeamBlueScore++;

        CurrentState = RoundState.RoundEnd;
        RoundTimer = TickTimer.CreateFromSeconds(Runner, 5f);
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        switch (CurrentState)
        {
            case RoundState.WaitingForPlayers:
                break;
            case RoundState.PreRound:
                if (RoundTimer.Expired(Runner)) StartRound();
                break;
            case RoundState.Playing:
                if (RoundTimer.Expired(Runner)) EndRound(null);
                break;
            case RoundState.RoundEnd:
                if (RoundTimer.Expired(Runner)) ResetForNewRound();
                break;
        }
    }

    private void StartRound()
    {
        CurrentState = RoundState.Playing;
        RoundTimer = TickTimer.CreateFromSeconds(Runner, 120f);
    }

    private void ResetForNewRound()
    {
        CurrentState = RoundState.PreRound;
        RoundTimer = TickTimer.CreateFromSeconds(Runner, 15f);

        // --- 1. ÇÖZÜM: Yerdeki Silahların Temizlenmesi ---
        // Fusion'ın kendi ağ listesini kontrol etmek tüm sahneyi taramaktan katbekat hızlıdır.
        foreach (var netObj in Runner.GetAllNetworkObjects())
        {
            if (netObj != null && netObj.IsValid && netObj.HasStateAuthority)
            {
                PhysicalWeaponInfo weapon = netObj.GetComponent<PhysicalWeaponInfo>();
                if (weapon != null)
                {
                    Runner.Despawn(netObj);
                }
            }
        }

        foreach (var player in _activePlayers)
        {
            // Önceki elden kalan augmentleri temizle
            player.ClearAugments();

            // --- 2. ÇÖZÜM: 500 Can Bug'ı (Inspector Override) ---
            player.MaxHealth = 100; // Unity Inspector'daki kalıntı veriyi kod ile eziyoruz
            player.Health = player.MaxHealth;
            player.Armor = 100;
            player.IsAlive = true;
            player.ClearDamageHistory();

            var customMovement = player.GetComponent<PlayerMovement>();
            if (customMovement != null)
            {
                customMovement.Velocity = Vector3.zero;
                customMovement.IsSliding = false;
            }

            if (player.EquippedWeapon != null)
            {
                player.EquippedWeapon.ResetAmmo();
            }

            Transform spawnPoint = GetSpawnPointForTeam(player.PlayerTeam);
            if (spawnPoint != null)
            {
                // --- 3. ÇÖZÜM: Doğru Işınlanma (Client Prediction Bypass) ---
                NetworkTransform netTransform = player.GetComponent<NetworkTransform>();
                if (netTransform != null)
                {
                    // İstemcinin eski konumunu tahmin etmesini engeller ve ağı zorla yeni lokasyona eşitler
                    netTransform.Teleport(spawnPoint.position, spawnPoint.rotation);
                }
                else
                {
                    // Fallback (Sadece NetworkTransform silindiyse çalışır)
                    player.transform.position = spawnPoint.position;
                    player.transform.rotation = spawnPoint.rotation;
                }
            }
        }
    }

    public Transform GetSpawnPointForTeam(Team team)
    {
        if (SpawnManager.Instance != null)
        {
            if (team == Team.Red) return SpawnManager.Instance.redSpawnPoint;
            else if (team == Team.Blue) return SpawnManager.Instance.blueSpawnPoint;
        }
        return transform;
    }
}
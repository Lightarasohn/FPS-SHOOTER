using Fusion;
using UnityEngine;
using static GlobalVariables; 

public class PlayerState : NetworkBehaviour
{
    public static PlayerState Local; // Arayüzün bu objeyi bulması için referans
    public static bool IsSpawned = false;

    [Header("Spawn Settings")]
    [SerializeField] private NetworkPrefabRef _characterPrefab; // Oynayacağın asıl karakter prefabı

    public override void Spawned()
    {
        // Bu temsilci objesi bize (bizim bilgisayarımıza) aitse
        if (Object.HasInputAuthority)
        {
            Local = this;

            if (TeamSelectUI.Instance != null)
            {
                TeamSelectUI.Instance.ShowMenu();
            }
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_RequestSpawn(Team team)
    {
        if (Object.HasStateAuthority)
        {
            Vector3 spawnPos = Vector3.zero;
            if (GameManager.Instance != null)
            {
                Transform teamSpawn = GameManager.Instance.GetSpawnPointForTeam(team);
                if (teamSpawn != null)
                {
                    spawnPos = teamSpawn.position;
                }
            }
            else
            {
                Debug.LogError("[PlayerState] Sahnede GameManager bulunamadı!");
            }

            // DÜZELTİLEN KISIM BURASI: (runner, obj) => lambda fonksiyonu ile doğmadan hemen önce takımı veriyoruz
            NetworkObject character = Runner.Spawn(_characterPrefab, spawnPos, Quaternion.identity, Object.InputAuthority, (runner, obj) =>
            {
                // Bu süslü parantezlerin içi, karakter haritaya düşmeden ve Player.Spawned() ÇALIŞMADAN ÖNCE çalışır!
                Player physicalPlayerScript = obj.GetComponent<Player>();
                if (physicalPlayerScript != null)
                {
                    physicalPlayerScript.PlayerTeam = team;
                }
            });

            // Doğmuş olan karakteri sunucu hafızasına kaydet
            Runner.SetPlayerObject(Object.InputAuthority, character);
            IsSpawned = true;

            Debug.Log($"[PlayerState] Oyuncu {Object.InputAuthority.RawEncoded} {team} takımında, {spawnPos} konumunda doğdu.");
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_RequestTeamChange(Team newTeam)
    {
        if (Object.HasStateAuthority)
        {
            // Runner üzerinden bu oyuncunun (InputAuthority) asıl karakter objesini buluyoruz.
            // RPC_RequestSpawn içinde Runner.SetPlayerObject ile bunu kaydetmiştik.
            if (Runner.TryGetPlayerObject(Object.InputAuthority, out NetworkObject characterObj))
            {
                Player physicalPlayerScript = characterObj.GetComponent<Player>();
                if (physicalPlayerScript != null)
                {
                    // Eğer oyuncu zaten seçtiği takımdaysa hiçbir şey yapma
                    if (physicalPlayerScript.PlayerTeam == newTeam) return;

                    // 1. Takımı Güncelle
                    physicalPlayerScript.PlayerTeam = newTeam;

                    // 2. Yeni takımın başlangıç noktasına anında ışınla
                    if (GameManager.Instance != null)
                    {
                        Transform teamSpawn = GameManager.Instance.GetSpawnPointForTeam(newTeam);
                        if (teamSpawn != null)
                        {
                            physicalPlayerScript.transform.position = teamSpawn.position;
                            physicalPlayerScript.transform.rotation = teamSpawn.rotation;
                        }

                        // İSTEĞE BAĞLI: Takım değiştirdiği için canını ve zırhını sıfırla (fulle)
                        physicalPlayerScript.Health = 100;
                        physicalPlayerScript.Armor = 100;
                        physicalPlayerScript.ClearDamageHistory();

                        // Silah mermilerini sıfırla
                        if (physicalPlayerScript.EquippedWeapon != null)
                        {
                            physicalPlayerScript.EquippedWeapon.ResetAmmo();
                        }

                        // 3. Takım dengeleri değiştiği için (eski takımda kimse kalmamış olabilir)
                        // kazanma durumunu tekrar kontrol ettir.
                        GameManager.Instance.CheckWinCondition();
                    }

                    Debug.Log($"[PlayerState] Oyuncu {Object.InputAuthority.RawEncoded} {newTeam} takımına geçti!");
                }
            }
        }
    }
}
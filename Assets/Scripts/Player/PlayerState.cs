using Fusion;
using UnityEngine;
using static GlobalVariables; 

public class PlayerState : NetworkBehaviour
{
    public static PlayerState Local; // Arayüzün bu objeyi bulması için referans


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

            Debug.Log($"[PlayerState] Oyuncu {Object.InputAuthority.RawEncoded} {team} takımında, {spawnPos} konumunda doğdu.");
        }
    }
}
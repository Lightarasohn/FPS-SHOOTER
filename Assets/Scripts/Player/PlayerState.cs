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
            // Sahnedeki SpawnManager'a ulaşıp noktaları alıyoruz
            if (SpawnManager.Instance != null)
            {
                spawnPos = (team == Team.Red)
                    ? SpawnManager.Instance.redSpawnPoint.position
                    : SpawnManager.Instance.blueSpawnPoint.position;
            }
            else
            {
                Debug.LogError("[PlayerState] Sahnede SpawnManager bulunamadı! Lütfen sahneye ekleyin.");
            }

            // Karakteri doğur ve yetkiyi isteği gönderen oyuncuya ver
            NetworkObject character = Runner.Spawn(_characterPrefab, spawnPos, Quaternion.identity, Object.InputAuthority);

            // Doğmuş olan karakteri sunucu hafızasına kaydediyoruz ki BasicSpawner içindeki OnInput onu bulabilsin
            Runner.SetPlayerObject(Object.InputAuthority, character);

            Debug.Log($"[PlayerState] Oyuncu {Object.InputAuthority.RawEncoded} {team} takımında doğdu.");
        }
    }
}
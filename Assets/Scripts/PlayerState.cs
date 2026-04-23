using Fusion;
using UnityEngine;
using static GlobalVariables; // Team enum'u için

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
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_RequestSpawn(Team team)
    {
        if (Object.HasStateAuthority)
        {
            // Takıma göre pozisyon veriyoruz (Test için direkt koordinat yazdım)
            Vector3 spawnPos = (team == Team.Red) ? new Vector3(-5, 1, 0) : new Vector3(5, 1, 0);

            // Karakteri doğur ve yetkiyi isteği gönderen oyuncuya ver
            NetworkObject character = Runner.Spawn(_characterPrefab, spawnPos, Quaternion.identity, Object.InputAuthority);

            // Doğmuş olan karakteri sunucu hafızasına kaydediyoruz ki BasicSpawner içindeki OnInput onu bulabilsin
            Runner.SetPlayerObject(Object.InputAuthority, character);

            Debug.Log($"[PlayerState] Oyuncu {Object.InputAuthority.RawEncoded} {team} takımında doğdu.");
        }
    }
}
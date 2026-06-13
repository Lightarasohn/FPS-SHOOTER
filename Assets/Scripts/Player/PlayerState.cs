using Fusion;
using UnityEngine;
using static GlobalVariables;

public class PlayerState : NetworkBehaviour
{
    public static PlayerState Local; // Arayüzün bu objeyi bulması için referans

    [Header("Team Character Pools")]
    [Tooltip("Kırmızı takım için rastgele seçilecek karakter prefabları")]
    [SerializeField] private NetworkPrefabRef[] _redTeamCharacterPrefabs;

    [Tooltip("Mavi takım için rastgele seçilecek karakter prefabları")]
    [SerializeField] private NetworkPrefabRef[] _blueTeamCharacterPrefabs;

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

            // --- TAKIMA GÖRE RASTGELE KARAKTER SEÇİMİ ---
            NetworkPrefabRef selectedPrefab = default;

            if (team == Team.Red && _redTeamCharacterPrefabs.Length > 0)
            {
                int randomIndex = Random.Range(0, _redTeamCharacterPrefabs.Length);
                selectedPrefab = _redTeamCharacterPrefabs[randomIndex];
            }
            else if (team == Team.Blue && _blueTeamCharacterPrefabs.Length > 0)
            {
                int randomIndex = Random.Range(0, _blueTeamCharacterPrefabs.Length);
                selectedPrefab = _blueTeamCharacterPrefabs[randomIndex];
            }

            // Eğer inspector'dan prefab atanmamışsa hata ver (NullReference önlemi)
            if (selectedPrefab == default)
            {
                Debug.LogError($"[PlayerState] {team} takımı için karakter havuzu boş! Lütfen PlayerProxy prefabı üzerinden karakterleri ekleyin.");
                return;
            }
            // ---------------------------------------------

            // DÜZELTİLEN KISIM BURASI: _characterPrefab yerine selectedPrefab kullanıyoruz
            NetworkObject character = Runner.Spawn(selectedPrefab, spawnPos, Quaternion.identity, Object.InputAuthority, (runner, obj) =>
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
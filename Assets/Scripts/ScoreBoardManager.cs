using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;
using UnityEngine.InputSystem; // Senin diğer scriptte kullandığın yeni sistem kütüphanesi

public class ScoreboardManager : MonoBehaviour
{
    [Header("UI Elementleri")]
    public GameObject scoreboardPanel;
    public Transform redTeamContainer;
    public Transform blueTeamContainer;
    public GameObject playerRowPrefab;

    private List<GameObject> spawnedRows = new List<GameObject>();

    void Start()
    {
        // Başlangıçta skor panosunu kapalı tut
        scoreboardPanel.SetActive(false);
    }

    void Update()
    {
        // GÜVENLİK: Eğer bilgisayara klavye takılı değilse kodun çökmesini engeller
        if (Keyboard.current == null) return;

        // YENİ SİSTEM KLAVYE KONTROLÜ
        // Tab tuşuna ilk basıldığı an (Aç)
        if (Keyboard.current.tabKey.wasPressedThisFrame)
        {
            scoreboardPanel.SetActive(true);
            UpdateScoreboard();
        }
        // Tab tuşundan elini çektiği an (Kapat)
        else if (Keyboard.current.tabKey.wasReleasedThisFrame)
        {
            scoreboardPanel.SetActive(false);
        }

        // Eğer menü açıksa skorları güncellemeye devam et
        if (scoreboardPanel.activeSelf)
        {
            UpdateScoreboard();
        }
    }

    private void UpdateScoreboard()
    {
        if (GameManager.Instance == null) return;

        // Önce eski satırları temizle
        foreach (var row in spawnedRows)
        {
            Destroy(row);
        }
        spawnedRows.Clear();

        var players = GameManager.Instance.ActivePlayers;

        // Kırmızı Takımı Kill sayısına göre büyükten küçüğe sırala
        var redTeam = players.Where(p => p.PlayerTeam == GlobalVariables.Team.Red)
                             .OrderByDescending(p => p.Kills)
                             .ToList();

        // Mavi Takımı Kill sayısına göre büyükten küçüğe sırala
        var blueTeam = players.Where(p => p.PlayerTeam == GlobalVariables.Team.Blue)
                              .OrderByDescending(p => p.Kills)
                              .ToList();

        // Kırmızı takımı listele
        foreach (var player in redTeam)
        {
            CreatePlayerRow(player, redTeamContainer);
        }

        // Mavi takımı listele
        foreach (var player in blueTeam)
        {
            CreatePlayerRow(player, blueTeamContainer);
        }
    }

    private void CreatePlayerRow(Player player, Transform container)
    {
        GameObject row = Instantiate(playerRowPrefab, container);
        spawnedRows.Add(row);

        TextMeshProUGUI[] texts = row.GetComponentsInChildren<TextMeshProUGUI>();

        texts[1].text = player.PlayerName.ToString();
        texts[2].text = player.Kills.ToString();
        texts[3].text = player.Deaths.ToString();
        texts[4].text = player.Assists.ToString();

        if (player.IsAlive)
        {
            texts[0].text = "Alive";
            texts[0].color = Color.green;
            texts[1].color = Color.white;
        }
        else
        {
            texts[0].text = "DEAD";
            texts[0].color = Color.red;

            texts[1].color = Color.gray;
            texts[2].color = Color.gray;
            texts[3].color = Color.gray;
            texts[4].color = Color.gray;
        }
    }
}
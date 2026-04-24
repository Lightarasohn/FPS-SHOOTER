using UnityEngine;
using UnityEngine.UI;
using static GlobalVariables;

public class TeamSelectUI : MonoBehaviour
{
    public static TeamSelectUI Instance;

    [Header("UI Elemanları")]
    public GameObject teamSelectionPanel; 
    public Button redButton;
    public Button blueButton;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // 1. Oyun ilk açıldığında menüyü GİZLE
        if (teamSelectionPanel != null)
        {
            teamSelectionPanel.SetActive(false);
        }

        redButton.onClick.AddListener(() => OnTeamSelected(Team.Red));
        blueButton.onClick.AddListener(() => OnTeamSelected(Team.Blue));
    }

    // 2. Oyuna bağlanıldığında bu fonksiyon dışarıdan çağrılacak
    public void ShowMenu()
    {
        teamSelectionPanel.SetActive(true);

        // Fareyi görünür yap 
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void OnTeamSelected(Team team)
    {
        if (PlayerState.Local != null)
        {
            PlayerState.Local.RPC_RequestSpawn(team);

            // Seçim yapıldıktan sonra menüyü GİZLE
            teamSelectionPanel.SetActive(false);
        }
    }
}
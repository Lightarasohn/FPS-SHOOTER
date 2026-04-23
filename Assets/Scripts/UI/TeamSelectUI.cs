using UnityEngine;
using UnityEngine.UI;
using static GlobalVariables;

public class TeamSelectUI : MonoBehaviour
{
    public Button redButton;
    public Button blueButton;

    void Start()
    {
        redButton.onClick.AddListener(() => OnTeamSelected(Team.Red));
        blueButton.onClick.AddListener(() => OnTeamSelected(Team.Blue));
    }

    void OnTeamSelected(Team team)
    {
        // Kendi ağ temsilcimiz sahnede doğduysa komutu gönder
        if (PlayerState.Local != null)
        {
            PlayerState.Local.RPC_RequestSpawn(team);
            gameObject.SetActive(false); // Arayüzü kapat
        }
        else
        {
            Debug.LogWarning("Temsilci henüz hazır değil, lütfen 1 saniye bekleyip tekrar tıklayın.");
        }
    }
}
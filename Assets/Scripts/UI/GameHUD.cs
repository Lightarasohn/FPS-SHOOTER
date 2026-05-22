using UnityEngine;
using TMPro;
using Fusion;
using Unity.VisualScripting;

public class GameHUD : MonoBehaviour
{
    [Header("UI Elementleri")]
    public TextMeshProUGUI timeText;
    public TextMeshProUGUI redScoreText;
    public TextMeshProUGUI blueScoreText;
    public TextMeshProUGUI stateMessageText;

    public static GameHUD Instance;


    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
      gameObject.SetActive(false); 
    }
    private void Update()
    {
        // 1. KONTROL: GameManager var mı ve Fusion ağı (Runner) aktif mi?
        if (!gameObject.activeSelf)
        {
       
            return; // Aşağıdaki skor güncelleme kodlarını boşuna çalıştırma
        }

        // DÜZELTME: GameManager henüz AĞA BAĞLANMADIYSA hiçbir şey yapma!
        if (GameManager.Instance == null || !GameManager.Instance.IsReady) return;

        // Değerleri güncelle
        UpdateScores();
        UpdateTime();
        UpdateStateMessage();
    }

    private void UpdateScores()
    {
        redScoreText.text = GameManager.Instance.TeamRedScore.ToString();
        blueScoreText.text = GameManager.Instance.TeamBlueScore.ToString();
    }

    private void UpdateTime()
    {
        NetworkRunner runner = GameManager.Instance.Runner;
        float? remainingTime = GameManager.Instance.RoundTimer.RemainingTime(runner);

        if (remainingTime.HasValue)
        {
            int minutes = Mathf.FloorToInt(remainingTime.Value / 60f);
            int seconds = Mathf.FloorToInt(remainingTime.Value % 60f);
            timeText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }
        else
        {
            timeText.text = "00:00";
        }
    }

    private void UpdateStateMessage()
    {
        switch (GameManager.Instance.CurrentState)
        {
            case GlobalVariables.RoundState.WaitingForPlayers:
                stateMessageText.text = "OYUNCULAR BEKLENIYOR...";
                break;
            case GlobalVariables.RoundState.PreRound:
                stateMessageText.text = "HAZIRLIK ASAMASI";
                break;
            case GlobalVariables.RoundState.Playing:
                stateMessageText.text = "";
                break;
            case GlobalVariables.RoundState.RoundEnd:
                stateMessageText.text = "ROUND BITTI!";
                break;
        }
    }
    public void SetVisible(bool isVisible)
    {
        gameObject.SetActive(isVisible);
    }
}
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static GlobalVariables;

public class BuffDebuffScript : MonoBehaviour
{
    [SerializeField]
    public GameObject BuffDebuffPanel;

    [Header("Sol Augment")]
    [SerializeField] public TMP_Text LeftAugment_Header;
    [SerializeField] public TMP_Text LeftAugment_Description;

    [Header("Orta Augment")]
    [SerializeField] public TMP_Text MiddleAugment_Header;
    [SerializeField] public TMP_Text MiddleAugment_Description;

    [Header("Sağ Augment")]
    [SerializeField] public TMP_Text RightAugment_Header;
    [SerializeField] public TMP_Text RightAugment_Description;

    // YENİ: Raunt takibi ve RPC gecikme kilidi
    private RoundState _lastKnownState;
    private bool _hasSelectedThisRound = false;
    private string _leftAugmentCodeName;
    private string _middleAugmentCodeName;
    private string _rightAugmentCodeName;

    // YENİ: Senin anlattığın süzgeç mantığı burada çalışıyor
    private void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsReady || GameManager.Instance.Object == null || !GameManager.Instance.Object.IsValid) return;

        RoundState currentState = GameManager.Instance.CurrentState;

        // 1. Eğer yepyeni bir Freeze Time (PreRound) başladıysa, kilitleri sıfırla
        if (currentState == RoundState.PreRound && _lastKnownState != RoundState.PreRound)
        {
            _hasSelectedThisRound = false;
        }
        _lastKnownState = currentState;

        // 2. FREEZE TIME İÇERİSİNDE MİYİZ?
        if (currentState == RoundState.PreRound)
        {
            // 3. OYUNCU SAHNEDE OYNUYOR MU? (InputAuthority bizde mi?)
            Player localPlayer = GetLocalPlayer();

            if (localPlayer != null)
            {
                // 4. OYUNCUNUN AUGMENTİ YOK MU VEYA SEÇİM YAPMAMIŞ MI?
                if (localPlayer.ActiveAugment == null && !_hasSelectedThisRound)
                {
                    // Panel kapalıysa aç ve içerikleri doldur
                    if (!BuffDebuffPanel.activeSelf)
                    {
                        FillButtonsContents();
                        BuffDebuffPanel.SetActive(true);

                        Cursor.lockState = CursorLockMode.None;
                        Cursor.visible = true;
                    }
                }
            }
        }
        else
        {
            // Freeze Time bittiyse (Maç başladıysa) ve ekran hala açıksa ZORLA KAPAT
            if (BuffDebuffPanel.activeSelf)
            {
                BuffDebuffPanel.SetActive(false);
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }

    // YARDIMCI METOD: Sahnede bizim kontrol ettiğimiz (Takım seçip doğmuş olan) karakteri bulur
    private Player GetLocalPlayer()
    {
        Player[] players = FindObjectsByType<Player>(FindObjectsSortMode.None);
        foreach (Player p in players)
        {
            if (p.HasInputAuthority) return p;
        }
        return null;
    }

    public AugmentType SelectBuffOrDebuff()
    {
        return (AugmentType)Random.Range(0, 2);
    }

    public BuffDebuff[] GetAugmentsRandomly(AugmentType augmentType)
    {
        var random = new System.Random();
        List<BuffDebuff> choosenAugments;
        if (augmentType == AugmentType.Debuff)
        {
            choosenAugments = ALL_BUFFS_AND_DEBUFFS.Where(a => a.Type == augmentType)
                                                   .OrderBy(a => Random.value)
                                                   .Take(3)
                                                   .ToList();
        }
        else
        {
            choosenAugments = ALL_BUFFS_AND_DEBUFFS.Where(a => a.Type == augmentType || a.Type == AugmentType.Normal)
                                                   .OrderBy(a => Random.value)
                                                   .Take(3)
                                                   .ToList();
        }

        return choosenAugments.ToArray();
    }

    public void FillButtonsContents()
    {
        AugmentType selectedType = SelectBuffOrDebuff();
        BuffDebuff[] choosenAugments = GetAugmentsRandomly(selectedType);

        LeftAugment_Header.text = choosenAugments[0].Name;
        LeftAugment_Description.text = choosenAugments[0].Description;
        _leftAugmentCodeName = choosenAugments[0].GetType().Name;

        MiddleAugment_Header.text = choosenAugments[1].Name;
        MiddleAugment_Description.text = choosenAugments[1].Description;
        _middleAugmentCodeName = choosenAugments[1].GetType().Name;

        RightAugment_Header.text = choosenAugments[2].Name;
        RightAugment_Description.text = choosenAugments[2].Description;
        _rightAugmentCodeName = choosenAugments[2].GetType().Name;
    }

    public void ClearButtonContents()
    {
        LeftAugment_Header.text = "BAŞLIK";
        LeftAugment_Description.text = "AÇIKLAMA";

        MiddleAugment_Header.text = "BAŞLIK";
        MiddleAugment_Description.text = "AÇIKLAMA";

        RightAugment_Header.text = "BAŞLIK";
        RightAugment_Description.text = "AÇIKLAMA";
    }

    public void OnAugmentButtonClicked(string selectedBuffName)
    {
        Player localPlayer = GetLocalPlayer();

        if (localPlayer != null)
        {
            // RPC Gecikmesi kilidini aktif et ki Update döngüsü ekranı tekrar açmasın
            _hasSelectedThisRound = true;

            // SUNUCUYA HABER VER
            localPlayer.RequestBuff(selectedBuffName);

            // ARAYÜZÜ TEMİZLE VE KAPAT
            ClearButtonContents();
            BuffDebuffPanel.SetActive(false);

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
    public void OnClick_LeftButton() { OnAugmentButtonClicked(_leftAugmentCodeName); }
    public void OnClick_MiddleButton() { OnAugmentButtonClicked(_middleAugmentCodeName); }
    public void OnClick_RightButton() { OnAugmentButtonClicked(_rightAugmentCodeName); }
}
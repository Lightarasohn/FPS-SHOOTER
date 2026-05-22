using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using static GlobalVariables;

public class BuffDebuffScript : MonoBehaviour
{
    [SerializeField] public GameObject BuffDebuffPanel;
    [SerializeField] private WeaponSelectionScript _weaponSelectionScript;

    [Header("Sol Augment")]
    [SerializeField] public TMP_Text LeftAugment_Header;
    [SerializeField] public TMP_Text LeftAugment_Description;

    [Header("Orta Augment")]
    [SerializeField] public TMP_Text MiddleAugment_Header;
    [SerializeField] public TMP_Text MiddleAugment_Description;

    [Header("Sağ Augment")]
    [SerializeField] public TMP_Text RightAugment_Header;
    [SerializeField] public TMP_Text RightAugment_Description;

    private RoundState _lastKnownState;
    private bool _hasSelectedThisRound = false;
    private string _leftAugmentCodeName;
    private string _middleAugmentCodeName;
    private string _rightAugmentCodeName;

    // Otomatik seçim için zamanlayıcı
    private float _autoSelectTimer = 0f;
    private const float AUTO_SELECT_DELAY = 14f;

    private void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsReady ||
            GameManager.Instance.Object == null || !GameManager.Instance.Object.IsValid) return;

        RoundState currentState = GameManager.Instance.CurrentState;

        if (currentState == RoundState.PreRound && _lastKnownState != RoundState.PreRound)
        {
            _hasSelectedThisRound = false;
            _autoSelectTimer = 0f;
        }
        _lastKnownState = currentState;

        if (currentState == RoundState.PreRound)
        {
            Player localPlayer = GetLocalPlayer();

            if (localPlayer != null)
            {
                // YENİ: WeaponSelectionScript var mı ve aktif mi diye ekstra güvenlik kontrolü
                bool weaponPhaseComplete = _weaponSelectionScript != null && _weaponSelectionScript.HasSelectedWeaponThisRound;

                if (weaponPhaseComplete && localPlayer.ActiveAugment == null && !_hasSelectedThisRound)
                {
                    if (!BuffDebuffPanel.activeSelf)
                    {
                        FillButtonsContents();
                        BuffDebuffPanel.SetActive(true);
                        Cursor.lockState = CursorLockMode.None;
                        Cursor.visible = true;
                        _autoSelectTimer = 0f;
                    }

                    _autoSelectTimer += Time.deltaTime;
                    if (_autoSelectTimer >= AUTO_SELECT_DELAY)
                    {
                        AutoSelectAugment();
                    }
                }
            }
        }
        else
        {
            if (BuffDebuffPanel.activeSelf)
            {
                BuffDebuffPanel.SetActive(false);
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }

    private Player GetLocalPlayer()
    {
        Player[] players = FindObjectsByType<Player>(FindObjectsInactive.Include, FindObjectsSortMode.None);
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

    private void AutoSelectAugment()
    {
        string[] options = { _leftAugmentCodeName, _middleAugmentCodeName, _rightAugmentCodeName };
        string randomPick = options[Random.Range(0, options.Length)];
        Debug.Log($"[BuffDebuff] Süre doldu! Otomatik seçim: {randomPick}");
        OnAugmentButtonClicked(randomPick);
    }

    public void OnAugmentButtonClicked(string selectedBuffName)
    {
        Player localPlayer = GetLocalPlayer();
        if (localPlayer == null) return;

        _hasSelectedThisRound = true;
        _autoSelectTimer = 0f;

        localPlayer.RequestBuff(selectedBuffName);

        ClearButtonContents();
        BuffDebuffPanel.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void OnClick_LeftButton() { OnAugmentButtonClicked(_leftAugmentCodeName); }
    public void OnClick_MiddleButton() { OnAugmentButtonClicked(_middleAugmentCodeName); }
    public void OnClick_RightButton() { OnAugmentButtonClicked(_rightAugmentCodeName); }
}
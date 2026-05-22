using System.Linq;
using TMPro;
using UnityEngine;
using static GlobalVariables;

public class WeaponSelectionScript : MonoBehaviour
{
    [SerializeField] public GameObject WeaponSelectionPanel;

    [Header("Sol Silah")]
    [SerializeField] public TMP_Text LeftWeapon_Header;
    [SerializeField] public TMP_Text LeftWeapon_Description;

    [Header("Orta Silah")]
    [SerializeField] public TMP_Text MiddleWeapon_Header;
    [SerializeField] public TMP_Text MiddleWeapon_Description;

    [Header("Sağ Silah")]
    [SerializeField] public TMP_Text RightWeapon_Header;
    [SerializeField] public TMP_Text RightWeapon_Description;

    private RoundState _lastKnownState;
    private bool _hasSelectedWeaponThisRound = false;
    private string _leftWeaponID;
    private string _middleWeaponID;
    private string _rightWeaponID;

    // Otomatik seçim için zamanlayıcı
    private float _autoSelectTimer = 0f;
    private const float AUTO_SELECT_DELAY = 14f;

    // BuffDebuffScript'in silah fazının bitip bitmediğini kontrol etmesi için
    public bool HasSelectedWeaponThisRound => _hasSelectedWeaponThisRound;

    private void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsReady ||
            GameManager.Instance.Object == null || !GameManager.Instance.Object.IsValid) return;

        RoundState currentState = GameManager.Instance.CurrentState;

        // Yeni PreRound başladıysa kilitleri sıfırla
        if (currentState == RoundState.PreRound && _lastKnownState != RoundState.PreRound)
        {
            _hasSelectedWeaponThisRound = false;
            _autoSelectTimer = 0f;
        }
        _lastKnownState = currentState;

        if (currentState == RoundState.PreRound)
        {
            Player localPlayer = GetLocalPlayer();

            if (localPlayer != null)
            {
                // Silah seçimi henüz yapılmadıysa paneli aç
                if (!_hasSelectedWeaponThisRound)
                {
                    if (!WeaponSelectionPanel.activeSelf)
                    {
                        FillWeaponButtons();
                        WeaponSelectionPanel.SetActive(true);

                        Cursor.lockState = CursorLockMode.None;
                        Cursor.visible = true;

                        _autoSelectTimer = 0f;
                    }

                    // Otomatik seçim sayacı
                    _autoSelectTimer += Time.deltaTime;
                    if (_autoSelectTimer >= AUTO_SELECT_DELAY)
                    {
                        AutoSelectWeapon();
                    }
                }
            }
        }
        else
        {
            // PreRound bitti, panel açıksa zorla kapat
            if (WeaponSelectionPanel.activeSelf)
            {
                WeaponSelectionPanel.SetActive(false);
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }

    private Player GetLocalPlayer()
    {
        Player[] players = FindObjectsByType<Player>(FindObjectsSortMode.None);
        foreach (Player p in players)
        {
            if (p.HasInputAuthority) return p;
        }
        return null;
    }

    private Weapon CreateWeaponFromID(WeaponID id)
    {
        switch (id)
        {
            case WeaponID.AK47: return new AK47();
            case WeaponID.M4A4: return new M4A4();
            case WeaponID.MP5: return new MP5();
            case WeaponID.MP9: return new MP9();
            case WeaponID.Baretta92: return new Baretta92();
            default: return null;
        }
    }

    public Weapon[] GetWeaponsRandomly()
    {
        var allWeaponIDs = System.Enum.GetValues(typeof(WeaponID))
                                      .Cast<WeaponID>()
                                      .Where(id => id != WeaponID.None)
                                      .OrderBy(_ => Random.value)
                                      .Take(3)
                                      .ToList();

        return allWeaponIDs.Select(id => CreateWeaponFromID(id))
                           .Where(w => w != null)
                           .ToArray();
    }

    public void FillWeaponButtons()
    {
        Weapon[] chosenWeapons = GetWeaponsRandomly();

        if (chosenWeapons.Length < 3)
        {
            Debug.LogError("[WeaponSelection] Yeterli silah verisi bulunamadı!");
            return;
        }

        LeftWeapon_Header.text = chosenWeapons[0].Name;
        LeftWeapon_Description.text = BuildWeaponDescription(chosenWeapons[0]);
        _leftWeaponID = chosenWeapons[0].ID.ToString();

        MiddleWeapon_Header.text = chosenWeapons[1].Name;
        MiddleWeapon_Description.text = BuildWeaponDescription(chosenWeapons[1]);
        _middleWeaponID = chosenWeapons[1].ID.ToString();

        RightWeapon_Header.text = chosenWeapons[2].Name;
        RightWeapon_Description.text = BuildWeaponDescription(chosenWeapons[2]);
        _rightWeaponID = chosenWeapons[2].ID.ToString();
    }

    private string BuildWeaponDescription(Weapon w)
    {
        return $"Hasar: {w.Damage}\n" +
               $"Şarjör: {w.MagCapacity} / {w.MagAmount} mag\n" +
               $"Ateş Hızı: {(1f / w.FireRate):F0} ATK/s\n" +
               $"Yeniden Yükleme: {w.ReloadTime:F1}s";
    }

    public void ClearWeaponButtons()
    {
        LeftWeapon_Header.text = "BAŞLIK"; LeftWeapon_Description.text = "AÇIKLAMA";
        MiddleWeapon_Header.text = "BAŞLIK"; MiddleWeapon_Description.text = "AÇIKLAMA";
        RightWeapon_Header.text = "BAŞLIK"; RightWeapon_Description.text = "AÇIKLAMA";
    }

    private void AutoSelectWeapon()
    {
        string[] options = { _leftWeaponID, _middleWeaponID, _rightWeaponID };
        string randomPick = options[Random.Range(0, options.Length)];
        Debug.Log($"[WeaponSelection] Süre doldu! Otomatik seçim: {randomPick}");
        OnWeaponButtonClicked(randomPick);
    }

    public void OnWeaponButtonClicked(string selectedWeaponIDStr)
    {
        Player localPlayer = GetLocalPlayer();
        if (localPlayer == null) return;

        _hasSelectedWeaponThisRound = true;
        _autoSelectTimer = 0f;

        localPlayer.RequestWeapon(selectedWeaponIDStr);

        ClearWeaponButtons();
        WeaponSelectionPanel.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void OnClick_LeftButton() { OnWeaponButtonClicked(_leftWeaponID); }
    public void OnClick_MiddleButton() { OnWeaponButtonClicked(_middleWeaponID); }
    public void OnClick_RightButton() { OnWeaponButtonClicked(_rightWeaponID); }
}
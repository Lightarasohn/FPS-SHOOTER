using UnityEngine;
using TMPro;

public class PlayerHUD : MonoBehaviour
{
    public static PlayerHUD Instance; // Her yerden erişim için

    [Header("UI Referansları")]
    public TextMeshProUGUI CanText;
    public TextMeshProUGUI MermiText;

    // YENİ: Asıl nişangah yöneticimizi buraya sürüklüyoruz
    public CrosshairManager HudCrosshair;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // YENİ: Oyun başladığında takım seçme ekranı geleceği için HUD kendi kendini kapatsın.
        SetVisible(false);
    }

    public void ArayuzuGuncelle(int can, int mevcutMermi, int yedekMermi)
    {
        if (CanText != null) CanText.text = "+" + can.ToString();
        if (MermiText != null) MermiText.text = mevcutMermi.ToString() + " / " + yedekMermi.ToString();
    }

    // YENİ: HUD'un tamamını açıp kapatacak metod
    public void SetVisible(bool isVisible)
    {
        gameObject.SetActive(isVisible);
    }
}
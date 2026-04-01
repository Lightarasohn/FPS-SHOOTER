using UnityEngine;
using UnityEngine.UI;
using static GlobalVariables;

public class CrosshairManager : MonoBehaviour
{
    [Header("UI Çizgi Referansları")]
    public RectTransform CrosshairRoot;
    public RectTransform TopLine;
    public RectTransform BottomLine;
    public RectTransform LeftLine;
    public RectTransform RightLine;

    [Header("Ölçeklendirme Çarpanı")]
    [Tooltip("0-1 arasındaki normalize veriyi ekrandaki piksele çeviren ana çarpan.")]
    public float BaseScale = 50f;

    public void ApplyCrosshairSettings(Crosshair data)
    {
        if (data == null) return;

        // --- MATEMATİK: Normalizasyonu Piksele Çevir ---
        float lengthPx = data.Length * BaseScale;
        float widthPx = data.Width * BaseScale;
        float spacePx = data.Space * BaseScale;

        // 1. ŞEKİL (Döndürme İşlemleri)
        CrosshairRoot.localRotation = Quaternion.identity;
        TopLine.gameObject.SetActive(true);

        switch (data.CrosshairType)
        {
            case CrosshairType.Default:
                break;
            case CrosshairType.X:
                CrosshairRoot.localRotation = Quaternion.Euler(0, 0, 45);
                break;
            case CrosshairType.Triangle:
                TopLine.gameObject.SetActive(false);
                RightLine.gameObject.SetActive(false);
                CrosshairRoot.localRotation = Quaternion.Euler(0, 0, 45);
                break;
        }

        // 2. BOYUT VE BOŞLUK AYARLARI (Artık çevrilmiş px değerlerini kullanıyoruz)

        // Üst Çizgi
        TopLine.sizeDelta = new Vector2(widthPx, lengthPx);
        TopLine.localPosition = new Vector3(0, spacePx + lengthPx, 0);

        // Alt Çizgi
        BottomLine.sizeDelta = new Vector2(widthPx, lengthPx);
        BottomLine.localPosition = new Vector3(0, -spacePx - lengthPx, 0);

        // Sol Çizgi
        LeftLine.sizeDelta = new Vector2(lengthPx, widthPx);
        LeftLine.localPosition = new Vector3(-spacePx - lengthPx, 0, 0);

        // Sağ Çizgi
        RightLine.sizeDelta = new Vector2(lengthPx, widthPx);
        RightLine.localPosition = new Vector3(spacePx + lengthPx, 0, 0);
    }
}
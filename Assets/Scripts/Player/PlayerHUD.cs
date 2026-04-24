using UnityEngine;
using TMPro;

public class PlayerHUD : MonoBehaviour
{
    [Header("UI Referansları")]
    public TextMeshProUGUI CanText;      // Sahnendeki "Health" objesi
    public TextMeshProUGUI MermiText;    // Sahnendeki "CurrentAmmo" objesi (Artık ikisini de tutacak)

    // Player.cs içindeki Render() fonksiyonu her karede burayı çağırır
    public void ArayuzuGuncelle(int can, int mevcutMermi, int yedekMermi)
    {
        if (CanText != null)
        {
            CanText.text = "+" + can.ToString();
        }

        if (MermiText != null)
        {
            // İki değeri arasına " / " koyarak tek bir yazıda birleştiriyoruz
            MermiText.text = mevcutMermi.ToString() + " / " + yedekMermi.ToString();
        }
    }
}
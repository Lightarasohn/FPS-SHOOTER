using UnityEngine;
using UnityEngine.UI;
using TMPro; // TextMeshPro kullanmak için bu kütüphane şart

public class SliderValueDisplay : MonoBehaviour
{
    [Header("Referanslar")]
    public Slider targetSlider;
    public TextMeshProUGUI valueText;

    [Header("Ayarlar")]
    [Tooltip("F0: Tam sayı, F1: 1 ondalık, F2: 2 ondalık gösterir")]
    public string format = "F2";

    void Start()
    {
        if (targetSlider != null && valueText != null)
        {
            // Oyuna girildiğinde text'i ilk değere göre güncelle
            UpdateText(targetSlider.value);

            // Slider'ın değeri her değiştiğinde UpdateText fonksiyonunu otomatik çalıştır
            targetSlider.onValueChanged.AddListener(UpdateText);
        }
    }

    // Slider'dan gelen yeni değeri alıp Text'e yazdıran fonksiyon
    private void UpdateText(float value)
    {
        // ToString(format) ile sayıyı istediğimiz formata çeviriyoruz
        valueText.text = value.ToString(format);
    }
}
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

// Harita bilgilerini Inspector'da tanımlayabilmemiz için gerekli veri yapısı
[Serializable]
public struct MapData
{
    public string MapName;
    public Sprite MapPreviewImage; // Haritanın arka plan resmi
    public int SceneBuildIndex;    // Unity'deki sahne numarası (Aşağıda açıklayacağım)
}

public class MapEntryUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TMP_Text _mapNameText;
    [SerializeField] private Image _mapBackgroundImage;
    [SerializeField] private Button _selectMapButton;

    /// <summary>
    /// BasicSpawner içindeki harita listesinden gelen veriyle UI'ı doldurur.
    /// </summary>
    public void Setup(MapData mapData, Action onMapSelected)
    {
        if (_mapNameText != null)
        {
            _mapNameText.text = mapData.MapName;
        }

        if (_mapBackgroundImage != null && mapData.MapPreviewImage != null)
        {
            _mapBackgroundImage.sprite = mapData.MapPreviewImage;
        }

        if (_selectMapButton != null)
        {
            _selectMapButton.onClick.RemoveAllListeners();
            _selectMapButton.onClick.AddListener(() => onMapSelected?.Invoke());
        }
    }
}
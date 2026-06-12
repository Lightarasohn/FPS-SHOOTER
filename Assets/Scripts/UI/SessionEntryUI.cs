using Fusion;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SessionEntryUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TMP_Text _roomNameText;     // Odanın adını yazdıracağınız Text componenti
    [SerializeField] private TMP_Text _playerCountText;  // Oyuncu sayısını (Örn: 1/8) yazdıracağınız Text componenti
    [SerializeField] private Button _joinButton;         // Odaya katılmayı tetikleyen Button componenti

    // --- YENİ: Lobide açık odaların yanında harita resmi gösterebilmek için gerekli UI alanı ---
    [SerializeField] private Image _mapPreviewImage;

    /// <summary>
    /// Odaların verileri lobiden geldikçe bu satırın UI elementlerini günceller.
    /// </summary>
    // --- GÜNCELLENDİ: Parametrelere Sprite mapSprite eklendi ---
    public void Setup(SessionInfo session, Sprite mapSprite, Action onJoinPressed)
    {
        // Ekrana kısa benzersiz oda adını basıyoruz
        if (_roomNameText != null)
        {
            // Buradaki hata giderildi (orijinal kodundaki text ataması korundu)
            _roomNameText.text = session.Name;
        }

        // Mevcut Oyuncu / Maksimum Oyuncu bilgisini yazıyoruz
        if (_playerCountText != null)
        {
            _playerCountText.text = $"{session.PlayerCount} / {session.MaxPlayers}";
        }

        // --- YENİ: Eğer harita resmi geldiyse bunu UI görgeline basıyoruz ---
        if (_mapPreviewImage != null && mapSprite != null)
        {
            _mapPreviewImage.sprite = mapSprite;
        }
        // ------------------------------------------------------------------

        // Önceki buton dinleyicilerini sıfırlayıp yenisini tanımlıyoruz
        if (_joinButton != null)
        {
            _joinButton.onClick.RemoveAllListeners();
            _joinButton.onClick.AddListener(() => onJoinPressed?.Invoke());
        }
    }
}
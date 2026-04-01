using Fusion;
using System;
using UnityEngine;

// İstemciden sunucuya her tick'te gönderilecek olan veri paketi (Struct)
public struct NetworkInput : INetworkInput
{
    // 1. Yön Tuşları (W, A, S, D)
    // X ekseni Sağ/Sol (A,D), Y ekseni İleri/Geri (W,S) hareketini tutar. (8 Byte)
    public Vector2 MoveDirection;

    // 2. Kamera Bakış Açıları (Fare)
    // CS:GO'da merminin nereye gideceğini bilmek için farenin mutlak açılarına ihtiyacımız var.
    public float LookYaw;   // Sağa/Sola dönüş açısı (Y ekseni etrafında) (4 Byte)
    public float LookPitch; // Yukarı/Aşağı bakış açısı (X ekseni etrafında) (4 Byte)

    // 3. Aksiyon Tuşları (Booleans)
    // Yukarıdaki PlayerAction enum'ını kullanarak tüm bool'ları tek bir değişkende paketliyoruz. (4 Byte)
    public NetworkButtons Buttons;

    internal void Set(NetworkInput currentInput)
    {
        throw new NotImplementedException();
    }
}
using Fusion;
using UnityEngine;

public class PlayerMovement : NetworkBehaviour
{
    // PlayerPrefab İÇİNDEKİ "NETWORK TRANSFORM" KULLANILACAK, SERVER İLE HABERLEŞEBİLMESİ İÇİN.
    /*
     * Yürüyüşe başlama keskin
     * Yürüyüş sonunda ters yöne basılmamışsa hızlıca yavaşlayarak durma
     * Yürüyüş yapılırken ters yöne basılmışsa 
       -AYNI ANDA İKİ YÖNE DE BASILABİLİR, BİR YÖNE GİDERKEN GİTMEYİ BIRAKIP TERS YÖNE DE BASILABİLİR-
       anında durma (Counter Strafing)
     * Shift tuşuna koşma 
     *  space Zıplama
     * Yer ve Hava süratleri farklı (sv_accelerate ve sv_airaccelerate)
     * Air strafe
     * shift+ctrl kayma 
     * CTRL = EĞİLME 
     * akla geldikçe eklenebilir.
    */
}

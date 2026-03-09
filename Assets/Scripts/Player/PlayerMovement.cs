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
     * Shift tuşuna koşma ya da yavaş yürüme özelliği (kendin karar ver)
     * Zıplama
     * Yer ve Hava süratleri farklı (sv_accelerate ve sv_airaccelerate)
     * Air strafe
     * CTRL = EĞİLME (???)
    */
}

using System.Collections;
using TMPro;
using UnityEngine;

public class NotificationScript : MonoBehaviour
{
    // Her yerden kolayca erişebilmek için statik referans
    public static NotificationScript Instance;

    public float NotificationTime = 2.0f;

    [SerializeField]
    public TMP_Text NotificationText;

    private Coroutine _notificationCoroutine;

    private void Awake()
    {
        // DontDestroyOnLoad yapmıyoruz. 
        // Her sahne yüklendiğinde, bu sahnedeki Canvas kendi bildirim scriptini Instance olarak atar.
        Instance = this;
    }

    void Start()
    {
        // Başlangıçta paneli gizliyoruz
        gameObject.SetActive(false);
    }

    public void ShowNotification(string message)
    {
        gameObject.SetActive(true);
        // Varsa eski bildirimi durdur
        if (_notificationCoroutine != null)
        {
            StopCoroutine(_notificationCoroutine);
        }

        // Yeni bildirimi başlat
        _notificationCoroutine = StartCoroutine(NotificationRoutine(message));
    }

    private IEnumerator NotificationRoutine(string message)
    {
        // Ekranda göster
        NotificationText.text = message;

        // Belirlenen süre kadar bekle
        yield return new WaitForSeconds(NotificationTime);

        // Gizle
        NotificationText.text = "";
        gameObject.SetActive(false);
    }
}
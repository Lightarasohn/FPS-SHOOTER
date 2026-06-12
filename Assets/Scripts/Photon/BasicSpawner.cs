using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static GlobalVariables;

public class BasicSpawner : MonoBehaviour, INetworkRunnerCallbacks
{
    private NetworkRunner _runner;
    private static BasicSpawner _instance;

    [Header("Prefabs")]
    [SerializeField] private NetworkPrefabRef _playerStatePrefab;

    [Header("UI List References (Lobby)")]
    [SerializeField] private Transform _sessionListContent;
    [SerializeField] private GameObject _sessionEntryPrefab;

    // --- YENİ EKLENEN KISIM: HARİTA SEÇİM SİSTEMİ ---
    [Header("Map Selection (Host)")]
    [SerializeField] private GameObject _mapSelectionPanel; // Harita seçim ekranının genel paneli
    [SerializeField] private Transform _mapListContent;     // Harita butonlarının ekleneceği yer
    [SerializeField] private GameObject _mapEntryPrefab;    // İçinde MapEntryUI scripti olan prefab
    [SerializeField] private List<MapData> _availableMaps;  // Editörden dolduracağın harita havuzu

    [Header("Buttons")]
    [SerializeField] public Button HostButton;
    [SerializeField] public Button ClientButton;

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // YENİ: Harita seçim menüsünü açan fonksiyon. (Host butonuna tıklandığında bunu çağıracağız)
    public void OpenMapSelectionMenu()
    {
        if (_mapSelectionPanel == null) return;

        _mapSelectionPanel.SetActive(true);

        // İçerideki eski haritaları temizle
        foreach (Transform child in _mapListContent)
        {
            Destroy(child.gameObject);
        }

        // Editörden girdiğimiz harita verilerini UI prefablarına aktar
        foreach (var map in _availableMaps)
        {
            GameObject entry = Instantiate(_mapEntryPrefab, _mapListContent);
            MapEntryUI entryScript = entry.GetComponent<MapEntryUI>();

            if (entryScript != null)
            {
                // Butona tıklandığında paneli kapat ve o haritanın indexi ile oyunu başlat
                entryScript.Setup(map, () =>
                {
                    _mapSelectionPanel.SetActive(false);
                    StartGameAsHost(map.SceneBuildIndex);
                });
            }
        }
    }

    // YENİ: Parametrelere "int sceneIndex" eklendi. Varsayılanı 1 yaptık ki Client'lar girerken sorun yaşamasın.
    async Task StartGame(GameMode mode, string sessionName = "", int sceneIndex = 1)
    {
        if (_runner != null)
        {
            _runner.RemoveCallbacks(this);
            await _runner.Shutdown();
        }

        GameObject runnerObj = new GameObject("FusionRunner");
        DontDestroyOnLoad(runnerObj);

        _runner = runnerObj.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;
        _runner.AddCallbacks(this);

        // YENİ: Hardcoded 1 yerine, parametreden gelen sceneIndex değerini Fusion'a veriyoruz.
        var scene = SceneRef.FromIndex(sceneIndex);
        var sceneInfo = new NetworkSceneInfo();
        if (scene.IsValid)
            sceneInfo.AddSceneRef(scene, LoadSceneMode.Single);

        if (mode == GameMode.Host && string.IsNullOrEmpty(sessionName))
        {
            sessionName = "Room_" + Guid.NewGuid().ToString().Substring(0, 8);
        }

        // --- GÜNCELLENEN KISIM: ODA ÖZELLİKLERİNE HARİTA BİLGİSİNİ EKLEME ---
        Dictionary<string, SessionProperty> customProperties = new Dictionary<string, SessionProperty>();
        if (mode == GameMode.Host)
        {
            customProperties.Add("MapIndex", sceneIndex);
        }
        // -------------------------------------------------------------------

        try
        {
            await _runner.StartGame(new StartGameArgs
            {
                GameMode = mode,
                Scene = sceneInfo,
                SessionName = sessionName,
                SceneManager = runnerObj.AddComponent<NetworkSceneManagerDefault>(),
                // --- GÜNCELLENEN KISIM: Özellikleri Fusion'a paslıyoruz ---
                SessionProperties = mode == GameMode.Host ? customProperties : null,
                IsOpen = true,
                IsVisible = true
            });

            if (SceneManager.GetActiveScene().buildIndex == 0)
            {
                _ = SceneManager.UnloadSceneAsync(0);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[BasicSpawner] Başlatma Hatası: {e.Message}");
            if (mode == GameMode.Client)
                NotificationScript.Instance.ShowNotification("Oyuna katılırken bir sorun oluştu.");
            else
                NotificationScript.Instance.ShowNotification("Oyun oluştururken bir sorun oluştu.");
        }
    }

    async Task ConnectToLobby()
    {
        if (_runner != null)
        {
            _runner.RemoveCallbacks(this);
            await _runner.Shutdown();
        }

        GameObject runnerObj = new GameObject("FusionRunner");
        DontDestroyOnLoad(runnerObj);

        _runner = runnerObj.AddComponent<NetworkRunner>();
        _runner.AddCallbacks(this);

        NotificationScript.Instance.ShowNotification("Lobiye bağlanılıyor...");

        var result = await _runner.JoinSessionLobby(SessionLobby.ClientServer);

        if (result.Ok)
        {
            NotificationScript.Instance.ShowNotification("Lobiye bağlanıldı. Odalar listeleniyor.");
        }
        else
        {
            Debug.LogError($"[BasicSpawner] Lobiye bağlanılamadı: {result.ShutdownReason}");
            NotificationScript.Instance.ShowNotification("Lobi bağlantısı başarısız oldu.");
            SetButtonsInteractable(true);
        }
    }

    public async void StartGameAsHost(int selectedSceneIndex)
    {
        SetButtonsInteractable(false);
        NotificationScript.Instance.ShowNotification("Oyun başlatılıyor...");
        await StartGame(GameMode.Host, "", selectedSceneIndex);
        SetButtonsInteractable(true);
    }

    public async void JoinGameAsClient()
    {
        SetButtonsInteractable(false);
        await ConnectToLobby();
        SetButtonsInteractable(true);
    }

    public async void JoinSelectedSession(string sessionName)
    {
        NotificationScript.Instance.ShowNotification($"{sessionName} odasına katılıyor...");
        await StartGame(GameMode.Client, sessionName);
    }

    private void SetButtonsInteractable(bool state)
    {
        if (HostButton != null) HostButton.interactable = state;
        if (ClientButton != null) ClientButton.interactable = state;
    }

    private void OnGUI()
    {
        if (_runner == null && SceneManager.GetActiveScene().buildIndex == 0)
        {
            if (GUI.Button(new Rect(0, 0, 200, 40), "Host (Harita Seç)"))
                OpenMapSelectionMenu();

            if (GUI.Button(new Rect(0, 40, 200, 40), "Join Lobby"))
                JoinGameAsClient();
        }
    }

    // --- HATANIN DÜZELTİLDİĞİ KISIM BURASI ---
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        Debug.Log($"[BasicSpawner] OnSessionListUpdated tetiklendi. Oda Sayısı: {sessionList.Count}");

        if (_sessionListContent == null || _sessionEntryPrefab == null) return;

        foreach (Transform child in _sessionListContent)
        {
            Destroy(child.gameObject);
        }

        foreach (var session in sessionList)
        {
            if (session.IsOpen && session.IsVisible)
            {
                GameObject entry = Instantiate(_sessionEntryPrefab, _sessionListContent);
                SessionEntryUI entryScript = entry.GetComponent<SessionEntryUI>();

                if (entryScript != null)
                {
                    Sprite mapSprite = null;

                    // Odadan ağ üzerinden "MapIndex" verisini güvenli bir şekilde çekiyoruz
                    if (session.Properties != null && session.Properties.TryGetValue("MapIndex", out var propValue))
                    {
                        // DÜZELTME: (int) ile doğrudan cast ediyoruz
                        int hostedMapIndex = (int)propValue;

                        // Havuzumuzdan (Available Maps listesinden) bu indexe ait resmi eşleştiriyoruz
                        MapData foundMap = _availableMaps.Find(m => m.SceneBuildIndex == hostedMapIndex);
                        if (foundMap.MapPreviewImage != null)
                        {
                            mapSprite = foundMap.MapPreviewImage;
                        }
                    }

                    // Setup fonksiyonuna bulduğumuz resmi parametre olarak ekledik
                    entryScript.Setup(session, mapSprite, () => JoinSelectedSession(session.Name));
                }
            }
        }
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (runner.IsServer)
        {
            runner.Spawn(_playerStatePrefab, Vector3.zero, Quaternion.identity, player);
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (runner.IsServer)
        {
            NetworkObject playerObj = runner.GetPlayerObject(player);

            if (playerObj != null)
            {
                NetworkObject[] allNetworkObjects = FindObjectsByType<NetworkObject>(FindObjectsSortMode.None);
                foreach (var no in allNetworkObjects)
                {
                    if (no.InputAuthority == player)
                    {
                        runner.Despawn(no);
                    }
                }

                runner.Despawn(playerObj);
                runner.SetPlayerObject(player, null);
            }
        }
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (runner != null && runner.gameObject != null)
        {
            Destroy(runner.gameObject);
        }

        _instance = null;
        Destroy(gameObject);

        if (SceneManager.GetActiveScene().buildIndex != 0)
        {
            SceneManager.LoadScene(0);
        }
    }

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }

    public void OnInput(NetworkRunner runner, Fusion.NetworkInput input)
    {
        var localPlayerObject = runner.GetPlayerObject(runner.LocalPlayer);
        if (localPlayerObject != null)
        {
            var inputHandler = localPlayerObject.GetComponent<PlayerInputHandler>();
            if (inputHandler != null)
            {
                input.Set(inputHandler.CurrentInput);
            }
        }
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, Fusion.NetworkInput input) { }
}
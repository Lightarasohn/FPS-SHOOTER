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

    // Oyun Başlatma (Host veya Doğrudan Belirli Bir Odaya Client Olarak Giriş)
    async Task StartGame(GameMode mode, string sessionName = "")
    {
        // Eski runner varsa temizle
        if (_runner != null)
        {
            // YENİ: Geçiş sırasında Spawner'ın OnShutdown algılamasını iptal et
            _runner.RemoveCallbacks(this);
            await _runner.Shutdown();
        }

        // YENİ: NetworkRunner'ı BasicSpawner'dan ayırıp YENİ bir objeye koyuyoruz!
        // Böylece Fusion shutdown olduğunda BasicSpawner silinmez.
        GameObject runnerObj = new GameObject("FusionRunner");
        DontDestroyOnLoad(runnerObj);

        _runner = runnerObj.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;
        _runner.AddCallbacks(this);

        var scene = SceneRef.FromIndex(1);
        var sceneInfo = new NetworkSceneInfo();
        if (scene.IsValid)
            sceneInfo.AddSceneRef(scene, LoadSceneMode.Single);

        if (mode == GameMode.Host && string.IsNullOrEmpty(sessionName))
        {
            sessionName = "Room_" + Guid.NewGuid().ToString().Substring(0, 8);
        }

        try
        {
            await _runner.StartGame(new StartGameArgs
            {
                GameMode = mode,
                Scene = sceneInfo,
                SessionName = sessionName,
                SceneManager = runnerObj.AddComponent<NetworkSceneManagerDefault>()
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

    // Client'ların Photon Lobi Sunucusuna Bağlanmasını Sağlayan Metot
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

    public async void StartGameAsHost()
    {
        SetButtonsInteractable(false);
        NotificationScript.Instance.ShowNotification("Oyun başlatılıyor...");
        await StartGame(GameMode.Host);
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
            if (GUI.Button(new Rect(0, 0, 200, 40), "Host (GUID)"))
                StartGameAsHost();

            if (GUI.Button(new Rect(0, 40, 200, 40), "Join Lobby"))
                JoinGameAsClient();
        }
    }

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
                    entryScript.Setup(session, () => JoinSelectedSession(session.Name));
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

        // Geçiş mantığını (isTransitioning) tamamen kaldırdık. 
        // Lobi > Oyun geçişinde zaten yukarıda "RemoveCallbacks" kullandığımız için bu metot asla çalışmaz.
        // EĞER bu metot çalışıyorsa, oyuncu Pause menüsünden veya hata yüzünden düşmüştür.
        // O yüzden arayüz referanslarını tazelemek için kendini imha edip Ana Menüye dönmesi DOĞRUDUR.

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
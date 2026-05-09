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
    // Kapsül değil, görünmez temsilci prefabını (PlayerState) buraya koyacağız
    [SerializeField] private NetworkPrefabRef _playerStatePrefab;


    [Header("Buttons")]
    [SerializeField] public Button HostButton;
    [SerializeField] public Button ClientButton;

    void Awake()
    {
        // Eğer sahnede halihazırda bir BasicSpawner varsa, sonradan geleni yok et.
        if (_instance != null && _instance != this)

        {
            Destroy(_instance.gameObject);
            return;
        }

        _instance = this;

        DontDestroyOnLoad(gameObject);
    }

    async Task StartGame(GameMode mode)
    {
        _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;
        _runner.AddCallbacks(this);

        //FIX: EĞER SCENE INDEX DEĞİŞİRSE BURAYI DA DEĞİŞTİR. UNUTMA!
        var scene = SceneRef.FromIndex(1);
        var sceneInfo = new NetworkSceneInfo();
        if (scene.IsValid)
            sceneInfo.AddSceneRef(scene, LoadSceneMode.Single);

        try
        {
            await _runner.StartGame(new StartGameArgs
            {
                GameMode = mode,
                Scene = sceneInfo,
                SessionName = "TestSession",
                SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
            });

            if (SceneManager.GetActiveScene().buildIndex == 0)
            {
                _ = SceneManager.UnloadSceneAsync(0);
            }
        }
        catch
        {
            if (mode == GameMode.Client)
                NotificationScript.Instance.ShowNotification("Oyuna katılırken bir sorun oluştu.");
            else
                NotificationScript.Instance.ShowNotification("Oyun oluştururken bir sorun oluştu.");
        }
    }

    private async void OnGUI()
    {
        if (_runner == null && SceneManager.GetActiveScene().buildIndex != 0)
        {
            if (GUI.Button(new Rect(0, 0, 200, 40), "Host"))
                await StartGame(GameMode.Host);

            if (GUI.Button(new Rect(0, 40, 200, 40), "Join"))
                await StartGame(GameMode.Client);
        }
    }
    
    public async void StartGameAsHost()
    {
        HostButton.interactable = false;
        ClientButton.interactable = false;

        NotificationScript.Instance.ShowNotification("Oyun başlatılıyor");
        await StartGame(GameMode.Host);

        HostButton.interactable = true;
        ClientButton.interactable = true;
    }

    public async void JoinGameAsClient()
    {
        HostButton.interactable = false;
        ClientButton.interactable = false;

        NotificationScript.Instance.ShowNotification("Oyuna katılınıyor");
        await StartGame(GameMode.Client);

        HostButton.interactable = true;
        ClientButton.interactable = true;
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"[BasicSpawner] OnPlayerJoined called. Runner.IsServer={runner.IsServer}, player={player.RawEncoded}");

        if (runner.IsServer)
        {
            // Sadece oyuncunun ağdaki işlemlerini yönetecek olan PlayerState prefabını doğuruyoruz.
            runner.Spawn(_playerStatePrefab, Vector3.zero, Quaternion.identity, player);
        }
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
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

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"[BasicSpawner] Oyuncu {player.RawEncoded} oyundan ayrıldı. Temizlik yapılıyor...");

        // Bu temizlik işlemini SADECE sunucu (Host/Server) yapabilir
        if (runner.IsServer)
        {
            // Ayrılan oyuncunun ağdaki ana temsilcisini (PlayerState) bul
            NetworkObject playerObj = runner.GetPlayerObject(player);

            if (playerObj != null)
            {
                // DİKKAT: Senin mimarinde asıl "Fiziksel Karakter"i PlayerState doğuruyordu.
                // Oyuncu çıkmadan hemen önce GameManager'dan çıkarılması için Player nesnesini bulmalıyız.

                // NOT: Eğer karakter ile PlayerState farklı objelerse (ki senin mimarinde öyle), 
                // sahnedeki tüm fiziksel karakterleri tarayıp InputAuthority'si bu çıkan oyuncuya ait olanı bulup silmeliyiz.

                NetworkObject[] allNetworkObjects = FindObjectsByType<NetworkObject>(FindObjectsSortMode.None);
                foreach (var no in allNetworkObjects)
                {
                    // Eğer sahnedeki obje çıkan oyuncuya aitse ve karakter/state ise
                    if (no.InputAuthority == player)
                    {
                        runner.Despawn(no);
                    }
                }

                // En son, ana temsilciyi (PlayerState) de yok et
                runner.Despawn(playerObj);

                // Hafızadan da sil
                runner.SetPlayerObject(player, null);
            }
        }
    }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) 
    {
        if (runner != null)
        {
            // Fusion'ın tüm ağ işlemlerini ve objeleri güvenle temizlemesini bekle
            runner.Shutdown();
        }
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        _instance = null;
        Destroy(gameObject);

        if(SceneManager.GetActiveScene().buildIndex != 0)
        { 
            SceneManager.LoadScene(0); 
        }
    }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, Fusion.NetworkInput input) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }

    public void OnInput(NetworkRunner runner, Fusion.NetworkInput input)
    {
        // Kendi karakterimizi (InputAuthority bizde olan objeyi) buluyoruz
        var localPlayerObject = runner.GetPlayerObject(runner.LocalPlayer);

        if (localPlayerObject != null)
        {
            // Karakterin üzerindeki kendi yazdığımız PlayerInputHandler scriptini alıyoruz
            var inputHandler = localPlayerObject.GetComponent<PlayerInputHandler>();
            if (inputHandler != null)
            {
                // Verileri Fusion ağına iletiyoruz
                input.Set(inputHandler.CurrentInput);
            }
        }
    }
}
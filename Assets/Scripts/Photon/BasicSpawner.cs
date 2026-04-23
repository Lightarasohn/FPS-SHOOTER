using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using static GlobalVariables;


public class BasicSpawner : MonoBehaviour, INetworkRunnerCallbacks
{
    private NetworkRunner _runner;

    [Header("Prefabs")]
    // Kapsül değil, görünmez temsilci prefabını (PlayerState) buraya koyacağız
    [SerializeField] private NetworkPrefabRef _playerStatePrefab;

    async void StartGame(GameMode mode)
    {
        _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;
        _runner.AddCallbacks(this);

        var scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);
        var sceneInfo = new NetworkSceneInfo();
        if (scene.IsValid)
            sceneInfo.AddSceneRef(scene, LoadSceneMode.Additive);

        await _runner.StartGame(new StartGameArgs
        {
            GameMode = mode,
            Scene = scene,
            SessionName = "TestSession",
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });
    }

    private void OnGUI()
    {
        if (_runner == null)
        {
            if (GUI.Button(new Rect(0, 0, 200, 40), "Host"))
                StartGame(GameMode.Host);

            if (GUI.Button(new Rect(0, 40, 200, 40), "Join"))
                StartGame(GameMode.Client);
        }
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

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
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

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, Fusion.NetworkInput input)
    {
        throw new NotImplementedException();
    }
}
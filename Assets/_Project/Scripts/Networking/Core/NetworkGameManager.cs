using System.Collections;
using FishNet.Managing;
using FishNet.Managing.Scened;
using FishNet.Transporting;
using UnityEngine;
using UnityScene = UnityEngine.SceneManagement.Scene;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

public sealed class NetworkGameManager : MonoBehaviour
{
    public static NetworkGameManager Instance { get; private set; }

    [SerializeField] private NetworkManager networkManager;

    [Header("Network Scenes")]
    [SerializeField] private string lobbyScene = "Lobby";
    [SerializeField] private string gameplayScene = "HoleInTheWall";

    private bool _initialLobbyLoaded;
    private bool _clientWasStarted;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        if (networkManager == null) return;

        networkManager.ServerManager.OnServerConnectionState += HandleServerConnectionState;
        networkManager.ClientManager.OnClientConnectionState += HandleClientConnectionState;
    }

    private void OnDestroy()
    {
        if (networkManager != null)
        {
            networkManager.ServerManager.OnServerConnectionState -= HandleServerConnectionState;
            networkManager.ClientManager.OnClientConnectionState -= HandleClientConnectionState;
        }

        if (Instance == this)
            Instance = null;
    }

    public void StartGame()
    {
        if (!IsServerRunning()) return;
        LoadOnlineScene(gameplayScene);
    }

    public void ReturnToLobby()
    {
        if (!IsServerRunning()) return;
        LoadOnlineScene(lobbyScene);
    }

    private void HandleServerConnectionState(ServerConnectionStateArgs args)
    {
        if (args.ConnectionState == LocalConnectionState.Started)
        {
            if (_initialLobbyLoaded) return;

            _initialLobbyLoaded = true;
            LoadOnlineScene(lobbyScene);
        }
        else if (args.ConnectionState == LocalConnectionState.Stopped)
        {
            _initialLobbyLoaded = false;
        }
    }

    private void HandleClientConnectionState(ClientConnectionStateArgs args)
    {
        if (args.ConnectionState == LocalConnectionState.Started)
        {
            _clientWasStarted = true;
            return;
        }

        if (args.ConnectionState != LocalConnectionState.Stopped || !_clientWasStarted)
            return;

        _clientWasStarted = false;
        StartCoroutine(CleanupOnlineScenesRoutine());
    }

    private void LoadOnlineScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName)) return;

        SceneLoadData loadData = new SceneLoadData(sceneName);
        loadData.ReplaceScenes = ReplaceOption.OnlineOnly;
        networkManager.SceneManager.LoadGlobalScenes(loadData);
    }

    private IEnumerator CleanupOnlineScenesRoutine()
    {
        yield return null;
        UnloadLocalScene(lobbyScene);
        UnloadLocalScene(gameplayScene);
    }

    private static void UnloadLocalScene(string sceneName)
    {
        UnityScene scene = UnitySceneManager.GetSceneByName(sceneName);

        if (!scene.IsValid() || !scene.isLoaded)
            return;

        UnitySceneManager.UnloadSceneAsync(scene);
    }

    private bool IsServerRunning()
    {
        return networkManager != null && networkManager.ServerManager.Started;
    }
}

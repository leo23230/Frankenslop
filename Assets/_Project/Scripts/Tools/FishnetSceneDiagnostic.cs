using System;
using System.Text;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Managing.Scened;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

public sealed class FishNetSceneDiagnostic : MonoBehaviour
{
    [SerializeField] private NetworkManager networkManager;
    private FishNet.Managing.Scened.SceneManager _sceneManager;

    private void Awake()
    {
        if (networkManager == null) networkManager = FindFirstObjectByType<NetworkManager>();
    }

    private void OnEnable()
    {
        if (networkManager == null) return;
        _sceneManager = networkManager.SceneManager;
        if (_sceneManager == null) return;

        _sceneManager.OnLoadStart += HandleLoadStart;
        _sceneManager.OnLoadEnd += HandleLoadEnd;
        _sceneManager.OnClientLoadedStartScenes += HandleClientLoadedStartScenes;
        UnitySceneManager.sceneLoaded += HandleUnitySceneLoaded;
        UnitySceneManager.sceneUnloaded += HandleUnitySceneUnloaded;
    }

    private void OnDisable()
    {
        if (_sceneManager != null)
        {
            _sceneManager.OnLoadStart -= HandleLoadStart;
            _sceneManager.OnLoadEnd -= HandleLoadEnd;
            _sceneManager.OnClientLoadedStartScenes -= HandleClientLoadedStartScenes;
        }

        UnitySceneManager.sceneLoaded -= HandleUnitySceneLoaded;
        UnitySceneManager.sceneUnloaded -= HandleUnitySceneUnloaded;
    }

    private void HandleLoadStart(SceneLoadStartEventArgs args)
    {
        string side = args.QueueData.AsServer ? "SERVER" : "CLIENT";
        Debug.Log($"[FishNet Scene] LOAD START | {side} | Scope: {args.QueueData.ScopeType}", this);
        DumpUnityScenes();
    }

    private void HandleLoadEnd(SceneLoadEndEventArgs args)
    {
        string side = args.QueueData.AsServer ? "SERVER" : "CLIENT";
        Debug.Log($"[FishNet Scene] LOAD END | {side} | Loaded: [{FormatScenes(args.LoadedScenes)}] | Skipped: [{FormatStrings(args.SkippedSceneNames)}] | Unloaded: [{FormatStrings(args.UnloadedSceneNames)}]", this);
        DumpUnityScenes();

        if (args.QueueData.AsServer) DumpServerSceneConnections();
    }

    private void HandleClientLoadedStartScenes(NetworkConnection connection, bool asServer)
    {
        string side = asServer ? "SERVER" : "CLIENT";
        Debug.Log($"[FishNet Scene] START SCENES COMPLETE | {side} | Client {connection.ClientId} | Scenes: [{FormatConnectionScenes(connection)}]", this);
    }

    private void HandleUnitySceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[Unity Scene] LOADED | {scene.name} | Handle: {scene.handle} | Mode: {mode}", this);
    }

    private void HandleUnitySceneUnloaded(Scene scene)
    {
        Debug.Log($"[Unity Scene] UNLOADED | {scene.name} | Handle: {scene.handle}", this);
    }

    private void DumpUnityScenes()
    {
        StringBuilder builder = new();

        for (int i = 0; i < UnitySceneManager.sceneCount; i++)
        {
            if (i > 0) builder.Append(", ");
            Scene scene = UnitySceneManager.GetSceneAt(i);
            builder.Append($"{scene.name}({scene.handle})");
        }

        Debug.Log($"[Unity Scene] Currently loaded: [{builder}] | Active: {UnitySceneManager.GetActiveScene().name}", this);
    }

    private void DumpServerSceneConnections()
    {
        if (_sceneManager == null || networkManager == null || !networkManager.ServerManager.Started) return;

        foreach (var pair in _sceneManager.SceneConnections)
        {
            StringBuilder clients = new();

            foreach (NetworkConnection connection in pair.Value)
            {
                if (clients.Length > 0) clients.Append(", ");
                clients.Append(connection.ClientId);
            }

            Debug.Log($"[FishNet Scene] SERVER MEMBERSHIP | {pair.Key.name}({pair.Key.handle}) | Clients: [{clients}]", this);
        }
    }

    private static string FormatScenes(Scene[] scenes)
    {
        if (scenes == null || scenes.Length == 0) return "none";
        StringBuilder builder = new();

        for (int i = 0; i < scenes.Length; i++)
        {
            if (i > 0) builder.Append(", ");
            builder.Append($"{scenes[i].name}({scenes[i].handle})");
        }

        return builder.ToString();
    }

    private static string FormatStrings(string[] values)
    {
        if (values == null || values.Length == 0) return "none";
        return string.Join(", ", values);
    }

    private static string FormatConnectionScenes(NetworkConnection connection)
    {
        if (connection == null) return "none";
        StringBuilder builder = new();

        foreach (Scene scene in connection.Scenes)
        {
            if (builder.Length > 0) builder.Append(", ");
            builder.Append($"{scene.name}({scene.handle})");
        }

        return builder.Length > 0 ? builder.ToString() : "none";
    }
}

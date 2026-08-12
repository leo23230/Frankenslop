using System;
using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Transporting;
using UnityEngine;

public sealed class LobbyPlayerRoster : NetworkBehaviour
{
    public static LobbyPlayerRoster Instance { get; private set; }

    private readonly SyncDictionary<int, string> _players = new();

    public event Action<IReadOnlyDictionary<int, string>> PlayersChanged;

    public IReadOnlyDictionary<int, string> Players => _players;

    private void Awake()
    {
        _players.OnChange += HandlePlayersChanged;
    }

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        Instance = this;
    }

    public override void OnStopNetwork()
    {
        if (Instance == this)
            Instance = null;

        base.OnStopNetwork();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        ServerManager.OnRemoteConnectionState += HandleRemoteConnectionState;

        foreach (KeyValuePair<int, NetworkConnection> pair in ServerManager.Clients)
        {
            NetworkConnection connection = pair.Value;

            if (connection == null)
                continue;

            AddPlayer(connection);
        }
    }

    public override void OnStopServer()
    {
        ServerManager.OnRemoteConnectionState -= HandleRemoteConnectionState;
        _players.Clear();
        base.OnStopServer();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        NotifyPlayersChanged();
    }

    private void HandleRemoteConnectionState(NetworkConnection connection, RemoteConnectionStateArgs args)
    {
        if (args.ConnectionState == RemoteConnectionState.Started)
            AddPlayer(connection);
        else if (args.ConnectionState == RemoteConnectionState.Stopped)
            RemovePlayer(connection);
    }

    [Server]
    private void AddPlayer(NetworkConnection connection)
    {
        if (connection == null)
            return;

        int clientId = connection.ClientId;

        if (_players.ContainsKey(clientId))
            return;

        _players.Add(clientId, $"Player {clientId + 1}");
    }

    [Server]
    private void RemovePlayer(NetworkConnection connection)
    {
        if (connection == null)
            return;

        _players.Remove(connection.ClientId);
    }

    private void HandlePlayersChanged(SyncDictionaryOperation operation, int key, string value, bool asServer)
    {
        NotifyPlayersChanged();
    }

    private void NotifyPlayersChanged()
    {
        PlayersChanged?.Invoke(_players);
    }
}
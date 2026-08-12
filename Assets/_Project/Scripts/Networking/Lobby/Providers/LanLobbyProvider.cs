using System;
using System.Collections.Generic;
using System.Net;
using FishNet.Discovery;
using UnityEngine;


public sealed class LanLobbyProvider : MonoBehaviour, ILobbyProvider
{
    [SerializeField] private NetworkDiscovery networkDiscovery;

    private readonly Dictionary<string, LobbyInfo> _foundLobbies = new();

    public LobbyProviderKind Kind => LobbyProviderKind.Lan;
    public bool IsAvailable => networkDiscovery != null && NetworkSessionManager.Instance != null;

    public event Action<LobbyProviderKind, IReadOnlyList<LobbyInfo>> LobbiesChanged;
    public event Action<string> StatusChanged;

    private void OnEnable()
    {
        if (networkDiscovery != null)
            networkDiscovery.ServerFoundCallback += HandleServerFound;
    }

    private void OnDisable()
    {
        if (networkDiscovery != null)
            networkDiscovery.ServerFoundCallback -= HandleServerFound;

        StopDiscovery();
    }

    public void Host(string lobbyName, int maxPlayers)
    {
        if (!IsAvailable)
        {
            StatusChanged?.Invoke("LAN networking is not available.");
            return;
        }

        StopDiscovery();

        bool started = NetworkSessionManager.Instance.StartHost(NetworkTransportKind.Tugboat, maxPlayers);

        if (!started)
        {
            StatusChanged?.Invoke("Failed to start LAN host.");
            return;
        }

        networkDiscovery.AdvertiseServer();

        string displayName = string.IsNullOrWhiteSpace(lobbyName) ? "LAN Game" : lobbyName.Trim();
        StatusChanged?.Invoke($"Hosting {displayName} on LAN.");
    }

    public void Refresh()
    {
        if (!IsAvailable)
        {
            StatusChanged?.Invoke("LAN discovery is not available.");
            return;
        }

        StopDiscovery();
        _foundLobbies.Clear();
        Publish();
        networkDiscovery.SearchForServers();
        StatusChanged?.Invoke("Searching for LAN games...");
    }

    public void Join(LobbyInfo lobby)
    {
        if (lobby == null || lobby.Provider != Kind) return;

        StopDiscovery();

        bool started = NetworkSessionManager.Instance.StartClient(NetworkTransportKind.Tugboat, lobby.ConnectionAddress);
        StatusChanged?.Invoke(started ? $"Connecting to {lobby.ConnectionAddress}..." : "Failed to start LAN client.");
    }

    public void Leave()
    {
        StopDiscovery();
        _foundLobbies.Clear();
        Publish();
    }

    private void HandleServerFound(IPEndPoint endpoint)
    {
        if (endpoint == null) return;

        string address = endpoint.Address.ToString();
        LobbyInfo lobby = new LobbyInfo(LobbyProviderKind.Lan, NetworkTransportKind.Tugboat, address, $"LAN Game ({address})", address, -1, -1);
        _foundLobbies[address] = lobby;
        Publish();
    }

    private void StopDiscovery()
    {
        if (networkDiscovery == null) return;

        if (networkDiscovery.IsAdvertising || networkDiscovery.IsSearching)
            networkDiscovery.StopSearchingOrAdvertising();
    }

    private void Publish()
    {
        LobbiesChanged?.Invoke(Kind, new List<LobbyInfo>(_foundLobbies.Values));
    }
}

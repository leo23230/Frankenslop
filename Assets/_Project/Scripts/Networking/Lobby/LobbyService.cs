using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class LobbyService : MonoBehaviour
{
    public static LobbyService Instance { get; private set; }

    [SerializeField] private MonoBehaviour[] providerBehaviours;

    private readonly Dictionary<LobbyProviderKind, ILobbyProvider> _providers = new();
    private readonly Dictionary<LobbyProviderKind, List<LobbyInfo>> _providerLobbies = new();
    private readonly List<LobbyInfo> _combinedLobbies = new();

    public event Action<IReadOnlyList<LobbyInfo>> LobbiesChanged;
    public event Action<string> StatusChanged;

    public LobbyProviderKind? ActiveProviderKind { get; private set; }

    public bool CanInvite
    {
        get
        {
            if (!TryGetActiveProvider(out ILobbyProvider provider)) return false;
            return provider is ILobbyInviteProvider inviteProvider && inviteProvider.CanInvite;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        RegisterProviders();
    }

    private void OnDestroy()
    {
        foreach (ILobbyProvider provider in _providers.Values)
        {
            provider.LobbiesChanged -= HandleProviderLobbiesChanged;
            provider.StatusChanged -= HandleProviderStatusChanged;
        }

        if (Instance == this)
            Instance = null;
    }

    public void Host(LobbyProviderKind providerKind, string lobbyName, int maxPlayers)
    {
        if (!_providers.TryGetValue(providerKind, out ILobbyProvider provider))
        {
            StatusChanged?.Invoke($"Lobby provider {providerKind} is not configured.");
            return;
        }

        if (!provider.IsAvailable)
        {
            StatusChanged?.Invoke($"{providerKind} is not currently available.");
            return;
        }

        LeaveOtherProviders(providerKind);
        ActiveProviderKind = providerKind;
        provider.Host(lobbyName, maxPlayers);
    }

    public void RefreshAll()
    {
        foreach (ILobbyProvider provider in _providers.Values)
        {
            if (provider.IsAvailable)
                provider.Refresh();
        }
    }

    public void Join(LobbyInfo lobby)
    {
        if (lobby == null) return;

        if (!_providers.TryGetValue(lobby.Provider, out ILobbyProvider provider))
        {
            StatusChanged?.Invoke($"Lobby provider {lobby.Provider} is not configured.");
            return;
        }

        if (!provider.IsAvailable)
        {
            StatusChanged?.Invoke($"{lobby.Provider} is not currently available.");
            return;
        }

        LeaveOtherProviders(lobby.Provider);
        ActiveProviderKind = lobby.Provider;
        provider.Join(lobby);
    }

    public void Disconnect()
    {
        foreach (ILobbyProvider provider in _providers.Values)
            provider.Leave();

        ActiveProviderKind = null;

        if (NetworkSessionManager.Instance != null)
            NetworkSessionManager.Instance.Disconnect();

        ClearAllLobbies();
    }

    public void OpenInviteOverlay()
    {
        if (!TryGetActiveProvider(out ILobbyProvider provider)) return;

        if (provider is ILobbyInviteProvider inviteProvider && inviteProvider.CanInvite)
            inviteProvider.OpenInviteOverlay();
    }

    private void RegisterProviders()
    {
        _providers.Clear();
        _providerLobbies.Clear();

        if (providerBehaviours == null) return;

        foreach (MonoBehaviour behaviour in providerBehaviours)
        {
            if (behaviour is not ILobbyProvider provider) continue;

            _providers[provider.Kind] = provider;
            _providerLobbies[provider.Kind] = new List<LobbyInfo>();
            provider.LobbiesChanged += HandleProviderLobbiesChanged;
            provider.StatusChanged += HandleProviderStatusChanged;
        }
    }

    private void LeaveOtherProviders(LobbyProviderKind providerToKeep)
    {
        foreach (KeyValuePair<LobbyProviderKind, ILobbyProvider> pair in _providers)
        {
            if (pair.Key == providerToKeep) continue;
            pair.Value.Leave();
        }
    }

    private void HandleProviderLobbiesChanged(LobbyProviderKind providerKind, IReadOnlyList<LobbyInfo> lobbies)
    {
        if (!_providerLobbies.TryGetValue(providerKind, out List<LobbyInfo> destination))
        {
            destination = new List<LobbyInfo>();
            _providerLobbies[providerKind] = destination;
        }

        destination.Clear();

        if (lobbies != null)
        {
            for (int i = 0; i < lobbies.Count; i++)
                destination.Add(lobbies[i]);
        }

        RebuildCombinedLobbyList();
    }

    private void HandleProviderStatusChanged(string message)
    {
        StatusChanged?.Invoke(message);
    }

    private void RebuildCombinedLobbyList()
    {
        _combinedLobbies.Clear();

        foreach (List<LobbyInfo> lobbies in _providerLobbies.Values)
            _combinedLobbies.AddRange(lobbies);

        _combinedLobbies.Sort(CompareLobbies);
        LobbiesChanged?.Invoke(_combinedLobbies);
    }

    private static int CompareLobbies(LobbyInfo a, LobbyInfo b)
    {
        int providerComparison = a.Provider.CompareTo(b.Provider);

        if (providerComparison != 0)
            return providerComparison;

        return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
    }

    private bool TryGetActiveProvider(out ILobbyProvider provider)
    {
        provider = null;

        if (!ActiveProviderKind.HasValue)
            return false;

        return _providers.TryGetValue(ActiveProviderKind.Value, out provider);
    }

    private void ClearAllLobbies()
    {
        foreach (List<LobbyInfo> lobbies in _providerLobbies.Values)
            lobbies.Clear();

        RebuildCombinedLobbyList();
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

#if STEAMWORKS_NET
using Steamworks;
#endif

public sealed class SteamLobbyProvider : MonoBehaviour, ILobbyProvider, ILobbyInviteProvider
{
    private const string ProjectField = "project";
    private const string BuildField = "build";
    private const string NameField = "name";
    private const string HostSteamIdField = "host_steam_id";

    [Header("Lobby Filtering")]
    [SerializeField] private string projectKey = "frankenslop-hole-in-the-wall";
    [SerializeField, Range(1, 50)] private int maxSearchResults = 50;

    private readonly List<LobbyInfo> _lobbies = new();
    private string _pendingLobbyName;
    private int _pendingMaxPlayers = 4;
    private ulong _currentLobbyId;

    public LobbyProviderKind Kind => LobbyProviderKind.Steam;

    public bool IsAvailable
    {
        get
        {
#if STEAMWORKS_NET
            return SteamManager.Initialized && NetworkSessionManager.Instance != null;
#else
            return false;
#endif
        }
    }

    public bool CanInvite => IsAvailable && _currentLobbyId != 0;

    public event Action<LobbyProviderKind, IReadOnlyList<LobbyInfo>> LobbiesChanged;
    public event Action<string> StatusChanged;

#if STEAMWORKS_NET
    private CallResult<LobbyCreated_t> _lobbyCreatedResult;
    private CallResult<LobbyMatchList_t> _lobbyListResult;
    private CallResult<LobbyEnter_t> _lobbyEnteredResult;
    private Callback<GameLobbyJoinRequested_t> _lobbyJoinRequestedCallback;
    private bool _callbacksReady;
    private bool _checkedCommandLine;
#endif

    private void Start()
    {
#if STEAMWORKS_NET
        EnsureSteamReady();
#endif
    }

    public void Host(string lobbyName, int maxPlayers)
    {
#if STEAMWORKS_NET
        if (!EnsureSteamReady()) return;

        _pendingLobbyName = string.IsNullOrWhiteSpace(lobbyName) ? $"{SteamFriends.GetPersonaName()}'s Lobby" : lobbyName.Trim();
        _pendingMaxPlayers = Mathf.Clamp(maxPlayers, 1, 250);

        SteamAPICall_t call = SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, _pendingMaxPlayers);
        _lobbyCreatedResult.Set(call);
        StatusChanged?.Invoke("Creating Steam lobby...");
#else
        ReportSteamUnavailable();
#endif
    }

    public void Refresh()
    {
#if STEAMWORKS_NET
        if (!EnsureSteamReady()) return;

        _lobbies.Clear();
        Publish();

        SteamMatchmaking.AddRequestLobbyListStringFilter(ProjectField, projectKey, ELobbyComparison.k_ELobbyComparisonEqual);
        SteamMatchmaking.AddRequestLobbyListStringFilter(BuildField, Application.version, ELobbyComparison.k_ELobbyComparisonEqual);
        SteamMatchmaking.AddRequestLobbyListDistanceFilter(ELobbyDistanceFilter.k_ELobbyDistanceFilterWorldwide);
        SteamMatchmaking.AddRequestLobbyListResultCountFilter(Mathf.Clamp(maxSearchResults, 1, 50));

        SteamAPICall_t call = SteamMatchmaking.RequestLobbyList();
        _lobbyListResult.Set(call);
        StatusChanged?.Invoke("Searching Steam lobbies...");
#else
        ReportSteamUnavailable();
#endif
    }

    public void Join(LobbyInfo lobby)
    {
#if STEAMWORKS_NET
        if (!EnsureSteamReady()) return;
        if (lobby == null || lobby.Provider != Kind) return;

        if (!ulong.TryParse(lobby.Id, out ulong lobbyId))
        {
            StatusChanged?.Invoke("Steam lobby ID is invalid.");
            return;
        }

        JoinSteamLobby(new CSteamID(lobbyId));
#else
        ReportSteamUnavailable();
#endif
    }

    public void Leave()
    {
#if STEAMWORKS_NET
        if (SteamManager.Initialized && _currentLobbyId != 0)
            SteamMatchmaking.LeaveLobby(new CSteamID(_currentLobbyId));
#endif

        _currentLobbyId = 0;
        _lobbies.Clear();
        Publish();
    }

    public void OpenInviteOverlay()
    {
#if STEAMWORKS_NET
        if (!EnsureSteamReady() || _currentLobbyId == 0) return;

        SteamFriends.ActivateGameOverlayInviteDialog(new CSteamID(_currentLobbyId));
#endif
    }

#if STEAMWORKS_NET
    private bool EnsureSteamReady()
    {
        if (!SteamManager.Initialized)
        {
            StatusChanged?.Invoke("Steam is not initialized.");
            return false;
        }

        if (NetworkSessionManager.Instance == null)
        {
            StatusChanged?.Invoke("NetworkSessionManager is missing.");
            return false;
        }

        if (!_callbacksReady)
        {
            _lobbyCreatedResult = CallResult<LobbyCreated_t>.Create(HandleLobbyCreated);
            _lobbyListResult = CallResult<LobbyMatchList_t>.Create(HandleLobbyListReceived);
            _lobbyEnteredResult = CallResult<LobbyEnter_t>.Create(HandleLobbyEntered);
            _lobbyJoinRequestedCallback = Callback<GameLobbyJoinRequested_t>.Create(HandleLobbyJoinRequested);
            _callbacksReady = true;
        }

        if (!_checkedCommandLine)
        {
            _checkedCommandLine = true;
            TryJoinLobbyFromCommandLine();
        }

        return true;
    }

    private void HandleLobbyCreated(LobbyCreated_t result, bool ioFailure)
    {
        if (ioFailure || result.m_eResult != EResult.k_EResultOK)
        {
            StatusChanged?.Invoke($"Steam lobby creation failed: {result.m_eResult}.");
            return;
        }

        CSteamID lobbyId = new CSteamID(result.m_ulSteamIDLobby);
        _currentLobbyId = lobbyId.m_SteamID;

        SteamMatchmaking.SetLobbyJoinable(lobbyId, false);
        SteamMatchmaking.SetLobbyData(lobbyId, ProjectField, projectKey);
        SteamMatchmaking.SetLobbyData(lobbyId, BuildField, Application.version);
        SteamMatchmaking.SetLobbyData(lobbyId, NameField, _pendingLobbyName);

        string hostSteamId = SteamUser.GetSteamID().m_SteamID.ToString();
        SteamMatchmaking.SetLobbyData(lobbyId, HostSteamIdField, hostSteamId);

        bool networkStarted = NetworkSessionManager.Instance.StartHost(NetworkTransportKind.Steam, _pendingMaxPlayers);

        if (!networkStarted)
        {
            SteamMatchmaking.LeaveLobby(lobbyId);
            _currentLobbyId = 0;
            StatusChanged?.Invoke("FishNet Steam host failed to start.");
            return;
        }

        SteamMatchmaking.SetLobbyJoinable(lobbyId, true);
        StatusChanged?.Invoke($"Hosting {_pendingLobbyName} on Steam.");
    }

    private void HandleLobbyListReceived(LobbyMatchList_t result, bool ioFailure)
    {
        _lobbies.Clear();

        if (ioFailure)
        {
            Publish();
            StatusChanged?.Invoke("Steam lobby search failed.");
            return;
        }

        int lobbyCount = (int)result.m_nLobbiesMatching;

        for (int i = 0; i < lobbyCount; i++)
        {
            CSteamID lobbyId = SteamMatchmaking.GetLobbyByIndex(i);
            string name = SteamMatchmaking.GetLobbyData(lobbyId, NameField);

            if (string.IsNullOrWhiteSpace(name))
                name = "Steam Lobby";

            string hostSteamId = SteamMatchmaking.GetLobbyData(lobbyId, HostSteamIdField);
            int playerCount = SteamMatchmaking.GetNumLobbyMembers(lobbyId);
            int maxPlayers = SteamMatchmaking.GetLobbyMemberLimit(lobbyId);

            LobbyInfo lobby = new LobbyInfo(
                LobbyProviderKind.Steam,
                NetworkTransportKind.Steam,
                lobbyId.m_SteamID.ToString(),
                name,
                hostSteamId,
                playerCount,
                maxPlayers
            );

            _lobbies.Add(lobby);
        }

        Publish();
        StatusChanged?.Invoke(_lobbies.Count == 0 ? "No Steam lobbies found." : $"Found {_lobbies.Count} Steam lobby(s).");
    }

    private void JoinSteamLobby(CSteamID lobbyId)
    {
        SteamAPICall_t call = SteamMatchmaking.JoinLobby(lobbyId);
        _lobbyEnteredResult.Set(call);
        StatusChanged?.Invoke("Joining Steam lobby...");
    }

    private void HandleLobbyEntered(LobbyEnter_t result, bool ioFailure)
    {
        EChatRoomEnterResponse response = (EChatRoomEnterResponse)result.m_EChatRoomEnterResponse;

        if (ioFailure || response != EChatRoomEnterResponse.k_EChatRoomEnterResponseSuccess)
        {
            StatusChanged?.Invoke($"Could not enter Steam lobby: {response}.");
            return;
        }

        CSteamID lobbyId = new CSteamID(result.m_ulSteamIDLobby);
        _currentLobbyId = lobbyId.m_SteamID;

        string hostSteamId = SteamMatchmaking.GetLobbyData(lobbyId, HostSteamIdField);

        if (string.IsNullOrWhiteSpace(hostSteamId))
            hostSteamId = SteamMatchmaking.GetLobbyOwner(lobbyId).m_SteamID.ToString();

        if (string.IsNullOrWhiteSpace(hostSteamId) || hostSteamId == "0")
        {
            SteamMatchmaking.LeaveLobby(lobbyId);
            _currentLobbyId = 0;
            StatusChanged?.Invoke("Steam lobby does not have a valid host.");
            return;
        }

        bool clientStarted = NetworkSessionManager.Instance.StartClient(NetworkTransportKind.Steam, hostSteamId);

        if (!clientStarted)
        {
            SteamMatchmaking.LeaveLobby(lobbyId);
            _currentLobbyId = 0;
            StatusChanged?.Invoke("FishNet Steam client failed to start.");
            return;
        }

        StatusChanged?.Invoke("Connected to Steam lobby.");
    }

    private void HandleLobbyJoinRequested(GameLobbyJoinRequested_t result)
    {
        if (NetworkSessionManager.Instance != null && (NetworkSessionManager.Instance.IsClientStarted || NetworkSessionManager.Instance.IsServerStarted))
            LobbyService.Instance?.Disconnect();

        JoinSteamLobby(result.m_steamIDLobby);
    }

    private void TryJoinLobbyFromCommandLine()
    {
        string[] arguments = Environment.GetCommandLineArgs();

        for (int i = 0; i < arguments.Length - 1; i++)
        {
            if (!arguments[i].Equals("+connect_lobby", StringComparison.OrdinalIgnoreCase))
                continue;

            if (!ulong.TryParse(arguments[i + 1], out ulong lobbyId))
                continue;

            JoinSteamLobby(new CSteamID(lobbyId));
            return;
        }
    }
#endif

    private void Publish()
    {
        LobbiesChanged?.Invoke(Kind, new List<LobbyInfo>(_lobbies));
    }

    private void ReportSteamUnavailable()
    {
        StatusChanged?.Invoke("Steamworks.NET is not installed or Steam is unavailable.");
    }
}

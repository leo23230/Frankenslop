using System;
using System.Collections.Generic;

public interface ILobbyProvider
{
    LobbyProviderKind Kind { get; }
    bool IsAvailable { get; }
    event Action<LobbyProviderKind, IReadOnlyList<LobbyInfo>> LobbiesChanged;
    event Action<string> StatusChanged;
    void Host(string lobbyName, int maxPlayers);
    void Refresh();
    void Join(LobbyInfo lobby);
    void Leave();
}

public interface ILobbyInviteProvider
{
    bool CanInvite { get; }
    void OpenInviteOverlay();
}

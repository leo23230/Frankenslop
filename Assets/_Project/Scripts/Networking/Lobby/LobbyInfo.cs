using System;

[Serializable]
public sealed class LobbyInfo
{
    public LobbyProviderKind Provider { get; }
    public NetworkTransportKind Transport { get; }
    public string Id { get; }
    public string Name { get; }
    public string ConnectionAddress { get; }
    public int PlayerCount { get; }
    public int MaxPlayers { get; }

    public LobbyInfo(LobbyProviderKind provider, NetworkTransportKind transport, string id, string name, string connectionAddress, int playerCount, int maxPlayers)
    {
        Provider = provider;
        Transport = transport;
        Id = id;
        Name = name;
        ConnectionAddress = connectionAddress;
        PlayerCount = playerCount;
        MaxPlayers = maxPlayers;
    }
}

using FishNet.Managing;
using FishNet.Transporting;
using FishNet.Transporting.Multipass;
using FishNet.Transporting.Tugboat;
using FishySteamworks;
using UnityEngine;

public sealed class NetworkSessionManager : MonoBehaviour
{
    public static NetworkSessionManager Instance { get; private set; }

    [Header("FishNet")]
    [SerializeField] private NetworkManager networkManager;
    [SerializeField] private Multipass multipass;

    [Header("Transports")]
    [SerializeField] private Tugboat tugboatTransport;
    [SerializeField] private FishySteamworks.FishySteamworks steamTransport;

    private int _activeTransportIndex = -1;

    public NetworkTransportKind? ActiveTransport { get; private set; }
    public bool IsClientStarted => networkManager != null && networkManager.ClientManager.Started;
    public bool IsServerStarted => networkManager != null && networkManager.ServerManager.Started;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public bool StartHost(NetworkTransportKind transportKind, int maxPlayers)
    {
        if (!ValidateReady()) return false;

        if (IsClientStarted || IsServerStarted)
        {
            Debug.LogWarning("Cannot start a host while a network session is already running.", this);
            return false;
        }

        Transport transport = GetTransport(transportKind);
        int index = GetTransportIndex(transport);

        if (transport == null || index < 0)
        {
            Debug.LogError($"Transport {transportKind} is not configured.", this);
            return false;
        }

        _activeTransportIndex = index;
        ActiveTransport = transportKind;
        multipass.SetClientTransport(transport);
        multipass.SetMaximumClients(Mathf.Max(1, maxPlayers), index);

        if (transportKind == NetworkTransportKind.Tugboat)
            multipass.SetClientAddress("127.0.0.1", index);

        bool serverStarted = multipass.StartConnection(true, index);

        if (!serverStarted)
        {
            ClearActiveTransport();
            return false;
        }

        bool clientStarted = networkManager.ClientManager.StartConnection();

        if (!clientStarted)
        {
            multipass.StopServerConnection(false, index);
            ClearActiveTransport();
            return false;
        }

        return true;
    }

    public bool StartClient(NetworkTransportKind transportKind, string address)
    {
        if (!ValidateReady()) return false;

        if (IsClientStarted || IsServerStarted)
        {
            Debug.LogWarning("Cannot join while a network session is already running.", this);
            return false;
        }

        if (string.IsNullOrWhiteSpace(address))
        {
            Debug.LogError("Cannot join without a connection address.", this);
            return false;
        }

        Transport transport = GetTransport(transportKind);
        int index = GetTransportIndex(transport);

        if (transport == null || index < 0)
        {
            Debug.LogError($"Transport {transportKind} is not configured.", this);
            return false;
        }

        _activeTransportIndex = index;
        ActiveTransport = transportKind;
        multipass.SetClientTransport(transport);
        multipass.SetClientAddress(address.Trim(), index);

        bool started = networkManager.ClientManager.StartConnection();

        if (!started)
            ClearActiveTransport();

        return started;
    }

    public void Disconnect()
    {
        if (!ValidateReady()) return;

        int serverTransportIndex = _activeTransportIndex;

        if (networkManager.ClientManager.Started)
            networkManager.ClientManager.StopConnection();

        if (serverTransportIndex >= 0 && networkManager.ServerManager.Started)
            multipass.StopServerConnection(true, serverTransportIndex);

        ClearActiveTransport();
    }

    private Transport GetTransport(NetworkTransportKind kind)
    {
        switch (kind)
        {
            case NetworkTransportKind.Tugboat:
                return tugboatTransport;
            case NetworkTransportKind.Steam:
                return steamTransport;
            default:
                return null;
        }
    }

    private int GetTransportIndex(Transport transport)
    {
        if (transport == null || multipass == null) return -1;

        for (int i = 0; i < multipass.Transports.Count; i++)
        {
            if (ReferenceEquals(multipass.Transports[i], transport))
                return i;
        }

        return -1;
    }

    private bool ValidateReady()
    {
        if (networkManager == null)
        {
            Debug.LogError("NetworkSessionManager has no NetworkManager.", this);
            return false;
        }

        if (multipass == null)
        {
            Debug.LogError("NetworkSessionManager has no Multipass transport.", this);
            return false;
        }

        return true;
    }

    private void ClearActiveTransport()
    {
        _activeTransportIndex = -1;
        ActiveTransport = null;
    }
}

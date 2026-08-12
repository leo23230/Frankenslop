using System.Collections.Generic;
using FishNet.Managing;
using FishNet.Transporting;
using TMPro;
using UnityEngine;

public sealed class MainMenuController : MonoBehaviour
{
    [Header("Services")]
    [SerializeField] private LobbyService lobbyService;
    [SerializeField] private NetworkManager networkManager;

    [Header("Panels")]
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private GameObject joinPanel;

    [Header("Hosting")]
    [Tooltip("Dropdown option 0 = LAN, option 1 = Steam.")]
    [SerializeField] private TMP_Dropdown providerDropdown;
    [SerializeField] private TMP_InputField lobbyNameInput;
    [SerializeField, Min(1)] private int maxPlayers = 4;

    [Header("Lobby Browser")]
    [SerializeField] private Transform lobbyListRoot;
    [SerializeField] private LobbyListItemUI lobbyItemPrefab;

    [Header("Status")]
    [SerializeField] private TMP_Text statusText;

    private readonly List<LobbyListItemUI> _spawnedRows = new();

    private void Start()
    {
        if (lobbyService != null)
        {
            lobbyService.LobbiesChanged += HandleLobbiesChanged;
            lobbyService.StatusChanged += HandleStatusChanged;
        }

        if (networkManager != null)
            networkManager.ClientManager.OnClientConnectionState += HandleClientConnectionState;

        if (joinPanel != null)
            joinPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (lobbyService != null)
        {
            lobbyService.LobbiesChanged -= HandleLobbiesChanged;
            lobbyService.StatusChanged -= HandleStatusChanged;
        }

        if (networkManager != null)
            networkManager.ClientManager.OnClientConnectionState -= HandleClientConnectionState;
    }

    public void Host()
    {
        if (lobbyService == null) return;

        LobbyProviderKind provider = providerDropdown != null && providerDropdown.value == 1 ? LobbyProviderKind.Steam : LobbyProviderKind.Lan;
        string lobbyName = lobbyNameInput != null ? lobbyNameInput.text : "My Lobby";
        lobbyService.Host(provider, lobbyName, maxPlayers);
    }

    public void OpenJoinBrowser()
    {
        if (joinPanel != null)
            joinPanel.SetActive(true);

        RefreshLobbies();
    }

    public void CloseJoinBrowser()
    {
        if (joinPanel != null)
            joinPanel.SetActive(false);
    }

    public void RefreshLobbies()
    {
        lobbyService?.RefreshAll();
    }

    public void Disconnect()
    {
        lobbyService?.Disconnect();
    }

    public void Quit()
    {
        Application.Quit();
    }

    private void HandleLobbiesChanged(IReadOnlyList<LobbyInfo> lobbies)
    {
        ClearLobbyRows();

        if (lobbyItemPrefab == null || lobbyListRoot == null || lobbies == null)
            return;

        for (int i = 0; i < lobbies.Count; i++)
        {
            LobbyInfo lobby = lobbies[i];
            LobbyListItemUI row = Instantiate(lobbyItemPrefab, lobbyListRoot);
            row.Initialize(lobby, HandleJoinClicked);
            _spawnedRows.Add(row);
        }
    }

    private void HandleJoinClicked(LobbyInfo lobby)
    {
        lobbyService?.Join(lobby);
    }

    private void HandleStatusChanged(string message)
    {
        if (statusText != null)
            statusText.text = message;

        Debug.Log($"Lobby: {message}", this);
    }

    private void HandleClientConnectionState(ClientConnectionStateArgs args)
    {
        if (menuPanel == null) return;

        switch (args.ConnectionState)
        {
            case LocalConnectionState.Started:
                menuPanel.SetActive(false);
                break;
            case LocalConnectionState.Stopped:
                menuPanel.SetActive(true);
                break;
        }
    }

    private void ClearLobbyRows()
    {
        for (int i = 0; i < _spawnedRows.Count; i++)
        {
            if (_spawnedRows[i] != null)
                Destroy(_spawnedRows[i].gameObject);
        }

        _spawnedRows.Clear();
    }
}

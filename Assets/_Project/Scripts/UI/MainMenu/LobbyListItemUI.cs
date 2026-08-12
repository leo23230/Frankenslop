using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class LobbyListItemUI : MonoBehaviour
{
    [SerializeField] private TMP_Text lobbyNameText;
    [SerializeField] private TMP_Text providerText;
    [SerializeField] private TMP_Text playerCountText;
    [SerializeField] private Button joinButton;

    private LobbyInfo _lobby;
    private Action<LobbyInfo> _joinCallback;

    public void Initialize(LobbyInfo lobby, Action<LobbyInfo> joinCallback)
    {
        _lobby = lobby;
        _joinCallback = joinCallback;

        if (lobbyNameText != null)
            lobbyNameText.text = lobby.Name;

        if (providerText != null)
            providerText.text = lobby.Provider.ToString();

        if (playerCountText != null)
            playerCountText.text = lobby.PlayerCount >= 0 && lobby.MaxPlayers > 0 ? $"{lobby.PlayerCount}/{lobby.MaxPlayers}" : "LAN";

        if (joinButton != null)
        {
            joinButton.onClick.RemoveAllListeners();
            joinButton.onClick.AddListener(Join);
        }
    }

    private void Join()
    {
        _joinCallback?.Invoke(_lobby);
    }
}

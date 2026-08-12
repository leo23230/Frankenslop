using FishNet;
using UnityEngine;
using UnityEngine.UI;

public sealed class LobbySceneUI : MonoBehaviour
{
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button inviteButton;

    private void Start()
    {
        bool isHost = InstanceFinder.ServerManager != null && InstanceFinder.ServerManager.Started;

        if (startGameButton != null)
            startGameButton.gameObject.SetActive(isHost);

        if (inviteButton != null)
        {
            bool canInvite = LobbyService.Instance != null && LobbyService.Instance.CanInvite;
            inviteButton.gameObject.SetActive(canInvite);
        }
    }

    public void StartGame()
    {
        if (InstanceFinder.ServerManager == null || !InstanceFinder.ServerManager.Started)
            return;

        NetworkGameManager.Instance?.StartGame();
    }

    public void InviteFriends()
    {
        LobbyService.Instance?.OpenInviteOverlay();
    }

    public void LeaveLobby()
    {
        LobbyService.Instance?.Disconnect();
    }
}

using TMPro;
using UnityEngine;

public sealed class LobbyPlayerListItemUI : MonoBehaviour
{
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text hostText;

    public void SetPlayer(int clientId, string playerName)
    {
        if (playerNameText != null)
            playerNameText.text = playerName;

        if (hostText != null)
            hostText.text = clientId == 0 ? "HOST" : string.Empty;
    }
}
using System.Collections.Generic;
using UnityEngine;

public sealed class LobbyPlayerListUI : MonoBehaviour
{
    [SerializeField] private Transform playerListRoot;
    [SerializeField] private LobbyPlayerListItemUI playerItemPrefab;

    private readonly List<LobbyPlayerListItemUI> _spawnedItems = new();
    private LobbyPlayerRoster _roster;

    private void Update()
    {
        if (_roster != null)
            return;

        if (LobbyPlayerRoster.Instance == null)
            return;

        BindRoster(LobbyPlayerRoster.Instance);
    }

    private void OnDisable()
    {
        UnbindRoster();
        ClearPlayerItems();
    }

    private void BindRoster(LobbyPlayerRoster roster)
    {
        UnbindRoster();

        _roster = roster;
        _roster.PlayersChanged += HandlePlayersChanged;
        RebuildPlayerList(_roster.Players);
    }

    private void UnbindRoster()
    {
        if (_roster == null)
            return;

        _roster.PlayersChanged -= HandlePlayersChanged;
        _roster = null;
    }

    private void HandlePlayersChanged(IReadOnlyDictionary<int, string> players)
    {
        RebuildPlayerList(players);
    }

    private void RebuildPlayerList(IReadOnlyDictionary<int, string> players)
    {
        ClearPlayerItems();

        if (playerListRoot == null || playerItemPrefab == null || players == null)
            return;

        List<int> clientIds = new(players.Keys);
        clientIds.Sort();

        foreach (int clientId in clientIds)
        {
            LobbyPlayerListItemUI item = Instantiate(playerItemPrefab, playerListRoot);
            item.SetPlayer(clientId, players[clientId]);
            _spawnedItems.Add(item);
        }
    }

    private void ClearPlayerItems()
    {
        for (int i = 0; i < _spawnedItems.Count; i++)
        {
            if (_spawnedItems[i] != null)
                Destroy(_spawnedItems[i].gameObject);
        }

        _spawnedItems.Clear();
    }
}
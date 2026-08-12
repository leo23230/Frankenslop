using FishNet.Connection;
using FishNet.Object;
using UnityEngine;

public class PlayerSpawner : NetworkBehaviour
{
    [SerializeField] private NetworkObject playerControlPrefab;

    public override void OnSpawnServer(NetworkConnection connection)
    {
        if (playerControlPrefab == null)
        {
            Debug.LogError("GameplayPlayerSpawner has no Player Control Prefab assigned.", this);
            return;
        }

        NetworkObject playerObject = NetworkManager.GetPooledInstantiated(playerControlPrefab, asServer: true);
        Spawn(playerObject, connection, gameObject.scene);
    }
}

using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Spawns a player prefab for each connected player in the game scene.
/// This script should be on a GameObject in the GameMap scene.
/// Both host and clients will instantiate player prefabs for all players.
/// </summary>
public class NetworkPlayerSpawner : NetworkBehaviour
{
    [Header("Player Prefab")]
    [SerializeField] private GameObject playerPrefab;

    [Header("Spawn Settings")]
    [SerializeField] private Transform spawnPointsParent;
    [SerializeField] private Vector3[] customSpawnPoints = new Vector3[0];

    private void Start()
    {
        if (!IsServer)
        {
            Debug.Log("⏸️ NetworkPlayerSpawner: Not server, skipping spawn logic");
            return;
        }

        if (playerPrefab == null)
        {
            Debug.LogError("❌ NetworkPlayerSpawner: playerPrefab is not assigned!");
            return;
        }

        Debug.Log($"🎮 Spawning players for {NetworkManager.Singleton.ConnectedClientsIds.Count + 1} connected players (including host)");

        // Spawn for all connected clients
        int spawnIndex = 0;
        foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            SpawnPlayerForClient(clientId, spawnIndex);
            spawnIndex++;
        }

        // Spawn for host (local player)
        SpawnPlayerForClient(NetworkManager.Singleton.LocalClientId, spawnIndex);
    }

    private void SpawnPlayerForClient(ulong clientId, int spawnIndex)
    {
        Vector3 spawnPos = GetSpawnPosition(spawnIndex);

        Debug.Log($"🎯 Spawning player for clientId={clientId} at position {spawnPos}");

        GameObject playerInstance = Instantiate(playerPrefab, spawnPos, Quaternion.identity);

        // Find NetworkObject and spawn it with ownership
        NetworkObject networkObj = playerInstance.GetComponent<NetworkObject>();
        if (networkObj != null)
        {
            networkObj.SpawnWithOwnership(clientId);
        }
        else
        {
            Debug.LogError($"❌ Player prefab does not have NetworkObject component!");
            Destroy(playerInstance);
        }
    }

    private Vector3 GetSpawnPosition(int index)
    {
        // Try custom spawn points first
        if (customSpawnPoints != null && customSpawnPoints.Length > index && customSpawnPoints[index] != Vector3.zero)
        {
            return customSpawnPoints[index];
        }

        // Try spawn points parent
        if (spawnPointsParent != null && spawnPointsParent.childCount > index)
        {
            return spawnPointsParent.GetChild(index).position;
        }

        // Fallback: circle spawn around origin
        float angle = (index / 9f) * Mathf.PI * 2f;
        float radius = 3f;
        return new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
    }
}
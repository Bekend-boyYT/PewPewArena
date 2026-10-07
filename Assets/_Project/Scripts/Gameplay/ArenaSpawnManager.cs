using Unity.Netcode;
using UnityEngine;
using SniperGame.Player;

namespace SniperGame.Gameplay
{
    public class ArenaSpawnManager : NetworkBehaviour
    {
        public static ArenaSpawnManager Instance { get; private set; }

        [Header("Spawn Points (Minimaal 2 nodig)")]
        [SerializeField] private Transform[] spawnPoints;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            EnsureSpawnPoints();
        }

        private void Start()
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer && !IsSpawned)
            {
                var netObj = GetComponent<NetworkObject>();
                if (netObj != null && !netObj.IsSpawned)
                {
                    netObj.Spawn();
                }
            }
        }

        private void EnsureSpawnPoints()
        {
            if (spawnPoints != null && spawnPoints.Length >= 2 && spawnPoints[0] != null && spawnPoints[1] != null)
            {
                return;
            }

            var list = new System.Collections.Generic.List<Transform>();

            Transform p1 = GameObject.Find("SpawnPoint_P1")?.transform 
                        ?? GameObject.Find("SpawnPoint1")?.transform 
                        ?? GameObject.Find("Spawn Point 1")?.transform;

            Transform p2 = GameObject.Find("SpawnPoint_P2")?.transform 
                        ?? GameObject.Find("SpawnPoint2")?.transform 
                        ?? GameObject.Find("Spawn Point 2")?.transform;

            if (p1 != null) list.Add(p1);
            if (p2 != null) list.Add(p2);

            if (list.Count < 2)
            {
                var respawns = GameObject.FindGameObjectsWithTag("Respawn");
                foreach (var r in respawns)
                {
                    if (!list.Contains(r.transform)) list.Add(r.transform);
                    if (list.Count >= 2) break;
                }
            }

            if (list.Count < 2)
            {
                foreach (var go in FindObjectsByType<GameObject>())
                {
                    if (go.name.StartsWith("SpawnPoint", System.StringComparison.OrdinalIgnoreCase) && !list.Contains(go.transform))
                    {
                        list.Add(go.transform);
                        if (list.Count >= 2) break;
                    }
                }
            }

            if (list.Count >= 2)
            {
                spawnPoints = list.ToArray();
                Debug.Log($"[ArenaSpawnManager] Auto-assigned {spawnPoints.Length} spawn points: {spawnPoints[0].name}, {spawnPoints[1].name}");
            }
            else
            {
                Debug.LogWarning($"[ArenaSpawnManager] Found {list.Count} spawn points in scene, minimum 2 needed!");
            }
        }

        /// <summary>
        /// Teleporteert alle verbonden spelers naar spawnpoints op basis van het huidige rondenummer.
        /// Even ronde = omgedraaide spawnpoints.
        /// </summary>
        public void RespawnAllPlayers(int currentRound)
        {
            if (!IsServer) return;

            if (spawnPoints == null || spawnPoints.Length < 2 || spawnPoints[0] == null || spawnPoints[1] == null)
            {
                EnsureSpawnPoints();
            }

            if (spawnPoints == null || spawnPoints.Length < 2)
            {
                Debug.LogError("[ArenaSpawnManager] Minimaal 2 spawnpoints toewijzen in de Inspector!");
                return;
            }

            var clients = NetworkManager.Singleton.ConnectedClientsList;

            for (int i = 0; i < clients.Count; i++)
            {
                var client = clients[i];
                if (client.PlayerObject != null)
                {
                    // Ronde 1: Speler 0 -> Spawn 0, Speler 1 -> Spawn 1
                    // Ronde 2: Speler 0 -> Spawn 1, Speler 1 -> Spawn 0
                    // Ronde 3: Speler 0 -> Spawn 0, Speler 1 -> Spawn 1
                    int spawnIndex = (i + (currentRound - 1)) % spawnPoints.Length;
                    Transform targetSpawn = spawnPoints[spawnIndex];

                    var movement = client.PlayerObject.GetComponent<PlayerMovement>();
                    if (movement != null)
                    {
                        movement.TeleportClientRpc(targetSpawn.position, targetSpawn.rotation);
                    }
                }
            }
        }
    }
}
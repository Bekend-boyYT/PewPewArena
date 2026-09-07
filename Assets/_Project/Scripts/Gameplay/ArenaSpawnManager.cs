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
        }

        /// <summary>
        /// Teleporteert alle verbonden spelers naar spawnpoints op basis van het huidige rondenummer.
        /// Even ronde = omgedraaide spawnpoints.
        /// </summary>
        public void RespawnAllPlayers(int currentRound)
        {
            if (!IsServer) return;

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
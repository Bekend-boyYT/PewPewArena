using System.Collections;
using Unity.Netcode;
using UnityEngine;
using SniperGame.Player;
using ProjectPlayerMovement = SniperGame.Player.PlayerMovement;

namespace SniperGame.Gameplay
{
    public class ArenaSpawnManager : NetworkBehaviour
    {
        public static ArenaSpawnManager Instance { get; private set; }

        [Header("Spawn Points")]
        [SerializeField] private Transform spawnPointP1;
        [SerializeField] private Transform spawnPointP2;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsServer)
            {
                // Wacht een fractie van een seconde zodat alle spelers netjes in de scene geladen zijn
                StartCoroutine(PositionAllPlayersRoutine());
            }
        }

        private IEnumerator PositionAllPlayersRoutine()
        {
            yield return new WaitForSeconds(0.2f);
            RespawnAllPlayers();
        }

        /// <summary>
        /// Plaatst alle verbonden spelers op hun respectievelijke spawnpoint.
        /// </summary>
        public void RespawnAllPlayers()
        {
            if (!IsServer) return;

            var clients = NetworkManager.Singleton.ConnectedClientsList;

            for (int i = 0; i < clients.Count; i++)
            {
                var client = clients[i];
                if (client.PlayerObject == null) continue;

                var playerMovement = client.PlayerObject.GetComponent<ProjectPlayerMovement>();
                if (playerMovement == null) continue;

                // Speler 0 (Host) krijgt Spawn 1, Speler 1 (Client) krijgt Spawn 2
                Transform targetSpawn = (i == 0) ? spawnPointP1 : spawnPointP2;

                if (targetSpawn != null)
                {
                    playerMovement.TeleportClientRpc(targetSpawn.position, targetSpawn.rotation);
                    Debug.Log($"[SpawnManager] Speler {client.ClientId} geteleporteerd naar Spawn {(i == 0 ? "1" : "2")}");
                }
            }
        }

        public Transform GetSpawnPoint(int playerIndex)
        {
            return playerIndex == 0 ? spawnPointP1 : spawnPointP2;
        }
    }
}
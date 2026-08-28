using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using SniperGame.Player;
using SniperGame.UI;

namespace SniperGame.Gameplay
{
    public enum MatchState
    {
        WaitingForPlayers,
        Countdown,
        InRound,
        RoundEnded,
        MatchEnded
    }

    public class RoundManager : NetworkBehaviour
    {
        public static RoundManager Instance { get; private set; }

        [Header("Match Settings")]
        [SerializeField] private int roundsToWin = 2; // Best of 3 (eerste met 2 punten wint)
        [SerializeField] private float countdownDuration = 3f;
        [SerializeField] private float roundEndDelay = 3f;

        [Header("State (Synchronized)")]
        public NetworkVariable<MatchState> CurrentState = new NetworkVariable<MatchState>(
            MatchState.WaitingForPlayers,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public NetworkVariable<int> HostScore = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<int> ClientScore = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<int> CurrentRound = new NetworkVariable<int>(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            HostScore.OnValueChanged += (prev, val) => UpdateScoresUI();
            ClientScore.OnValueChanged += (prev, val) => UpdateScoresUI();

            if (IsServer)
            {
                StartCoroutine(InitialMatchStartRoutine());
            }
        }

        private IEnumerator InitialMatchStartRoutine()
        {
            // Wacht kort tot beide spelers volledig geladen zijn
            yield return new WaitForSeconds(0.5f);
            StartNewRound();
        }

        public bool CanPlayersFight()
        {
            return CurrentState.Value == MatchState.InRound;
        }

        private void StartNewRound()
        {
            if (!IsServer) return;

            // 1. Reset en teleporteer alle spelers
            ResetAllPlayers();

            // 2. Start Countdown
            StartCoroutine(RoundCountdownRoutine());
        }

        private IEnumerator RoundCountdownRoutine()
        {
            CurrentState.Value = MatchState.Countdown;

            for (int i = (int)countdownDuration; i > 0; i--)
            {
                ShowAnnouncementClientRpc($"{i}", Color.yellow, 0.9f);
                yield return new WaitForSeconds(1.0f);
            }

            ShowAnnouncementClientRpc("FIGHT!", Color.green, 1.0f);
            CurrentState.Value = MatchState.InRound;
        }

        public void OnPlayerDied(ulong victimClientId)
        {
            if (!IsServer || CurrentState.Value != MatchState.InRound) return;

            CurrentState.Value = MatchState.RoundEnded;

            // Bepaal winnaar van de ronde (de andere speler)
            ulong winnerClientId = GetOtherPlayerClientId(victimClientId);

            bool isHostWinner = (winnerClientId == NetworkManager.Singleton.LocalClientId);

            if (isHostWinner) HostScore.Value++;
            else ClientScore.Value++;

            UpdateScoresUI();

            // Controleer of match gewonnen is (Best of 3)
            if (HostScore.Value >= roundsToWin || ClientScore.Value >= roundsToWin)
            {
                StartCoroutine(EndMatchRoutine(winnerClientId));
            }
            else
            {
                StartCoroutine(EndRoundRoutine(winnerClientId));
            }
        }

        private IEnumerator EndRoundRoutine(ulong roundWinnerClientId)
        {
            NotifyRoundOutcomeClientRpc(roundWinnerClientId);

            yield return new WaitForSeconds(roundEndDelay);

            CurrentRound.Value++;
            StartNewRound();
        }

        private IEnumerator EndMatchRoutine(ulong matchWinnerClientId)
        {
            CurrentState.Value = MatchState.MatchEnded;

            NotifyMatchOutcomeClientRpc(matchWinnerClientId);

            yield return null;
        }

        private void ResetAllPlayers()
        {
            if (ArenaSpawnManager.Instance != null)
            {
                ArenaSpawnManager.Instance.RespawnAllPlayers();
            }

            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                if (client.PlayerObject != null)
                {
                    var health = client.PlayerObject.GetComponent<PlayerHealth>();
                    if (health != null)
                    {
                        // Herstel HP naar 100
                        health.ResetHealthServer();
                    }
                }
            }
        }

        private ulong GetOtherPlayerClientId(ulong victimClientId)
        {
            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                if (client.ClientId != victimClientId) return client.ClientId;
            }
            return NetworkManager.Singleton.LocalClientId;
        }

        private void UpdateScoresUI()
        {
            if (CombatHUD.Instance != null)
            {
                CombatHUD.Instance.UpdateMatchScore(HostScore.Value, ClientScore.Value, CurrentRound.Value);
            }
        }

        [ClientRpc]
        private void ShowAnnouncementClientRpc(string message, Color color, float duration)
        {
            if (CombatHUD.Instance != null)
            {
                CombatHUD.Instance.ShowAnnouncement(message, color, duration);
            }
        }

        [ClientRpc]
        private void NotifyRoundOutcomeClientRpc(ulong winnerClientId)
        {
            bool isMeWinner = (NetworkManager.Singleton.LocalClientId == winnerClientId);
            string msg = isMeWinner ? "RONDE GEWONNEN!" : "RONDE VERLOREN!";
            Color col = isMeWinner ? Color.green : Color.red;

            if (CombatHUD.Instance != null)
            {
                CombatHUD.Instance.ShowAnnouncement(msg, col, 2.5f);
            }
        }

        [ClientRpc]
        private void NotifyMatchOutcomeClientRpc(ulong winnerClientId)
        {
            bool isMeWinner = (NetworkManager.Singleton.LocalClientId == winnerClientId);

            if (CombatHUD.Instance != null)
            {
                CombatHUD.Instance.ShowMatchEndScreen(isMeWinner);
            }
        }
    }
}
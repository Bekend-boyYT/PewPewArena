using System.Collections;
using Unity.Netcode;
using UnityEngine;
using SniperGame.Player;
using SniperGame.UI;
using SniperGame.Weapons;

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
        [SerializeField] private float roundDuration = 60f; // 60 seconden per ronde
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
        public NetworkVariable<float> RoundTimeRemaining = new NetworkVariable<float>(60f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

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

            HostScore.OnValueChanged += (prev, val) => UpdateScoresUI();
            ClientScore.OnValueChanged += (prev, val) => UpdateScoresUI();
            RoundTimeRemaining.OnValueChanged += (prev, val) => UpdateTimerUI(val);

            if (IsServer)
            {
                StartCoroutine(InitialMatchStartRoutine());
            }
        }

        private void Update()
        {
            if (!IsServer) return;

            // Timer synchroon aftellen tijdens een actieve ronde
            if (CurrentState.Value == MatchState.InRound)
            {
                RoundTimeRemaining.Value -= Time.deltaTime;

                if (RoundTimeRemaining.Value <= 0f)
                {
                    RoundTimeRemaining.Value = 0f;
                    OnTimeExpired();
                }
            }
        }

        private IEnumerator InitialMatchStartRoutine()
        {
            // Wacht kort tot beide spelers volledig in de gameplay scene geladen zijn
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

            // 1. Reset HP, munitie en teleporteer naar spawnpoints
            ResetAllPlayers();

            // 2. Reset timer
            RoundTimeRemaining.Value = roundDuration;

            // 3. Start de 3-seconden aftelling
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

        private void OnTimeExpired()
        {
            if (CurrentState.Value != MatchState.InRound) return;

            CurrentState.Value = MatchState.RoundEnded;
            ShowAnnouncementClientRpc("TIJD IS OM!", Color.yellow, 2.5f);

            StartCoroutine(EndDrawRoundRoutine());
        }

        private IEnumerator EndDrawRoundRoutine()
        {
            yield return new WaitForSeconds(roundEndDelay);
            CurrentRound.Value++;
            StartNewRound();
        }

        public void OnPlayerDied(ulong victimClientId)
        {
            if (!IsServer || CurrentState.Value != MatchState.InRound) return;

            CurrentState.Value = MatchState.RoundEnded;

            // Bepaal de winnaar van de ronde (de overlevende speler)
            ulong winnerClientId = GetOtherPlayerClientId(victimClientId);
            bool isHostWinner = (winnerClientId == NetworkManager.Singleton.LocalClientId);

            if (isHostWinner) HostScore.Value++;
            else ClientScore.Value++;

            UpdateScoresUI();

            // Controleer of de wedstrijd beslist is (Best of 3)
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
            // Teleporteer spelers naar hun respectievelijke spawns
            if (ArenaSpawnManager.Instance != null)
            {
                ArenaSpawnManager.Instance.RespawnAllPlayers();
            }

            // Herstel levenspunten en munitie
            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                if (client.PlayerObject != null)
                {
                    var health = client.PlayerObject.GetComponent<PlayerHealth>();
                    if (health != null)
                    {
                        health.ResetHealthServer();
                    }

                    var weapon = client.PlayerObject.GetComponent<SniperWeapon>();
                    if (weapon != null)
                    {
                        weapon.ResetAmmo();
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

        private void UpdateTimerUI(float timeRemaining)
        {
            if (CombatHUD.Instance != null)
            {
                CombatHUD.Instance.UpdateTimer(timeRemaining);
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
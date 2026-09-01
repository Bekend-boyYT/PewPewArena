using Unity.Netcode;
using UnityEngine;
using SniperGame.UI;
using SniperGame.Gameplay;

namespace SniperGame.Player
{
    public class PlayerHealth : NetworkBehaviour
    {
        [Header("Health Settings")]
        [SerializeField] private int maxHealth = 100;

        public NetworkVariable<int> CurrentHealth = new NetworkVariable<int>(
            100,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        [Header("Components to Disable on Death")]
        [SerializeField] private CharacterController characterController;
        [SerializeField] private MeshRenderer visualsRenderer;
        [SerializeField] private PlayerLook playerLook;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            CurrentHealth.OnValueChanged += OnHealthChanged;

            // Directe UI-update bij het spawnen van de speler (voor zowel Host als Client)
            if (IsOwner && CombatHUD.Instance != null)
            {
                CombatHUD.Instance.UpdateHealth(CurrentHealth.Value, maxHealth);
            }
        }

        public override void OnNetworkDespawn()
        {
            CurrentHealth.OnValueChanged -= OnHealthChanged;
            base.OnNetworkDespawn();
        }

        private void OnHealthChanged(int previousValue, int newValue)
        {
            if (IsOwner && CombatHUD.Instance != null)
            {
                CombatHUD.Instance.UpdateHealth(newValue, maxHealth);
            }

            if (newValue <= 0)
            {
                HandleDeath();
            }
            else if (previousValue <= 0 && newValue > 0)
            {
                HandleRevive();
            }
        }

        public void TakeDamage(int damageAmount, ulong attackerClientId)
        {
            if (!IsServer || CurrentHealth.Value <= 0) return;

            int newHp = Mathf.Max(0, CurrentHealth.Value - damageAmount);
            CurrentHealth.Value = newHp;

            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { OwnerClientId } }
            };
            OnTakeDamageFeedbackClientRpc(clientRpcParams);

            if (newHp <= 0 && RoundManager.Instance != null)
            {
                RoundManager.Instance.OnPlayerDied(OwnerClientId);
            }
        }

        [ClientRpc]
        private void OnTakeDamageFeedbackClientRpc(ClientRpcParams clientRpcParams = default)
        {
            if (CombatHUD.Instance != null)
            {
                CombatHUD.Instance.TriggerDamageFlash();
            }

            if (playerLook != null)
            {
                playerLook.AddRecoil(Random.Range(2.5f, 4.5f), Random.Range(-2.5f, 2.5f));
            }
        }

        private void HandleDeath()
        {
            if (characterController != null) characterController.enabled = false;
            if (visualsRenderer != null) visualsRenderer.enabled = false;
        }

        private void HandleRevive()
        {
            if (characterController != null) characterController.enabled = true;
            if (visualsRenderer != null) visualsRenderer.enabled = true;
        }

        public void ResetHealthServer()
        {
            if (!IsServer) return;
            CurrentHealth.Value = maxHealth;
        }
    }
}
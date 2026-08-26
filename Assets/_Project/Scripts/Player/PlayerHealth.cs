using Unity.Netcode;
using UnityEngine;
using SniperGame.UI;

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

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            CurrentHealth.OnValueChanged += OnHealthChanged;
            PushHealthToUI(CurrentHealth.Value);
        }

        private void Start()
        {
            PushHealthToUI(CurrentHealth.Value);
        }

        public override void OnNetworkDespawn()
        {
            CurrentHealth.OnValueChanged -= OnHealthChanged;
            base.OnNetworkDespawn();
        }

        private void OnHealthChanged(int previousValue, int newValue)
        {
            PushHealthToUI(newValue);

            if (newValue <= 0)
            {
                HandleDeath();
            }
        }

        private void PushHealthToUI(int hp)
        {
            if (IsOwner && CombatHUD.Instance != null)
            {
                CombatHUD.Instance.UpdateHealth(hp, maxHealth);
            }
        }

        public void TakeDamage(int damageAmount, ulong attackerClientId)
        {
            if (!IsServer || CurrentHealth.Value <= 0) return;

            int newHp = Mathf.Max(0, CurrentHealth.Value - damageAmount);
            CurrentHealth.Value = newHp;

            Debug.Log($"[Combat] Speler {OwnerClientId} ontving {damageAmount} damage. Resterend: {newHp}");
        }

        private void HandleDeath()
        {
            Debug.Log($"[Combat] Speler {OwnerClientId} is geëlimineerd!");

            if (characterController != null) characterController.enabled = false;
            if (visualsRenderer != null) visualsRenderer.enabled = false;
        }

        public void ResetPlayer(Vector3 spawnPosition)
        {
            if (!IsServer) return;

            CurrentHealth.Value = maxHealth;

            if (characterController != null)
            {
                characterController.enabled = false;
                transform.position = spawnPosition;
                characterController.enabled = true;
            }
            else
            {
                transform.position = spawnPosition;
            }

            if (visualsRenderer != null) visualsRenderer.enabled = true;
        }
    }
}
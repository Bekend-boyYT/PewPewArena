using Unity.Netcode;
using UnityEngine;
using SniperGame.Player;
using SniperGame.UI;
using SniperGame.Gameplay;

namespace SniperGame.Weapons
{
    public class SniperWeapon : NetworkBehaviour
    {
        [Header("Weapon Settings")]
        [SerializeField] private int damage = 100;
        [SerializeField] private float range = 500f;
        [SerializeField] private float fireRate = 1.3f; // Bolt-action tussentijd

        [Header("Scope / ADS Settings")]
        [SerializeField] private float hipFOV = 60f;
        [SerializeField] private float scopedFOV = 18f;
        [SerializeField] private float zoomSpeed = 16f;
        [SerializeField] private float scopedSensitivityMultiplier = 0.35f;

        [Header("References")]
        [SerializeField] private Camera playerCamera;
        [SerializeField] private Transform firePoint;
        [SerializeField] private PlayerLook playerLook;

        [Header("Effects")]
        [SerializeField] private ParticleSystem muzzleFlash;
        [SerializeField] private GameObject hitEffectPrefab;

        [Header("Layers")]
        [SerializeField] private LayerMask hitMask = ~0;

        private float nextFireTime;
        private bool isAiming;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (!IsOwner)
            {
                enabled = false;
            }
        }

        private void Update()
        {
            // Alleen de eigenaar kan dit wapen besturen
            if (!IsOwner) return;

            // Blokkeer schieten en zoomen als het aftellen van de ronde nog bezig is
            if (RoundManager.Instance != null && !RoundManager.Instance.CanPlayersFight())
            {
                ResetAimingState();
                return;
            }

            HandleAiming();

            if (Input.GetMouseButtonDown(0))
            {
                TryShoot();
            }
        }

        private void HandleAiming()
        {
            // Rechtermuisknop ingedrukt houden om in te zoomen
            isAiming = Input.GetMouseButton(1);

            // Vloeiende overgang van de camera FOV
            if (playerCamera != null)
            {
                float targetFOV = isAiming ? scopedFOV : hipFOV;
                playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, Time.deltaTime * zoomSpeed);
            }

            // Gevoeligheid van de muis verlagen tijdens het richten voor meer precisie
            if (playerLook != null)
            {
                playerLook.SetSensitivityMultiplier(isAiming ? scopedSensitivityMultiplier : 1.0f);
            }

            // HUD aansturen (donkere randen / scope overlay)
            if (CombatHUD.Instance != null)
            {
                CombatHUD.Instance.SetScopeActive(isAiming);
            }
        }

        private void ResetAimingState()
        {
            isAiming = false;

            if (playerCamera != null)
            {
                playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, hipFOV, Time.deltaTime * zoomSpeed);
            }

            if (playerLook != null)
            {
                playerLook.SetSensitivityMultiplier(1.0f);
            }

            if (CombatHUD.Instance != null)
            {
                CombatHUD.Instance.SetScopeActive(false);
            }
        }

        private void TryShoot()
        {
            if (Time.time < nextFireTime) return;

            nextFireTime = Time.time + fireRate;

            Vector3 origin;
            Vector3 direction;

            // Schiet direct door het midden van het vizier
            if (playerCamera != null)
            {
                origin = playerCamera.transform.position;
                direction = playerCamera.transform.forward;
            }
            else if (firePoint != null)
            {
                origin = firePoint.position;
                direction = firePoint.forward;
            }
            else
            {
                origin = transform.position;
                direction = transform.forward;
            }

            // Speel lokale flits direct af voor 0ms feedback
            PlayShootEffects();

            // Vraag de server om de Raycast uit te voeren en de hit te valideren
            ShootServerRpc(origin, direction, OwnerClientId);
        }

        [ServerRpc]
        private void ShootServerRpc(
            Vector3 origin,
            Vector3 direction,
            ulong shooterClientId,
            ServerRpcParams serverRpcParams = default)
        {
            direction.Normalize();

            // Server-authoritative Raycast
            if (Physics.Raycast(origin, direction, out RaycastHit hit, range, hitMask, QueryTriggerInteraction.Ignore))
            {
                PlayerHealth targetHealth = hit.collider.GetComponentInParent<PlayerHealth>();

                if (targetHealth != null)
                {
                    // Schiet jezelf niet neer
                    if (targetHealth.OwnerClientId == shooterClientId) return;

                    // Schade toebrengen op de server
                    targetHealth.TakeDamage(damage, shooterClientId);

                    Debug.Log($"[Sniper] Speler {shooterClientId} raakte Speler {targetHealth.OwnerClientId} voor {damage} damage.");

                    // Stuur hitmarker bevestiging specifiek terug naar de schutter
                    ClientRpcParams clientRpcParams = new ClientRpcParams
                    {
                        Send = new ClientRpcSendParams
                        {
                            TargetClientIds = new ulong[] { shooterClientId }
                        }
                    };
                    ConfirmHitClientRpc(clientRpcParams);

                    // Spawn impact/kogel effect voor alle spelers
                    SpawnHitEffectClientRpc(hit.point, hit.normal);
                }
                else
                {
                    // Muur of vloer geraakt
                    SpawnHitEffectClientRpc(hit.point, hit.normal);
                }
            }
        }

        [ClientRpc]
        private void ConfirmHitClientRpc(ClientRpcParams clientRpcParams = default)
        {
            if (CombatHUD.Instance != null)
            {
                CombatHUD.Instance.ShowHitmarker();
            }
        }

        private void PlayShootEffects()
        {
            if (muzzleFlash != null)
            {
                muzzleFlash.Play();
            }

            PlayShootEffectsClientRpc();
        }

        [ClientRpc]
        private void PlayShootEffectsClientRpc(ClientRpcParams clientRpcParams = default)
        {
            // Eigenaar heeft het effect lokaal al direct afgespeeld
            if (IsOwner) return;

            if (muzzleFlash != null)
            {
                muzzleFlash.Play();
            }
        }

        [ClientRpc]
        private void SpawnHitEffectClientRpc(Vector3 hitPosition, Vector3 hitNormal)
        {
            if (hitEffectPrefab == null) return;

            GameObject effect = Instantiate(
                hitEffectPrefab,
                hitPosition,
                Quaternion.LookRotation(hitNormal)
            );

            Destroy(effect, 3f);
        }

        private void OnDrawGizmosSelected()
        {
            if (firePoint == null) return;

            Gizmos.color = Color.red;
            Gizmos.DrawRay(firePoint.position, firePoint.forward * range);
        }
    }
}
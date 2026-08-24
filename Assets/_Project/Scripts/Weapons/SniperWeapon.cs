using Unity.Netcode;
using UnityEngine;
using SniperGame.Player;
using SniperGame.UI;

namespace SniperGame.Weapons
{
    public class SniperWeapon : NetworkBehaviour
    {
        [Header("Weapon Settings")]
        [SerializeField] private int damage = 100;
        [SerializeField] private float range = 500f;
        [SerializeField] private float fireRate = 1.5f;

        [Header("Scope / ADS Settings")]
        [SerializeField] private float hipFOV = 60f;
        [SerializeField] private float scopedFOV = 18f;
        [SerializeField] private float zoomSpeed = 16f;

        [Header("References")]
        [SerializeField] private Camera playerCamera;
        [SerializeField] private Transform firePoint;

        [Header("Effects")]
        [SerializeField] private ParticleSystem muzzleFlash;
        [SerializeField] private GameObject hitEffectPrefab;

        [Header("Layers")]
        [SerializeField] private LayerMask hitMask = ~0;

        private float nextFireTime;
        private bool isAiming;

        private void Update()
        {
            // Alleen de eigenaar bestuurt dit wapen
            if (!IsOwner)
                return;

            HandleAiming();

            if (Input.GetMouseButtonDown(0))
            {
                TryShoot();
            }
        }

        private void HandleAiming()
        {
            // Rechtermuisknop ingedrukt houden om te richten
            isAiming = Input.GetMouseButton(1);

            // Pas FOV (inzoomen) van de camera aan
            if (playerCamera != null)
            {
                float targetFOV = isAiming ? scopedFOV : hipFOV;
                playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, Time.deltaTime * zoomSpeed);
            }

            // Schakel Scope Overlay en Hip Crosshair in/uit via CombatHUD
            if (CombatHUD.Instance != null)
            {
                CombatHUD.Instance.SetScopeActive(isAiming);
            }
        }

        private void TryShoot()
        {
            if (Time.time < nextFireTime)
                return;

            nextFireTime = Time.time + fireRate;

            Vector3 origin;
            Vector3 direction;

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

            PlayShootEffects();

            // Vraag de server om het schot te verwerken
            ShootServerRpc(origin, direction);
        }

        [ServerRpc]
        private void ShootServerRpc(
            Vector3 origin,
            Vector3 direction,
            ServerRpcParams serverRpcParams = default)
        {
            direction.Normalize();

            if (Physics.Raycast(
                origin,
                direction,
                out RaycastHit hit,
                range,
                hitMask,
                QueryTriggerInteraction.Ignore))
            {
                SniperGame.Player.PlayerHealth playerHealth =
                    hit.collider.GetComponentInParent<SniperGame.Player.PlayerHealth>();

                if (playerHealth != null)
                {
                    // Schiet jezelf niet neer
                    if (playerHealth.OwnerClientId == OwnerClientId)
                        return;

                    // Schade toepassen op server
                    playerHealth.TakeDamage(damage, OwnerClientId);

                    Debug.Log($"[Sniper] Player {OwnerClientId} hit Player {playerHealth.OwnerClientId} for {damage} damage.");

                    // Stuur hitmarker direct terug naar de schutter
                    ClientRpcParams clientRpcParams = new ClientRpcParams
                    {
                        Send = new ClientRpcSendParams
                        {
                            TargetClientIds = new ulong[] { OwnerClientId }
                        }
                    };
                    ConfirmHitClientRpc(clientRpcParams);

                    // Spawn hit effect voor iedereen
                    SpawnHitEffectClientRpc(hit.point, hit.normal);
                }
                else
                {
                    SpawnHitEffectClientRpc(hit.point, hit.normal);
                }
            }
        }

        [ClientRpc]
        private void ConfirmHitClientRpc(ClientRpcParams clientRpcParams = default)
        {
            // Laat het 'X' kruisje oplichten op het scherm van de schutter
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
            if (IsOwner)
                return;

            if (muzzleFlash != null)
            {
                muzzleFlash.Play();
            }
        }

        [ClientRpc]
        private void SpawnHitEffectClientRpc(Vector3 hitPosition, Vector3 hitNormal)
        {
            if (hitEffectPrefab == null)
                return;

            GameObject effect = Instantiate(
                hitEffectPrefab,
                hitPosition,
                Quaternion.LookRotation(hitNormal)
            );

            Destroy(effect, 3f);
        }

        private void OnDrawGizmosSelected()
        {
            if (firePoint == null)
                return;

            Gizmos.DrawRay(firePoint.position, firePoint.forward * range);
        }
    }
}
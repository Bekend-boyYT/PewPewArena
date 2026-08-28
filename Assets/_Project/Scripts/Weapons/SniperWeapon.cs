using System.Collections;
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
        [SerializeField] private float fireRate = 1.3f;

        [Header("Scope / ADS Settings")]
        [SerializeField] private float hipFOV = 60f;
        [SerializeField] private float scopedFOV = 18f;
        [SerializeField] private float zoomSpeed = 16f;
        [SerializeField] private float scopedSensitivityMultiplier = 0.35f;

        [Header("Recoil Kick Settings")]
        [SerializeField] private float hipRecoilPitch = 3.2f;
        [SerializeField] private float hipRecoilYaw = 1.0f;
        [SerializeField] private float scopedRecoilPitch = 1.4f;
        [SerializeField] private float scopedRecoilYaw = 0.4f;

        [Header("References")]
        [SerializeField] private Camera playerCamera;
        [SerializeField] private Transform firePoint;
        [SerializeField] private PlayerLook playerLook;

        [Header("Effects & Tracers")]
        [SerializeField] private ParticleSystem muzzleFlash;
        [SerializeField] private GameObject hitEffectPrefab;
        [SerializeField] private Material tracerMaterial;
        [SerializeField] private float tracerDuration = 0.08f;
        [SerializeField] private float tracerWidth = 0.04f;

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
            if (!IsOwner) return;

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
            isAiming = Input.GetMouseButton(1);

            if (playerCamera != null)
            {
                float targetFOV = isAiming ? scopedFOV : hipFOV;
                playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, Time.deltaTime * zoomSpeed);
            }

            if (playerLook != null)
            {
                playerLook.SetSensitivityMultiplier(isAiming ? scopedSensitivityMultiplier : 1.0f);
            }

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

            // Pas recoil kick toe op de camera van de schutter
            if (playerLook != null)
            {
                float pitch = isAiming ? scopedRecoilPitch : hipRecoilPitch;
                float yaw = Random.Range(-1f, 1f) * (isAiming ? scopedRecoilYaw : hipRecoilYaw);
                playerLook.AddRecoil(pitch, yaw);
            }

            PlayShootEffects();

            // Voer Server-Side Raycast uit
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

            Vector3 hitPoint = origin + direction * range;
            Vector3 hitNormal = -direction;

            if (Physics.Raycast(origin, direction, out RaycastHit hit, range, hitMask, QueryTriggerInteraction.Ignore))
            {
                hitPoint = hit.point;
                hitNormal = hit.normal;

                PlayerHealth targetHealth = hit.collider.GetComponentInParent<PlayerHealth>();

                if (targetHealth != null && targetHealth.OwnerClientId != shooterClientId)
                {
                    targetHealth.TakeDamage(damage, shooterClientId);

                    ClientRpcParams clientRpcParams = new ClientRpcParams
                    {
                        Send = new ClientRpcSendParams
                        {
                            TargetClientIds = new ulong[] { shooterClientId }
                        }
                    };
                    ConfirmHitClientRpc(clientRpcParams);
                }
            }

            // Spawn Tracer en Impact effect op alle clients
            Vector3 spawnOrigin = (firePoint != null) ? firePoint.position : origin;
            SpawnShotVisualsClientRpc(spawnOrigin, hitPoint, hitNormal);
        }

        [ClientRpc]
        private void ConfirmHitClientRpc(ClientRpcParams clientRpcParams = default)
        {
            if (CombatHUD.Instance != null)
            {
                CombatHUD.Instance.ShowHitmarker();
            }
        }

        [ClientRpc]
        private void SpawnShotVisualsClientRpc(Vector3 origin, Vector3 hitPosition, Vector3 hitNormal)
        {
            // Start kogelspoor (Tracer)
            StartCoroutine(DrawTracerRoutine(origin, hitPosition));

            // Spawn impact FX
            if (hitEffectPrefab != null)
            {
                GameObject effect = Instantiate(hitEffectPrefab, hitPosition, Quaternion.LookRotation(hitNormal));
                Destroy(effect, 3f);
            }
        }

        private IEnumerator DrawTracerRoutine(Vector3 start, Vector3 end)
        {
            GameObject tracerObj = new GameObject("BulletTracer");
            LineRenderer lr = tracerObj.AddComponent<LineRenderer>();

            lr.startWidth = tracerWidth;
            lr.endWidth = tracerWidth * 0.5f;
            lr.positionCount = 2;
            lr.SetPosition(0, start);
            lr.SetPosition(1, end);

            // Gebruik het toegewezen materiaal of een standaard wit/geel sprite materiaal
            if (tracerMaterial != null)
            {
                lr.material = tracerMaterial;
            }
            else
            {
                lr.material = new Material(Shader.Find("Sprites/Default"));
                lr.startColor = new Color(1f, 0.9f, 0.3f, 0.9f); // Goudgeel spoor
                lr.endColor = new Color(1f, 1f, 1f, 0.4f);
            }

            yield return new WaitForSeconds(tracerDuration);

            Destroy(tracerObj);
        }

        private void PlayShootEffects()
        {
            if (muzzleFlash != null) muzzleFlash.Play();
            PlayShootEffectsClientRpc();
        }

        [ClientRpc]
        private void PlayShootEffectsClientRpc(ClientRpcParams clientRpcParams = default)
        {
            if (IsOwner) return;
            if (muzzleFlash != null) muzzleFlash.Play();
        }

        private void OnDrawGizmosSelected()
        {
            if (firePoint == null) return;
            Gizmos.color = Color.red;
            Gizmos.DrawRay(firePoint.position, firePoint.forward * range);
        }
    }
}
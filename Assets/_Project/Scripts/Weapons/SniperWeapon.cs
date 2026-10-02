using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using SniperGame.Player;
using SniperGame.UI;
using SniperGame.Gameplay;
using SniperGame.Audio;

namespace SniperGame.Weapons
{
    public class SniperWeapon : NetworkBehaviour
    {
        [Header("Weapon Settings")]
        [SerializeField] private float range = 500f;
        [SerializeField] private float fireRate = 1.3f;

        [Header("Ammo Settings")]
        [SerializeField] private int maxClipAmmo = 5;
        [SerializeField] private float reloadDuration = 2.2f;

        [Header("Audio Settings (3D Spatial)")]
        [SerializeField] private AudioSource weaponAudioSource;
        [SerializeField] private AudioClip gunshotClip;
        [SerializeField] private AudioClip reloadClip;

        [Header("Scope / ADS Settings")]
        [SerializeField] private float hipFOV = 80f;
        [SerializeField] private float scopedFOV = 18f;
        [SerializeField] private float zoomSpeed = 22f;
        [SerializeField] private float scopedSensitivityMultiplier = 0.35f;

        [Header("Weapon Model & Scope Animation")]
        [SerializeField] private Animator weaponAnimator;
        [Tooltip("The GameObject holding the sniper mesh/renderers (e.g. WeaponHolder or the Sniper Model)")]
        [SerializeField] private GameObject weaponVisuals;
        [Tooltip("Time in seconds for the ScopedIn animation to complete")]
        [SerializeField] private float scopeInDuration = 0.20f;
        [Tooltip("How many seconds before the animation completely finishes the overlay should appear")]
        [SerializeField] private float scopeOverlayLeadTime = 0.05f;
        [SerializeField] private string isScopedParam = "IsScoped";

        [Header("Recoil Kick Settings")]
        [SerializeField] private float hipRecoilPitch = 3.2f;
        [SerializeField] private float hipRecoilYaw = 1.0f;
        [SerializeField] private float scopedRecoilPitch = 1.4f;
        [SerializeField] private float scopedRecoilYaw = 0.4f;

        [Header("References")]
        [SerializeField] private Camera playerCamera;
        [SerializeField] private Transform firePoint;
        [SerializeField] private PlayerMovement playerMovement;

        [Header("Effects, Bullets & Tracers")]
        [Tooltip("Your 3D bullet prefab that flies from the barrel")]
        [SerializeField] private GameObject bulletPrefab;
        [SerializeField] private float bulletSpeed = 450f;
        [SerializeField] private ParticleSystem muzzleFlash;
        [SerializeField] private GameObject hitEffectPrefab;
        [SerializeField] private Material tracerMaterial;
        [SerializeField] private float tracerDuration = 0.08f;
        [SerializeField] private float tracerWidth = 0.04f;

        [Header("Layers")]
        [SerializeField] private LayerMask hitMask = ~0;

        private int _currentAmmo;
        private bool _isReloading;
        private float _reloadStartTime;
        private Coroutine _reloadCoroutine;
        private Coroutine _scopeCoroutine;

        private float nextFireTime;
        private bool _wantsToScope;
        private bool _isFullyScoped;
        private Renderer[] _weaponRenderers;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (!IsOwner)
            {
                enabled = false;
                return;
            }

            ResetAmmo();
        }

        private void Awake()
        {
            CacheWeaponRenderers();
        }

        private void Start()
        {
            if (IsOwner)
            {
                UpdateAmmoDisplay();
                SetWeaponVisualsVisible(true);
            }
        }

        private void CacheWeaponRenderers()
        {
            if (weaponVisuals != null)
            {
                _weaponRenderers = weaponVisuals.GetComponentsInChildren<Renderer>(true);
            }
        }

        private void Update()
        {
            if (!IsOwner) return;

            if (PauseMenu.IsPaused || (RoundManager.Instance != null && !RoundManager.Instance.CanPlayersFight()))
            {
                ResetAimingState();
                return;
            }

            bool reloadPressed = (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) || UI.Settings.KeybindManager.GetKeyDown(UI.Settings.KeyAction.Reload);
            if (reloadPressed && !_isReloading && _currentAmmo < maxClipAmmo)
            {
                StartReload();
            }

            if (!_isReloading)
            {
                HandleAimingInput();
            }
            else
            {
                ResetAimingState();
            }

            UpdateCameraZoom();
            UpdateCooldownIndicator();

            bool shootPressed = Input.GetMouseButtonDown(0) || UI.Settings.KeybindManager.GetKeyDown(UI.Settings.KeyAction.Shoot);
            if (shootPressed)
            {
                TryShoot();
            }
        }

        private void UpdateCooldownIndicator()
        {
            if (CombatHUD.Instance == null) return;

            if (_isReloading)
            {
                float reloadProgress = Mathf.Clamp01((Time.time - _reloadStartTime) / reloadDuration);
                CombatHUD.Instance.UpdateCooldownBar(reloadProgress, true);
            }
            else if (Time.time < nextFireTime)
            {
                float shootProgress = 1f - Mathf.Clamp01((nextFireTime - Time.time) / fireRate);
                CombatHUD.Instance.UpdateCooldownBar(shootProgress, false);
            }
            else
            {
                CombatHUD.Instance.UpdateCooldownBar(1f, false);
            }
        }

        private void HandleAimingInput()
        {
            bool rmbPressed = Input.GetMouseButton(1) || UI.Settings.KeybindManager.GetKey(UI.Settings.KeyAction.Aim);

            if (rmbPressed && !_wantsToScope)
            {
                _wantsToScope = true;

                if (_scopeCoroutine != null) StopCoroutine(_scopeCoroutine);
                _scopeCoroutine = StartCoroutine(ScopeInRoutine());
            }
            else if (!rmbPressed && _wantsToScope)
            {
                ScopeOut();
            }
        }

        private IEnumerator ScopeInRoutine()
        {
            // 1. Immediately start raising the 3D sniper weapon
            if (weaponAnimator != null)
            {
                weaponAnimator.SetBool(isScopedParam, true);
            }

            // 2. Ensure weapon visuals are visible while raising
            SetWeaponVisualsVisible(true);

            // 3. Immediately start camera FOV zoom and sensitivity scaling
            if (playerMovement != null)
            {
                playerMovement.SetScopeState(true, scopedFOV, scopedSensitivityMultiplier);
            }

            // 4. Ensure overlay is hidden while weapon is raising
            if (CombatHUD.Instance != null)
            {
                CombatHUD.Instance.SetScopeActive(false);
            }

            // 5. Wait until exactly 0.05s before the scoped-in animation finishes
            float overlayDelay = Mathf.Max(0.01f, scopeInDuration - scopeOverlayLeadTime);
            yield return new WaitForSeconds(overlayDelay);

            if (!_wantsToScope) yield break;

            // 6. Overlay appears 0.05s before the sniper scoped-in animation is done
            if (CombatHUD.Instance != null)
            {
                CombatHUD.Instance.SetScopeActive(true);
            }

            // 7. Wait the remaining 0.05s for the 3D animation to complete
            yield return new WaitForSeconds(scopeOverlayLeadTime);

            if (!_wantsToScope) yield break;

            // 8. Fully scoped: cleanly hide 3D model so it doesn't clip through the overlay reticle
            _isFullyScoped = true;
            SetWeaponVisualsVisible(false);
        }

        private void ScopeOut()
        {
            _wantsToScope = false;
            _isFullyScoped = false;

            if (_scopeCoroutine != null)
            {
                StopCoroutine(_scopeCoroutine);
                _scopeCoroutine = null;
            }

            // 1. Instantly restore 3D weapon mesh so it lowers cleanly into hip position
            SetWeaponVisualsVisible(true);

            // 2. Immediately lower the weapon
            if (weaponAnimator != null)
            {
                weaponAnimator.SetBool(isScopedParam, false);
            }

            // 3. Immediately reverse camera FOV zoom and restore sensitivity
            if (playerMovement != null)
            {
                playerMovement.SetScopeState(false, scopedFOV, 1.0f);
            }

            // 4. Immediately fade out the 2D scope overlay
            if (CombatHUD.Instance != null)
            {
                CombatHUD.Instance.SetScopeActive(false);
            }
        }

        private void UpdateCameraZoom()
        {
            if (playerCamera == null || playerMovement != null) return;

            float targetFOV = _isFullyScoped ? scopedFOV : hipFOV;
            playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, Time.deltaTime * zoomSpeed);
        }

        private void ResetAimingState()
        {
            ScopeOut();
        }

        private void SetWeaponVisualsVisible(bool visible)
        {
            if (_weaponRenderers == null || _weaponRenderers.Length == 0)
            {
                CacheWeaponRenderers();
            }

            if (_weaponRenderers != null)
            {
                for (int i = 0; i < _weaponRenderers.Length; i++)
                {
                    if (_weaponRenderers[i] != null)
                    {
                        _weaponRenderers[i].enabled = visible;
                    }
                }
            }
        }

        private void TryShoot()
        {
            if (_isReloading || Time.time < nextFireTime) return;

            if (_currentAmmo <= 0)
            {
                StartReload();
                return;
            }

            _currentAmmo--;
            UpdateAmmoDisplay();

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

            if (playerMovement != null)
            {
                float pitch = (_wantsToScope || _isFullyScoped) ? scopedRecoilPitch : hipRecoilPitch;
                float yaw = Random.Range(-1f, 1f) * ((_wantsToScope || _isFullyScoped) ? scopedRecoilYaw : hipRecoilYaw);
                playerMovement.AddRecoil(pitch, yaw);
            }

            PlayShootEffects();

            ShootServerRpc(origin, direction, OwnerClientId);
        }

        public void StartReload()
        {
            if (_isReloading || _currentAmmo >= maxClipAmmo) return;

            if (_reloadCoroutine != null) StopCoroutine(_reloadCoroutine);
            _reloadCoroutine = StartCoroutine(ReloadRoutine());
        }

        private IEnumerator ReloadRoutine()
        {
            _isReloading = true;
            _reloadStartTime = Time.time;
            ResetAimingState();
            UpdateAmmoDisplay();

            PlayReloadSoundServerRpc();

            yield return new WaitForSeconds(reloadDuration);

            _currentAmmo = maxClipAmmo;
            _isReloading = false;
            UpdateAmmoDisplay();
        }

        [ServerRpc]
        private void PlayReloadSoundServerRpc()
        {
            PlayReloadSoundClientRpc();
        }

        [ClientRpc]
        private void PlayReloadSoundClientRpc()
        {
            if (weaponAudioSource != null && reloadClip != null)
            {
                weaponAudioSource.PlayOneShot(reloadClip, 0.9f);
            }
        }

        public void ResetAmmo()
        {
            if (_reloadCoroutine != null) StopCoroutine(_reloadCoroutine);
            _isReloading = false;
            _currentAmmo = maxClipAmmo;
            UpdateAmmoDisplay();
        }

        private void UpdateAmmoDisplay()
        {
            if (CombatHUD.Instance != null)
            {
                CombatHUD.Instance.UpdateAmmo(_currentAmmo, maxClipAmmo, _isReloading);
            }
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

            RaycastHit[] hits = Physics.RaycastAll(origin, direction, range, hitMask, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (var hit in hits)
            {
                PlayerHitbox hitbox = hit.collider.GetComponent<PlayerHitbox>();
                PlayerHealth targetHealth = hitbox != null ? hitbox.Health : hit.collider.GetComponentInParent<PlayerHealth>();

                if (targetHealth != null && targetHealth.OwnerClientId == shooterClientId)
                {
                    continue;
                }

                hitPoint = hit.point;
                hitNormal = hit.normal;

                if (targetHealth != null && targetHealth.OwnerClientId != shooterClientId)
                {
                    HitboxType hitType = (hitbox != null) ? hitbox.Type : HitboxType.Body;
                    int appliedDamage = (hitbox != null) ? hitbox.GetDamage() : 100;

                    targetHealth.TakeDamage(appliedDamage, shooterClientId);

                    ClientRpcParams clientRpcParams = new ClientRpcParams
                    {
                        Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { shooterClientId } }
                    };
                    ConfirmHitClientRpc(hitType, appliedDamage, hitPoint, clientRpcParams);
                }

                break;
            }

            Vector3 spawnOrigin = (firePoint != null) ? firePoint.position : origin;
            SpawnShotVisualsClientRpc(spawnOrigin, hitPoint, hitNormal);
        }

        [ClientRpc]
        private void ConfirmHitClientRpc(HitboxType hitboxType, int damage, Vector3 hitPosition, ClientRpcParams clientRpcParams = default)
        {
            if (UI.Settings.SettingsManager.ShowHitmarkers)
            {
                if (CombatHUD.Instance != null)
                {
                    CombatHUD.Instance.ShowHitmarker(hitboxType);
                }

                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayHitmarker(hitboxType == HitboxType.Head);
                }
            }

            if (UI.Settings.SettingsManager.ShowDamageNumbers)
            {
                if (UI.DamageNumbers.DamageNumberManager.Instance == null)
                {
                    var dnmGo = new GameObject("DamageNumberManager");
                    dnmGo.AddComponent<UI.DamageNumbers.DamageNumberManager>();
                }
                UI.DamageNumbers.DamageNumberManager.Instance.SpawnDamageNumber(damage, hitPosition, hitboxType == HitboxType.Head);
            }
        }

        [ClientRpc]
        private void SpawnShotVisualsClientRpc(Vector3 origin, Vector3 hitPosition, Vector3 hitNormal)
        {
            if (bulletPrefab != null)
            {
                GameObject bullet = Instantiate(bulletPrefab, origin, Quaternion.identity);
                var cosmetic = bullet.GetComponent<CosmeticBullet>();
                if (cosmetic == null)
                {
                    cosmetic = bullet.AddComponent<CosmeticBullet>();
                }
                cosmetic.Initialize(hitPosition, hitNormal, hitEffectPrefab, bulletSpeed);
            }
            else
            {
                StartCoroutine(DrawTracerRoutine(origin, hitPosition));

                if (hitEffectPrefab != null)
                {
                    GameObject effect = Instantiate(hitEffectPrefab, hitPosition, Quaternion.LookRotation(hitNormal));
                    Destroy(effect, 3f);
                }
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

            if (tracerMaterial != null)
            {
                lr.material = tracerMaterial;
            }
            else
            {
                lr.material = new Material(Shader.Find("Sprites/Default"));
                lr.startColor = new Color(1f, 0.9f, 0.3f, 0.9f);
                lr.endColor = new Color(1f, 1f, 1f, 0.4f);
            }

            yield return new WaitForSeconds(tracerDuration);

            Destroy(tracerObj);
        }

        private void PlayShootEffects()
        {
            if (muzzleFlash != null) muzzleFlash.Play();
            if (weaponAudioSource != null && gunshotClip != null)
            {
                weaponAudioSource.pitch = Random.Range(0.96f, 1.04f);
                weaponAudioSource.PlayOneShot(gunshotClip, 1.0f);
            }

            if (IsOwner && CombatHUD.Instance != null)
            {
                CombatHUD.Instance.TriggerScopeRecoil(1.0f);
            }

            PlayShootEffectsClientRpc();
        }

        [ClientRpc]
        private void PlayShootEffectsClientRpc(ClientRpcParams clientRpcParams = default)
        {
            if (IsOwner) return;

            if (muzzleFlash != null) muzzleFlash.Play();
            if (weaponAudioSource != null && gunshotClip != null)
            {
                weaponAudioSource.pitch = Random.Range(0.96f, 1.04f);
                weaponAudioSource.PlayOneShot(gunshotClip, 1.0f);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (firePoint == null) return;
            Gizmos.color = Color.red;
            Gizmos.DrawRay(firePoint.position, firePoint.forward * range);
        }
    }
}
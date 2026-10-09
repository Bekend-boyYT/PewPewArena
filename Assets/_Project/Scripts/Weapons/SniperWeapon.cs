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

        [Header("Sniper 1 Setup")]
        [SerializeField] private GameObject weaponHolder1;
        [SerializeField] private Animator weaponAnimator1;
        [SerializeField] private Transform firePoint1;
        [SerializeField] private int maxClipAmmo1 = 5;

        [Header("Sniper 2 Setup")]
        [SerializeField] private GameObject weaponHolder2;
        [SerializeField] private Animator weaponAnimator2;
        [SerializeField] private Transform firePoint2;
        [SerializeField] private int maxClipAmmo2 = 3;
        [SerializeField] private float hipRecoilPitch2 = 0.95f;
        [SerializeField] private float hipRecoilYaw2 = 0.65f;
        [SerializeField] private float scopedRecoilPitch2 = 0.80f;
        [SerializeField] private float scopedRecoilYaw2 = 0.45f;
        [SerializeField] private int sniper2Damage = 999;

        [Header("Ammo Settings (Legacy Fallback)")]
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

        [Header("Recoil Kick Settings (Sniper 1)")]
        [SerializeField] private float hipRecoilPitch = 3.2f;
        [SerializeField] private float hipRecoilYaw = 1.0f;
        [SerializeField] private float scopedRecoilPitch = 1.4f;
        [SerializeField] private float scopedRecoilYaw = 0.4f;

        [Header("References")]
        [SerializeField] private Camera playerCamera;
        [SerializeField] private Transform firePoint;
        [SerializeField] private PlayerMovement playerMovement;

        [Header("Effects, Bullets & Tracers (Fallback)")]
        [Tooltip("Legacy fallback bullet prefab")]
        [SerializeField] private GameObject bulletPrefab;
        [SerializeField] private float bulletSpeed = 450f;
        [SerializeField] private ParticleSystem muzzleFlash;
        [SerializeField] private GameObject hitEffectPrefab;
        [SerializeField] private Material tracerMaterial;
        [SerializeField] private float tracerDuration = 0.08f;
        [SerializeField] private float tracerWidth = 0.04f;

        [Header("Sniper 1 VFX")]
        [SerializeField] private GameObject muzzleFlashPrefab1;
        [SerializeField] private GameObject bulletPrefab1;
        [SerializeField] private GameObject hitEffectPrefab1;
        [SerializeField] private float bulletSpeed1 = 500f;

        [Header("Sniper 2 VFX")]
        [SerializeField] private GameObject muzzleFlashPrefab2;
        [SerializeField] private GameObject bulletPrefab2;
        [SerializeField] private GameObject hitEffectPrefab2;
        [SerializeField] private float bulletSpeed2 = 550f;

        [Header("Layers")]
        [SerializeField] private LayerMask hitMask = ~0;

        [Header("Synchronized Weapon State")]
        public NetworkVariable<int> ActiveWeaponIndex = new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner
        );

        private int _activeWeaponSlot = 0;
        private int _ammoSlot1 = 5;
        private int _ammoSlot2 = 3;

        private int CurrentClipAmmo
        {
            get => (_activeWeaponSlot == 0) ? _ammoSlot1 : _ammoSlot2;
            set
            {
                if (_activeWeaponSlot == 0) _ammoSlot1 = value;
                else _ammoSlot2 = value;
            }
        }

        private int CurrentMaxClipAmmo => (_activeWeaponSlot == 0) ? maxClipAmmo1 : maxClipAmmo2;

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

            ResolveWeaponReferences();

            ActiveWeaponIndex.OnValueChanged += (prev, current) =>
            {
                ApplyWeaponVisuals(current);
            };

            if (RoundManager.Instance != null)
            {
                RoundManager.Instance.IsSniper2Allowed.OnValueChanged += HandleSniper2AllowedChanged;
            }

            if (!IsOwner)
            {
                enabled = false;
                ApplyWeaponVisuals(ActiveWeaponIndex.Value);
                return;
            }

            int startingSlot = 0;
            if (RoundManager.Instance != null && !RoundManager.Instance.IsSniper2Allowed.Value)
            {
                startingSlot = RoundManager.Instance.HostSelectedSniper.Value;
            }
            else
            {
                startingSlot = PlayerPrefs.GetInt("SelectedLoadout", 0);
            }
            if (startingSlot != 0 && startingSlot != 1) startingSlot = 0;

            ActiveWeaponIndex.Value = startingSlot;
            EquipWeapon(startingSlot);
            ResetAmmo();
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            if (RoundManager.Instance != null)
            {
                RoundManager.Instance.IsSniper2Allowed.OnValueChanged -= HandleSniper2AllowedChanged;
            }
        }

        private void HandleSniper2AllowedChanged(bool previous, bool current)
        {
            if (!IsOwner) return;

            if (!current && RoundManager.Instance != null)
            {
                int forcedSlot = RoundManager.Instance.HostSelectedSniper.Value;
                if (ActiveWeaponIndex.Value != forcedSlot)
                {
                    SwitchWeapon(forcedSlot);
                }
            }
        }

        private void Awake()
        {
            ResolveWeaponReferences();

            int preferredSlot = PlayerPrefs.GetInt("SelectedLoadout", 0);
            if (preferredSlot != 0 && preferredSlot != 1) preferredSlot = 0;

            SetWeaponSlotActive(preferredSlot);
        }

        private void Start()
        {
            if (IsOwner)
            {
                if (RoundManager.Instance != null && !RoundManager.Instance.IsSniper2Allowed.Value)
                {
                    int forcedSlot = RoundManager.Instance.HostSelectedSniper.Value;
                    if (ActiveWeaponIndex.Value != forcedSlot)
                    {
                        SwitchWeapon(forcedSlot);
                    }
                    else
                    {
                        SetWeaponSlotActive(forcedSlot);
                    }
                }
                else
                {
                    SetWeaponSlotActive(_activeWeaponSlot);
                }

                UpdateAmmoDisplay();
                SetWeaponVisualsVisible(true);
            }
            else
            {
                SetWeaponSlotActive(ActiveWeaponIndex.Value);
            }
        }

        private void ResolveWeaponReferences()
        {
            if (maxClipAmmo != 5 && maxClipAmmo1 == 5) maxClipAmmo1 = maxClipAmmo;

            Transform[] allTransforms = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < allTransforms.Length; i++)
            {
                Transform t = allTransforms[i];
                if (t == null) continue;

                if (t.name == "WeaponHolder" && weaponHolder1 == null)
                {
                    weaponHolder1 = t.gameObject;
                }
                else if (t.name == "WeaponHolder 2" && weaponHolder2 == null)
                {
                    weaponHolder2 = t.gameObject;
                }
            }

            if (weaponHolder1 == null && weaponVisuals != null)
            {
                weaponHolder1 = weaponVisuals;
            }

            if (weaponAnimator1 == null && weaponHolder1 != null)
            {
                weaponAnimator1 = weaponHolder1.GetComponent<Animator>();
            }
            if (weaponAnimator1 == null && weaponAnimator != null)
            {
                weaponAnimator1 = weaponAnimator;
            }

            if (weaponAnimator2 == null && weaponHolder2 != null)
            {
                weaponAnimator2 = weaponHolder2.GetComponent<Animator>();
            }

            if (firePoint1 == null && weaponHolder1 != null)
            {
                firePoint1 = weaponHolder1.transform.Find("FirePoint");
            }
            if (firePoint1 == null && firePoint != null)
            {
                firePoint1 = firePoint;
            }

            if (firePoint2 == null && weaponHolder2 != null)
            {
                firePoint2 = weaponHolder2.transform.Find("FirePoint");
            }

            if (weaponVisuals == null) weaponVisuals = weaponHolder1;
            if (weaponAnimator == null) weaponAnimator = weaponAnimator1;
            if (firePoint == null) firePoint = firePoint1;
        }

        public void SwitchWeapon(int newSlot)
        {
            if (newSlot < 0 || newSlot > 1) return;
            if (newSlot == ActiveWeaponIndex.Value) return;

            if (_isReloading)
            {
                if (_reloadCoroutine != null) StopCoroutine(_reloadCoroutine);
                _isReloading = false;
            }

            ResetAimingState();

            ActiveWeaponIndex.Value = newSlot;
            EquipWeapon(newSlot);
        }

        private void EquipWeapon(int slot)
        {
            _activeWeaponSlot = slot;

            SetWeaponSlotActive(slot);

            if (IsOwner)
            {
                UpdateAmmoDisplay();
                if (CombatHUD.Instance != null)
                {
                    CombatHUD.Instance.UpdateCooldownBar(1f, false);
                }
            }
        }

        public void SetWeaponSlotActive(int slot)
        {
            _activeWeaponSlot = slot;

            if (weaponHolder1 == null || weaponHolder2 == null)
            {
                ResolveWeaponReferences();
            }

            // Sniper 1 (Slot 0)
            if (weaponHolder1 != null)
            {
                bool isSlot0 = (slot == 0);
                weaponHolder1.SetActive(isSlot0);

                if (isSlot0)
                {
                    Transform[] transforms1 = weaponHolder1.GetComponentsInChildren<Transform>(true);
                    for (int i = 0; i < transforms1.Length; i++)
                    {
                        if (transforms1[i] != null) transforms1[i].gameObject.SetActive(true);
                    }
                }

                Renderer[] renderers1 = weaponHolder1.GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < renderers1.Length; i++)
                {
                    if (renderers1[i] != null)
                    {
                        renderers1[i].enabled = isSlot0 && !_isFullyScoped;
                    }
                }
            }

            // Sniper 2 (Slot 1)
            if (weaponHolder2 != null)
            {
                bool isSlot1 = (slot == 1);
                weaponHolder2.SetActive(isSlot1);

                if (isSlot1)
                {
                    Transform[] transforms2 = weaponHolder2.GetComponentsInChildren<Transform>(true);
                    for (int i = 0; i < transforms2.Length; i++)
                    {
                        if (transforms2[i] != null) transforms2[i].gameObject.SetActive(true);
                    }
                }

                Renderer[] renderers2 = weaponHolder2.GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < renderers2.Length; i++)
                {
                    if (renderers2[i] != null)
                    {
                        renderers2[i].enabled = isSlot1 && !_isFullyScoped;
                    }
                }
            }

            weaponVisuals = (slot == 0) ? weaponHolder1 : weaponHolder2;
            weaponAnimator = (slot == 0) ? weaponAnimator1 : weaponAnimator2;
            firePoint = (slot == 0) ? firePoint1 : firePoint2;

            CacheWeaponRenderers();
        }

        private void ApplyWeaponVisuals(int slot)
        {
            SetWeaponSlotActive(slot);
        }

        private void CacheWeaponRenderers()
        {
            GameObject activeObj = (_activeWeaponSlot == 0) ? weaponHolder1 : weaponHolder2;
            if (activeObj == null) activeObj = weaponVisuals;

            if (activeObj != null)
            {
                _weaponRenderers = activeObj.GetComponentsInChildren<Renderer>(true);
            }
        }

        public bool CanSwitchWeapons()
        {
            if (RoundManager.Instance != null && !RoundManager.Instance.IsSniper2Allowed.Value)
            {
                return false;
            }
            return true;
        }

        private void Update()
        {
            if (!IsOwner) return;

            if (PauseMenu.IsPaused || (RoundManager.Instance != null && !RoundManager.Instance.CanPlayersFight()))
            {
                ResetAimingState();
                return;
            }

            if (CanSwitchWeapons())
            {
                int targetSlot = -1;
                bool key1Pressed = (Keyboard.current != null && Keyboard.current.digit1Key.wasPressedThisFrame) ||
                                   Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1);
                bool key2Pressed = (Keyboard.current != null && Keyboard.current.digit2Key.wasPressedThisFrame) ||
                                   Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2);

                if (key1Pressed) targetSlot = 0;
                else if (key2Pressed) targetSlot = 1;
                else
                {
                    float scroll = Input.mouseScrollDelta.y;
                    if (Mathf.Abs(scroll) > 0.05f)
                    {
                        targetSlot = (ActiveWeaponIndex.Value == 0) ? 1 : 0;
                    }
                }

                if (targetSlot != -1 && targetSlot != ActiveWeaponIndex.Value)
                {
                    SwitchWeapon(targetSlot);
                }
            }

            bool reloadPressed = (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) || UI.Settings.KeybindManager.GetKeyDown(UI.Settings.KeyAction.Reload);
            if (reloadPressed && !_isReloading && CurrentClipAmmo < CurrentMaxClipAmmo)
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
            GameObject activeObj = (_activeWeaponSlot == 0) ? weaponHolder1 : weaponHolder2;
            if (activeObj == null) activeObj = weaponVisuals;

            if (activeObj != null)
            {
                Renderer[] activeRenderers = activeObj.GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < activeRenderers.Length; i++)
                {
                    if (activeRenderers[i] != null)
                    {
                        activeRenderers[i].enabled = visible;
                    }
                }
            }

            GameObject inactiveObj = (_activeWeaponSlot == 0) ? weaponHolder2 : weaponHolder1;
            if (inactiveObj != null)
            {
                Renderer[] inactiveRenderers = inactiveObj.GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < inactiveRenderers.Length; i++)
                {
                    if (inactiveRenderers[i] != null)
                    {
                        inactiveRenderers[i].enabled = false;
                    }
                }
            }
        }

        private void TryShoot()
        {
            if (_isReloading || Time.time < nextFireTime) return;

            if (CurrentClipAmmo <= 0)
            {
                StartReload();
                return;
            }

            CurrentClipAmmo--;
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
                float activeHipPitch = (_activeWeaponSlot == 0) ? hipRecoilPitch : hipRecoilPitch2;
                float activeHipYaw = (_activeWeaponSlot == 0) ? hipRecoilYaw : hipRecoilYaw2;
                float activeScopedPitch = (_activeWeaponSlot == 0) ? scopedRecoilPitch : scopedRecoilPitch2;
                float activeScopedYaw = (_activeWeaponSlot == 0) ? scopedRecoilYaw : scopedRecoilYaw2;

                float pitch = (_wantsToScope || _isFullyScoped) ? activeScopedPitch : activeHipPitch;
                float yaw = Random.Range(-1f, 1f) * ((_wantsToScope || _isFullyScoped) ? activeScopedYaw : activeHipYaw);
                playerMovement.AddRecoil(pitch, yaw);
            }

            PlayShootEffects(_activeWeaponSlot);

            ShootServerRpc(origin, direction, OwnerClientId, _activeWeaponSlot);
        }

        public void StartReload()
        {
            if (_isReloading || CurrentClipAmmo >= CurrentMaxClipAmmo) return;

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

            CurrentClipAmmo = CurrentMaxClipAmmo;
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

        [ClientRpc]
        public void ResetAmmoClientRpc()
        {
            ResetAmmoLocal();
        }

        public void ResetAmmo()
        {
            if (IsServer && !IsOwner)
            {
                ResetAmmoClientRpc();
                return;
            }

            ResetAmmoLocal();
        }

        private void ResetAmmoLocal()
        {
            if (_reloadCoroutine != null) StopCoroutine(_reloadCoroutine);
            _isReloading = false;
            _ammoSlot1 = maxClipAmmo1;
            _ammoSlot2 = maxClipAmmo2;

            if (IsOwner)
            {
                UpdateAmmoDisplay();
                if (CombatHUD.Instance != null)
                {
                    CombatHUD.Instance.UpdateCooldownBar(1f, false);
                }
            }
        }

        private void UpdateAmmoDisplay()
        {
            if (CombatHUD.Instance != null)
            {
                CombatHUD.Instance.UpdateAmmo(CurrentClipAmmo, CurrentMaxClipAmmo, _isReloading);
            }
        }

        [ServerRpc]
        private void ShootServerRpc(
            Vector3 origin,
            Vector3 direction,
            ulong shooterClientId,
            int weaponSlot,
            ServerRpcParams serverRpcParams = default)
        {
            direction.Normalize();

            Vector3 hitPoint = origin + direction * range;
            Vector3 hitNormal = -direction;
            bool isCharacterHit = false;

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
                    isCharacterHit = true;
                    HitboxType hitType = (hitbox != null) ? hitbox.Type : HitboxType.Body;
                    int appliedDamage;
                    bool showHitmarker;

                    if (weaponSlot == 1) // Sniper 2: One-shot damage, no hitmarkers
                    {
                        appliedDamage = sniper2Damage;
                        showHitmarker = false;
                    }
                    else // Sniper 1: Regular hitbox damage, show hitmarkers
                    {
                        appliedDamage = (hitbox != null) ? hitbox.GetDamage() : 100;
                        showHitmarker = true;
                    }

                    targetHealth.TakeDamage(appliedDamage, shooterClientId);

                    ClientRpcParams clientRpcParams = new ClientRpcParams
                    {
                        Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { shooterClientId } }
                    };
                    ConfirmHitClientRpc(hitType, appliedDamage, hitPoint, showHitmarker, clientRpcParams);
                }

                break;
            }

            Transform activeMuzzle = (weaponSlot == 0) ? firePoint1 : firePoint2;
            Vector3 spawnOrigin = (activeMuzzle != null) ? activeMuzzle.position : ((firePoint != null) ? firePoint.position : origin);
            SpawnShotVisualsClientRpc(spawnOrigin, hitPoint, hitNormal, weaponSlot, isCharacterHit);
        }

        [ClientRpc]
        private void ConfirmHitClientRpc(HitboxType hitboxType, int damage, Vector3 hitPosition, bool showHitmarker, ClientRpcParams clientRpcParams = default)
        {
            if (showHitmarker && UI.Settings.SettingsManager.ShowHitmarkers)
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
        private void SpawnShotVisualsClientRpc(Vector3 origin, Vector3 hitPosition, Vector3 hitNormal, int weaponSlot, bool isCharacterHit)
        {
            GameObject bPrefab = (weaponSlot == 0)
                ? (bulletPrefab1 != null ? bulletPrefab1 : bulletPrefab)
                : (bulletPrefab2 != null ? bulletPrefab2 : bulletPrefab);

            GameObject hPrefab = (weaponSlot == 0)
                ? (hitEffectPrefab1 != null ? hitEffectPrefab1 : hitEffectPrefab)
                : (hitEffectPrefab2 != null ? hitEffectPrefab2 : hitEffectPrefab);

            float speed = (weaponSlot == 0)
                ? (bulletSpeed1 > 0 ? bulletSpeed1 : bulletSpeed)
                : (bulletSpeed2 > 0 ? bulletSpeed2 : bulletSpeed);

            if (bPrefab != null)
            {
                GameObject bullet = Instantiate(bPrefab, origin, Quaternion.identity);
                var cosmetic = bullet.GetComponent<CosmeticBullet>();
                if (cosmetic == null)
                {
                    cosmetic = bullet.AddComponent<CosmeticBullet>();
                }
                cosmetic.Initialize(hitPosition, hitNormal, hPrefab, speed, isCharacterHit);
            }
            else
            {
                StartCoroutine(DrawTracerRoutine(origin, hitPosition));

                if (hPrefab != null)
                {
                    Quaternion rot = (hitNormal != Vector3.zero) ? Quaternion.LookRotation(hitNormal) : Quaternion.identity;
                    GameObject effect = Instantiate(hPrefab, hitPosition, rot);
                    var cleanup = effect.GetComponent<VFXAutoCleanup>();
                    if (cleanup != null)
                    {
                        cleanup.SetCharacterHit(isCharacterHit);
                    }
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

        private void PlayShootEffects(int slot)
        {
            PlayMuzzleFlash(slot);

            if (weaponAudioSource != null && gunshotClip != null)
            {
                weaponAudioSource.pitch = (slot == 1) ? Random.Range(0.88f, 0.94f) : Random.Range(0.96f, 1.04f);
                weaponAudioSource.PlayOneShot(gunshotClip, (slot == 1) ? 1.0f : 0.95f);
            }

            if (IsOwner && CombatHUD.Instance != null)
            {
                CombatHUD.Instance.TriggerScopeRecoil(1.0f);
            }

            PlayShootEffectsClientRpc(slot);
        }

        [ClientRpc]
        private void PlayShootEffectsClientRpc(int slot, ClientRpcParams clientRpcParams = default)
        {
            if (IsOwner) return;

            PlayMuzzleFlash(slot);

            if (weaponAudioSource != null && gunshotClip != null)
            {
                weaponAudioSource.pitch = (slot == 1) ? Random.Range(0.88f, 0.94f) : Random.Range(0.96f, 1.04f);
                weaponAudioSource.PlayOneShot(gunshotClip, (slot == 1) ? 1.0f : 0.95f);
            }
        }

        private void PlayMuzzleFlash(int slot)
        {
            Transform muzzleTransform = (slot == 0) ? firePoint1 : firePoint2;
            if (muzzleTransform == null) muzzleTransform = (firePoint != null) ? firePoint : transform;

            GameObject prefabToSpawn = (slot == 0)
                ? (muzzleFlashPrefab1 != null ? muzzleFlashPrefab1 : (muzzleFlash != null ? muzzleFlash.gameObject : null))
                : (muzzleFlashPrefab2 != null ? muzzleFlashPrefab2 : (muzzleFlash != null ? muzzleFlash.gameObject : null));

            if (prefabToSpawn != null && muzzleTransform != null)
            {
                GameObject flashObj = Instantiate(prefabToSpawn, muzzleTransform.position, muzzleTransform.rotation, muzzleTransform);
                Destroy(flashObj, 1.5f);
            }
            else if (muzzleFlash != null)
            {
                muzzleFlash.Play();
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
using Unity.Netcode;
using UnityEngine;
using SniperGame.Weapons;

namespace SniperGame.Player
{
    /// <summary>
    /// Synchronizes 3D soldier animations across the network with 0 root motion desync.
    /// Drives locomotion blend trees, jump, wall-run, slide, and death states on both local owner and remote opponents.
    /// </summary>
    public class PlayerAnimationDriver : NetworkBehaviour
    {
        [Header("Animator & Component References")]
        [Tooltip("The Animator on the 3D soldier model (auto-detected if empty)")]
        [SerializeField] private Animator soldierAnimator;
        [SerializeField] private PlayerMovement playerMovement;
        [SerializeField] private PlayerHealth playerHealth;

        // Compact network synchronization structure (6 bytes total)
        public struct PlayerAnimNetworkData : INetworkSerializable
        {
            public float moveSpeed;     // 0.0 to ~16.0
            public byte stateFlags;     // Bit 0: Grounded, Bit 1: Crouched, Bit 2: Sliding, Bit 3: Dead
            public byte wallRunSide;    // 0 = None, 1 = Left, 2 = Right

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref moveSpeed);
                serializer.SerializeValue(ref stateFlags);
                serializer.SerializeValue(ref wallRunSide);
            }
        }

        private readonly NetworkVariable<PlayerAnimNetworkData> _netAnimData = new NetworkVariable<PlayerAnimNetworkData>(
            new PlayerAnimNetworkData { stateFlags = 1, moveSpeed = 0, wallRunSide = 0 },
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner
        );

        // Cached Animator Parameter Hashes
        private static readonly int MoveSpeedHash = Animator.StringToHash("MoveSpeed");
        private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
        private static readonly int IsCrouchedHash = Animator.StringToHash("IsCrouched");
        private static readonly int IsSlidingHash = Animator.StringToHash("IsSliding");
        private static readonly int WallRunSideHash = Animator.StringToHash("WallRunSide");
        private static readonly int JumpTriggerHash = Animator.StringToHash("Jump");
        private static readonly int WallJumpTriggerHash = Animator.StringToHash("WallJump");
        private static readonly int DieTriggerHash = Animator.StringToHash("Die");
        private static readonly int ReviveTriggerHash = Animator.StringToHash("Revive");
        private static readonly int ShootTriggerHash = Animator.StringToHash("Shoot");
        private static readonly int ReloadTriggerHash = Animator.StringToHash("Reload");
        private static readonly int IsScopedHash = Animator.StringToHash("IsScoped");

        private Vector3 _lastPosition;
        private float _currentDisplayedSpeed = 0f;
        private bool _isScoped = false;

        private void Awake()
        {
            if (playerMovement == null) playerMovement = GetComponent<PlayerMovement>();
            if (playerHealth == null) playerHealth = GetComponent<PlayerHealth>();

            FindSoldierAnimator();
        }

        public void FindSoldierAnimator()
        {
            if (soldierAnimator == null)
            {
                var animators = GetComponentsInChildren<Animator>(true);
                foreach (var anim in animators)
                {
                    // Ignore weapon animator (which is on WeaponCamera/CameraHolder)
                    if (anim.gameObject.name.ToLower().Contains("weapon")) continue;

                    soldierAnimator = anim;
                    break;
                }
            }

            if (soldierAnimator == null)
            {
                // Auto-detect soldier visual root and ensure Animator is attached
                Transform soldierTransform = transform.Find("Low poly soldier (1)");
                if (soldierTransform == null)
                {
                    foreach (Transform child in transform)
                    {
                        string childName = child.name.ToLower();
                        if (childName.Contains("camera") || childName.Contains("weapon") || childName.Contains("hitbox"))
                            continue;

                        if (child.GetComponentInChildren<SkinnedMeshRenderer>() != null)
                        {
                            soldierTransform = child;
                            break;
                        }
                    }
                }

                if (soldierTransform != null)
                {
                    soldierAnimator = soldierTransform.GetComponent<Animator>();
                    if (soldierAnimator == null)
                    {
                        soldierAnimator = soldierTransform.gameObject.AddComponent<Animator>();
                    }

                    if (soldierAnimator.runtimeAnimatorController == null)
                    {
                        soldierAnimator.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>("SoldierAnimatorController");
                    }
                }
            }

            if (soldierAnimator != null)
            {
                if (soldierAnimator.runtimeAnimatorController == null)
                {
                    soldierAnimator.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>("SoldierAnimatorController");
                }

                // CRITICAL: Disable root motion so CharacterController retains 100% physics authority
                soldierAnimator.applyRootMotion = false;
                soldierAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            FindSoldierAnimator();
            GetComponent<SniperWeapon>()?.ResolveThirdPersonWeapons();
            _lastPosition = transform.position;
        }

        private void Update()
        {
            if (soldierAnimator == null)
            {
                FindSoldierAnimator();
                if (soldierAnimator == null) return;
            }

            if (IsOwner)
            {
                UpdateLocalOwnerState();
            }
            else
            {
                UpdateRemoteProxyState();
            }
        }

        private void UpdateLocalOwnerState()
        {
            if (playerMovement == null) return;

            // Get velocity and parkour states directly from PlayerMovement
            float speed = playerMovement.CurrentHorizontalSpeed;
            bool grounded = playerMovement.IsGrounded;
            bool crouched = playerMovement.IsCrouched;
            bool sliding = playerMovement.IsSliding;
            int wallSide = playerMovement.WallRunSide; // 0, 1=Left, 2=Right
            bool dead = playerHealth != null && playerHealth.CurrentHealth.Value <= 0;

            byte flags = 0;
            if (grounded) flags |= (1 << 0);
            if (crouched) flags |= (1 << 1);
            if (sliding) flags |= (1 << 2);
            if (dead) flags |= (1 << 3);
            if (_isScoped) flags |= (1 << 4);

            // Update network variable if changed or moving
            PlayerAnimNetworkData current = _netAnimData.Value;
            if (Mathf.Abs(current.moveSpeed - speed) > 0.08f || current.stateFlags != flags || current.wallRunSide != (byte)wallSide)
            {
                _netAnimData.Value = new PlayerAnimNetworkData
                {
                    moveSpeed = speed,
                    stateFlags = flags,
                    wallRunSide = (byte)wallSide
                };
            }

            // Apply directly to animator
            ApplyParameters(speed, grounded, crouched, sliding, wallSide, _isScoped);
        }

        private void UpdateRemoteProxyState()
        {
            PlayerAnimNetworkData data = _netAnimData.Value;

            bool grounded = (data.stateFlags & (1 << 0)) != 0;
            bool crouched = (data.stateFlags & (1 << 1)) != 0;
            bool sliding = (data.stateFlags & (1 << 2)) != 0;
            bool isScoped = (data.stateFlags & (1 << 4)) != 0;
            int wallSide = data.wallRunSide;

            // Fallback speed deduction from transform delta if network variable is slightly latent
            float deltaDist = (transform.position - _lastPosition).magnitude;
            _lastPosition = transform.position;
            float estimatedSpeed = Time.deltaTime > 0 ? (deltaDist / Time.deltaTime) : 0f;
            float targetSpeed = Mathf.Max(data.moveSpeed, estimatedSpeed);

            _currentDisplayedSpeed = Mathf.Lerp(_currentDisplayedSpeed, targetSpeed, Time.deltaTime * 12f);

            ApplyParameters(_currentDisplayedSpeed, grounded, crouched, sliding, wallSide, isScoped);
        }

        private void ApplyParameters(float speed, bool grounded, bool crouched, bool sliding, int wallSide, bool isScoped)
        {
            soldierAnimator.SetFloat(MoveSpeedHash, speed);
            soldierAnimator.SetBool(IsGroundedHash, grounded);
            soldierAnimator.SetBool(IsCrouchedHash, crouched);
            soldierAnimator.SetBool(IsSlidingHash, sliding);
            soldierAnimator.SetInteger(WallRunSideHash, wallSide);
            soldierAnimator.SetBool(IsScopedHash, isScoped);
        }

        #region Action Triggers

        public void TriggerJump()
        {
            if (soldierAnimator != null)
            {
                soldierAnimator.SetTrigger(JumpTriggerHash);
            }

            if (IsOwner && IsSpawned)
            {
                TriggerJumpServerRpc();
            }
        }

        [ServerRpc]
        private void TriggerJumpServerRpc()
        {
            TriggerJumpClientRpc();
        }

        [ClientRpc]
        private void TriggerJumpClientRpc()
        {
            if (!IsOwner && soldierAnimator != null)
            {
                soldierAnimator.SetTrigger(JumpTriggerHash);
            }
        }

        public void TriggerWallJump()
        {
            if (soldierAnimator != null)
            {
                soldierAnimator.SetTrigger(WallJumpTriggerHash);
            }

            if (IsOwner && IsSpawned)
            {
                TriggerWallJumpServerRpc();
            }
        }

        [ServerRpc]
        private void TriggerWallJumpServerRpc()
        {
            TriggerWallJumpClientRpc();
        }

        [ClientRpc]
        private void TriggerWallJumpClientRpc()
        {
            if (!IsOwner && soldierAnimator != null)
            {
                soldierAnimator.SetTrigger(WallJumpTriggerHash);
            }
        }

        public void TriggerDie()
        {
            if (soldierAnimator != null)
            {
                soldierAnimator.ResetTrigger(ReviveTriggerHash);
                soldierAnimator.SetTrigger(DieTriggerHash);
            }
        }

        public void TriggerRevive()
        {
            if (soldierAnimator != null)
            {
                soldierAnimator.ResetTrigger(DieTriggerHash);
                soldierAnimator.SetTrigger(ReviveTriggerHash);
            }
        }

        public void SetScoped(bool isScoped)
        {
            _isScoped = isScoped;
            if (soldierAnimator != null)
            {
                soldierAnimator.SetBool(IsScopedHash, isScoped);
            }
        }

        public void TriggerShoot()
        {
            if (soldierAnimator != null)
            {
                soldierAnimator.SetTrigger(ShootTriggerHash);
            }

            if (IsOwner && IsSpawned)
            {
                TriggerShootServerRpc();
            }
        }

        [ServerRpc]
        private void TriggerShootServerRpc()
        {
            TriggerShootClientRpc();
        }

        [ClientRpc]
        private void TriggerShootClientRpc()
        {
            if (!IsOwner && soldierAnimator != null)
            {
                soldierAnimator.SetTrigger(ShootTriggerHash);
            }
        }

        public void TriggerReload()
        {
            if (soldierAnimator != null)
            {
                soldierAnimator.SetTrigger(ReloadTriggerHash);
            }

            if (IsOwner && IsSpawned)
            {
                TriggerReloadServerRpc();
            }
        }

        [ServerRpc]
        private void TriggerReloadServerRpc()
        {
            TriggerReloadClientRpc();
        }

        [ClientRpc]
        private void TriggerReloadClientRpc()
        {
            if (!IsOwner && soldierAnimator != null)
            {
                soldierAnimator.SetTrigger(ReloadTriggerHash);
            }
        }

        #endregion
    }
}

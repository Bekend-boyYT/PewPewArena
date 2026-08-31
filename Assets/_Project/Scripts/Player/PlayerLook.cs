using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using SniperGame.Gameplay;

namespace SniperGame.Player
{
    public class PlayerLook : NetworkBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform cameraHolder;

        [Header("Sensitivity Settings")]
        [SerializeField] private float mouseSensitivityX = 0.15f;
        [SerializeField] private float mouseSensitivityY = 0.15f;

        [Header("Rotation Limits")]
        [SerializeField] private float minPitch = -85f;
        [SerializeField] private float maxPitch = 85f;

        [Header("Procedural Recoil Settings")]
        [SerializeField] private float recoilSnappiness = 22.0f;
        [SerializeField] private float recoilReturnSpeed = 12.0f;

        private float _verticalRotation = 0f;
        private float _sensitivityMultiplier = 1.0f;

        private Vector2 _currentRecoil;
        private Vector2 _targetRecoil;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (!IsOwner)
            {
                enabled = false;
                return;
            }

            SceneManager.sceneLoaded += OnSceneLoaded;
            CheckActiveScene();
        }

        public override void OnNetworkDespawn()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            base.OnNetworkDespawn();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            CheckActiveScene();
        }

        private void CheckActiveScene()
        {
            if (!IsOwner) return;

            if (SceneManager.GetActiveScene().name == "Maintestgameplay")
            {
                SetCursorState(true);
            }
            else
            {
                SetCursorState(false);
            }
        }

        private void Update()
        {
            if (!IsOwner) return;

            if (SceneManager.GetActiveScene().name != "Maintestgameplay") return;

            if (RoundManager.Instance != null && RoundManager.Instance.CurrentState.Value == MatchState.MatchEnded)
            {
                if (Cursor.lockState != CursorLockMode.None || !Cursor.visible)
                {
                    SetCursorState(false);
                }
                return;
            }

            HandleRecoilDecay();
            HandleCursorLockInput();
            HandleLook();
        }

        private void HandleRecoilDecay()
        {
            // Recoil herstelt soepel naar de nulpositie
            _targetRecoil = Vector2.Lerp(_targetRecoil, Vector2.zero, Time.deltaTime * recoilReturnSpeed);
            _currentRecoil = Vector2.Lerp(_currentRecoil, _targetRecoil, Time.deltaTime * recoilSnappiness);
        }

        private void HandleLook()
        {
            if (Mouse.current == null || Cursor.lockState != CursorLockMode.Locked) return;

            Vector2 mouseDelta = Mouse.current.delta.ReadValue();

            float lookX = mouseDelta.x * (mouseSensitivityX * _sensitivityMultiplier);
            float lookY = mouseDelta.y * (mouseSensitivityY * _sensitivityMultiplier);

            // Horizontale rotatie (inclusief horizontale recoil)
            transform.Rotate(Vector3.up * (lookX + _currentRecoil.y * Time.deltaTime));

            // Verticale rotatie
            _verticalRotation -= lookY;
            _verticalRotation = Mathf.Clamp(_verticalRotation, minPitch, maxPitch);

            if (cameraHolder != null)
            {
                // Pas pitch toe inclusief actieve terugslag-kick
                float pitchWithRecoil = Mathf.Clamp(_verticalRotation - _currentRecoil.x, minPitch, maxPitch);
                cameraHolder.localRotation = Quaternion.Euler(pitchWithRecoil, 0f, 0f);
            }
        }

        /// <summary>
        /// Voegt een directe terugslag-kick toe aan het scherm.
        /// </summary>
        public void AddRecoil(float pitchKick, float yawKick)
        {
            _targetRecoil += new Vector2(pitchKick, yawKick);
        }

        private void HandleCursorLockInput()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                SetCursorState(false);
            }

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
            {
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                {
                    return;
                }

                SetCursorState(true);
            }
        }

        public void SetCursorState(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        public void SetSensitivityMultiplier(float multiplier)
        {
            _sensitivityMultiplier = multiplier;
        }
    }
}
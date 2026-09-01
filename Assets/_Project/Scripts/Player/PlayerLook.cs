using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using SniperGame.Gameplay;
using SniperGame.UI;

namespace SniperGame.Player
{
    public class PlayerLook : NetworkBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform cameraHolder;

        [Header("Sensitivity Settings")]
        [SerializeField] private float baseSensitivity = 0.15f;

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

            // Als pauzemenu open staat of match is afgelopen: blokkeer rondkijken
            if (PauseMenu.IsPaused || (RoundManager.Instance != null && RoundManager.Instance.CurrentState.Value == MatchState.MatchEnded))
            {
                return;
            }

            HandleRecoilDecay();
            HandleCursorLockInput();
            HandleLook();
        }

        private void HandleRecoilDecay()
        {
            _targetRecoil = Vector2.Lerp(_targetRecoil, Vector2.zero, Time.deltaTime * recoilReturnSpeed);
            _currentRecoil = Vector2.Lerp(_currentRecoil, _targetRecoil, Time.deltaTime * recoilSnappiness);
        }

        private void HandleLook()
        {
            if (Mouse.current == null || Cursor.lockState != CursorLockMode.Locked) return;

            Vector2 mouseDelta = Mouse.current.delta.ReadValue();

            // Lees dynamisch de gevoeligheid uit van de PauseMenu slider
            float liveSensitivity = PauseMenu.GetSensitivity();

            float lookX = mouseDelta.x * (liveSensitivity * _sensitivityMultiplier);
            float lookY = mouseDelta.y * (liveSensitivity * _sensitivityMultiplier);

            transform.Rotate(Vector3.up * (lookX + _currentRecoil.y * Time.deltaTime));

            _verticalRotation -= lookY;
            _verticalRotation = Mathf.Clamp(_verticalRotation, minPitch, maxPitch);

            if (cameraHolder != null)
            {
                float pitchWithRecoil = Mathf.Clamp(_verticalRotation - _currentRecoil.x, minPitch, maxPitch);
                cameraHolder.localRotation = Quaternion.Euler(pitchWithRecoil, 0f, 0f);
            }
        }

        public void AddRecoil(float pitchKick, float yawKick)
        {
            _targetRecoil += new Vector2(pitchKick, yawKick);
        }

        private void HandleCursorLockInput()
        {
            if (PauseMenu.IsPaused) return;

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
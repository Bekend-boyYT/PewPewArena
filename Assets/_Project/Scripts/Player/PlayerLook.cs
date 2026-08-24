using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

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

        private float _verticalRotation = 0f;
        private float _sensitivityMultiplier = 1.0f;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsOwner)
            {
                // Alleen cursor locken als we daadwerkelijk in de gameplay arena zijn
                if (SceneManager.GetActiveScene().name == "Maintestgameplay")
                {
                    SetCursorState(true);
                }
                else
                {
                    SetCursorState(false);
                }
            }
            else
            {
                enabled = false;
            }
        }

        private void Update()
        {
            if (!IsOwner) return;

            // Blokkeer rondkijken als we nog in het menu zitten
            if (SceneManager.GetActiveScene().name != "Maintestgameplay")
            {
                if (Cursor.lockState != CursorLockMode.None)
                {
                    SetCursorState(false);
                }
                return;
            }

            HandleCursorLockInput();
            HandleLook();
        }

        private void HandleLook()
        {
            if (Mouse.current == null || Cursor.lockState != CursorLockMode.Locked) return;

            Vector2 mouseDelta = Mouse.current.delta.ReadValue();

            float lookX = mouseDelta.x * (mouseSensitivityX * _sensitivityMultiplier);
            float lookY = mouseDelta.y * (mouseSensitivityY * _sensitivityMultiplier);

            transform.Rotate(Vector3.up * lookX);

            _verticalRotation -= lookY;
            _verticalRotation = Mathf.Clamp(_verticalRotation, minPitch, maxPitch);

            if (cameraHolder != null)
            {
                cameraHolder.localRotation = Quaternion.Euler(_verticalRotation, 0f, 0f);
            }
        }

        private void HandleCursorLockInput()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                SetCursorState(false);
            }

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
            {
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
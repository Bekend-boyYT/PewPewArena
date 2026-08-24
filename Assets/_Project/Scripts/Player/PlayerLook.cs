using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SniperGame.Player
{
    public class PlayerLook : NetworkBehaviour
    {
        [Header("References")]
        [Tooltip("Sleep hier het CameraHolder GameObject in.")]
        [SerializeField] private Transform cameraHolder;

        [Header("Sensitivity Settings")]
        [SerializeField] private float mouseSensitivityX = 0.15f;
        [SerializeField] private float mouseSensitivityY = 0.15f;

        [Header("Rotation Limits")]
        [SerializeField] private float minPitch = -85f;
        [SerializeField] private float maxPitch = 85f;

        private float _verticalRotation = 0f;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            // Alleen de eigenaar vergrendelt zijn muis en activeert input
            if (IsOwner)
            {
                SetCursorState(true);
            }
            else
            {
                enabled = false; // Schakel dit script uit op proxies van de tegenstander
            }
        }

        private void Update()
        {
            if (!IsOwner) return;

            HandleCursorLockInput();
            HandleLook();
        }

        private void HandleLook()
        {
            if (Mouse.current == null || Cursor.lockState != CursorLockMode.Locked) return;

            Vector2 mouseDelta = Mouse.current.delta.ReadValue();

            float lookX = mouseDelta.x * mouseSensitivityX;
            float lookY = mouseDelta.y * mouseSensitivityY;

            // 1. Horizontale rotatie: draai het gehele spelerlichaam (Y-as)
            transform.Rotate(Vector3.up * lookX);

            // 2. Verticale rotatie: kantel alleen de camera (X-as) met limiet
            _verticalRotation -= lookY;
            _verticalRotation = Mathf.Clamp(_verticalRotation, minPitch, maxPitch);

            if (cameraHolder != null)
            {
                cameraHolder.localRotation = Quaternion.Euler(_verticalRotation, 0f, 0f);
            }
        }

        private void HandleCursorLockInput()
        {
            // Druk op Escape om muis te bevrijden (bijv. voor menu of inspector)
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                SetCursorState(false);
            }

            // Klik met linkermuisknop om weer in de game te locken
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

        public void SetSensitivity(float sensX, float sensY)
        {
            mouseSensitivityX = sensX;
            mouseSensitivityY = sensY;
        }
    }
}
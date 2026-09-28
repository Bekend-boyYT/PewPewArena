using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using SniperGame.UI;

namespace ParkourFPS
{
    public class CursorState : NetworkBehaviour
    {
        [Header("Mouse")]
        [SerializeField] private bool mouseLocked = true;
        [SerializeField] private Texture2D mouseTexture;

        [Header("Cursor")]
        [SerializeField] private bool cursorVisible = true;
        [SerializeField] private Sprite cursorSprite;
        [SerializeField] private Image cursorImage;
        [SerializeField] private Vector2 cursorSize = new Vector2(10, 10);

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (!IsOwner)
            {
                if (cursorImage != null) cursorImage.gameObject.SetActive(false);
                enabled = false;
                return;
            }

            SetCursor(mouseLocked, cursorVisible);
        }

        private void Update()
        {
            if (!IsOwner) return;

            // Als het menu of pauzescherm open is, cursor vrijgeven zodat je knoppen kunt indrukken
            if (PauseMenu.IsPaused)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                if (cursorImage != null) cursorImage.gameObject.SetActive(false);
            }
            else
            {
                Cursor.lockState = mouseLocked ? CursorLockMode.Locked : CursorLockMode.None;
                Cursor.visible = !mouseLocked;
                if (cursorImage != null && cursorVisible) cursorImage.gameObject.SetActive(true);
            }
        }

        public void SetCursor(bool locked, bool visible)
        {
            mouseLocked = locked;
            cursorVisible = visible;

            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;

            if (mouseTexture != null)
                Cursor.SetCursor(mouseTexture, Vector2.zero, CursorMode.Auto);

            if (cursorSprite != null && cursorImage != null && visible)
            {
                cursorImage.gameObject.SetActive(true);
                cursorImage.sprite = cursorSprite;
                cursorImage.rectTransform.sizeDelta = cursorSize;
            }
            else if (cursorImage != null)
            {
                cursorImage.gameObject.SetActive(false);
            }
        }
    }
}
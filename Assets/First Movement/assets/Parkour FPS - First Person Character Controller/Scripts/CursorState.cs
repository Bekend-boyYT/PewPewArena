using UnityEngine;
using UnityEngine.UI;

/*
    Script to handle player cursor and mouse state.

    Please read How_To_Use.pdf in the asset folder.

    Credit to snon200,
    For any questions: snon200@gmail.com
*/

namespace ParkourFPS
{
    public class CursorState : MonoBehaviour
    {
        [Header("Mouse")]
        [Tooltip("if mouse is locked to the middle of the screen")]
        [SerializeField] private bool mouseLocked = true;
        [Tooltip("the mouse texture, leave empty for default mouse")]
        [SerializeField] private Texture2D mouseTexture;

        [Header("Cursor")]
        [Tooltip("if the cursor is visible")]
        [SerializeField] private bool cursorVisible = true;
        [Tooltip("the cursor sprite, leave empty for no cursor")]
        [SerializeField] private Sprite cursorSprite;
        [Tooltip("the cursor image object")]
        [SerializeField] private Image cursorImage;
        [Tooltip("the cursor size")]
        [SerializeField] private Vector2 cursorSize = new Vector2(10, 10);

        // Start is called before the first frame update
        private void Start()
        {
            SetCursor(mouseLocked, cursorVisible);
        }

        // set cursor state
        public void SetCursor(bool mouseLocked, bool cursorVisible)
        {
            Cursor.lockState = mouseLocked ? CursorLockMode.Locked : CursorLockMode.None; // make cursor locked / unlocked

            if (mouseTexture != null) // if mouse texture is set
                Cursor.SetCursor(mouseTexture, Vector2.zero, CursorMode.Auto); // set mouse texture

            if (cursorSprite != null && cursorImage != null && cursorVisible) // if cursor sprite is set and cursor is visible
            {
                cursorImage.gameObject.SetActive(true); // enable cursor

                cursorImage.sprite = cursorSprite; // set cursor sprite
                cursorImage.rectTransform.sizeDelta = cursorSize; // set cursor size
            }
            else
                cursorImage.gameObject.SetActive(false); // disable cursor
        }
    }
}
using Unity.Netcode;
using UnityEngine;

namespace SniperGame.Player
{
    public class PlayerSetup : NetworkBehaviour
    {
        [Header("Camera & Audio Setup")]
        [SerializeField] private Camera playerCamera;
        [SerializeField] private AudioListener audioListener;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsOwner)
            {
                // Lokale speler: activeer eigen camera en audio listener
                if (playerCamera != null)
                {
                    playerCamera.enabled = true;
                    playerCamera.tag = "MainCamera";
                }

                if (audioListener != null)
                {
                    audioListener.enabled = true;
                }
            }
            else
            {
                // Tegenstander: deactiveer camera en audio om conflicten te voorkomen
                if (playerCamera != null)
                {
                    playerCamera.enabled = false;
                    if (playerCamera.CompareTag("MainCamera"))
                    {
                        playerCamera.tag = "Untagged";
                    }
                }

                if (audioListener != null)
                {
                    audioListener.enabled = false;
                }
            }
        }
    }
}
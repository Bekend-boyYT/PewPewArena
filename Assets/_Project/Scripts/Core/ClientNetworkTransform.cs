using Unity.Netcode.Components;
using UnityEngine;

namespace SniperGame.Core
{
    [DisallowMultipleComponent]
    public class ClientNetworkTransform : NetworkTransform
    {
        /// <summary>
        /// Door false terug te geven, weet Netcode dat de Client (eigenaar) 
        /// zijn eigen transform mag updaten in plaats van alleen de server.
        /// </summary>
        protected override bool OnIsServerAuthoritative()
        {
            return false;
        }
    }
}
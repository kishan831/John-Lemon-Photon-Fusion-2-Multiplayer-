using Fusion;
using UnityEngine;

namespace StealthGame.Multiplayer
{
    /// <summary>
    /// Trigger volume at the level exit. Reports each player that reaches it to the
    /// shared NetworkGameManager. Only the Master Client (State Authority) records the
    /// arrival so counting stays consistent across clients.
    ///
    /// Put this on the finish trigger object (must have a NetworkObject + a trigger Collider).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ExitZone : NetworkBehaviour
    {
        public NetworkGameManager gameManager;

        void OnTriggerEnter(Collider other)
        {
            if (!HasStateAuthority || gameManager == null)
                return;

            var player = other.GetComponentInParent<NetworkPlayerController>();
            if (player != null)
                gameManager.ReportPlayerAtExit(player.Object.InputAuthority);
        }
    }
}

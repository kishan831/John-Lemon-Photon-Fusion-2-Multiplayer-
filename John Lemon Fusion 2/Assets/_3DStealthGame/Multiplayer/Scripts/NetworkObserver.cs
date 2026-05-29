using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace StealthGame.Multiplayer
{
    /// <summary>
    /// Networked replacement for Observer. Detects ANY networked player (not a single
    /// hard-wired one) and, when it has clear line of sight, reports the catch to the
    /// shared NetworkGameManager. Only the Master Client (State Authority) evaluates
    /// detection so the verdict is authoritative and consistent for everyone.
    /// </summary>
    public class NetworkObserver : NetworkBehaviour
    {
        [Tooltip("Shared game manager that owns the win/lose state.")]
        public NetworkGameManager gameManager;

        readonly List<NetworkPlayerController> m_PlayersInRange = new();

        void OnTriggerEnter(Collider other)
        {
            var player = other.GetComponentInParent<NetworkPlayerController>();
            if (player != null && !m_PlayersInRange.Contains(player))
                m_PlayersInRange.Add(player);
        }

        void OnTriggerExit(Collider other)
        {
            var player = other.GetComponentInParent<NetworkPlayerController>();
            if (player != null)
                m_PlayersInRange.Remove(player);
        }

        public override void FixedUpdateNetwork()
        {
            // Only the authority evaluates the catch so it's deterministic for all clients.
            if (!HasStateAuthority || gameManager == null)
                return;

            for (int i = m_PlayersInRange.Count - 1; i >= 0; i--)
            {
                var player = m_PlayersInRange[i];
                if (player == null)
                {
                    m_PlayersInRange.RemoveAt(i);
                    continue;
                }

                Vector3 direction = player.transform.position - transform.position + Vector3.up;
                if (Physics.Raycast(transform.position, direction, out RaycastHit hit))
                {
                    // Clear line of sight to the player (no wall in between)?
                    if (hit.collider.GetComponentInParent<NetworkPlayerController>() == player)
                    {
                        gameManager.ReportPlayerCaught(player.Object.InputAuthority);
                    }
                }
            }
        }
    }
}

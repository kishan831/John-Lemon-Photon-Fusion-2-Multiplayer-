using Fusion;
using UnityEngine;

namespace StealthGame.Multiplayer
{
    /// <summary>
    /// Networked replacement for WaypointPatrol. The enemy is a scene NetworkObject owned
    /// by the Shared Mode Master Client, so only that client simulates the patrol; a
    /// NetworkTransform on the same object replicates the motion to everyone else.
    /// </summary>
    public class NetworkWaypointPatrol : NetworkBehaviour
    {
        public float moveSpeed = 1.0f;
        public Transform[] waypoints;

        [Networked] int CurrentWaypointIndex { get; set; }

        public override void FixedUpdateNetwork()
        {
            // Only the authority (master client) drives the enemy; proxies interpolate.
            if (!HasStateAuthority || waypoints == null || waypoints.Length == 0)
                return;

            Transform currentWaypoint = waypoints[CurrentWaypointIndex];
            Vector3 currentToTarget = currentWaypoint.position - transform.position;

            if (currentToTarget.magnitude < 0.1f)
                CurrentWaypointIndex = (CurrentWaypointIndex + 1) % waypoints.Length;

            if (currentToTarget.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(currentToTarget);

            transform.position += currentToTarget.normalized * moveSpeed * Runner.DeltaTime;
        }
    }
}

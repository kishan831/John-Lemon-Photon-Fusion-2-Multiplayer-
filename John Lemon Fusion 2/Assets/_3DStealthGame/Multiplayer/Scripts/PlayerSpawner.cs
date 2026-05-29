using Fusion;
using UnityEngine;

namespace StealthGame.Multiplayer
{
    /// <summary>
    /// Spawns one networked John Lemon per player. Lives on a scene NetworkObject
    /// (e.g. the GameManager). Fusion calls PlayerJoined/PlayerLeft on every client
    /// for every player; in Shared Mode each client spawns ONLY its own avatar so it
    /// holds State Authority over it.
    /// </summary>
    public class PlayerSpawner : NetworkBehaviour, IPlayerJoined, IPlayerLeft
    {
        [Tooltip("The networked player prefab (must have a NetworkObject + NetworkCharacterController).")]
        public NetworkObject playerPrefab;

        [Tooltip("Optional spawn points. If empty, players spawn at this object's position.")]
        public Transform[] spawnPoints;

        public void PlayerJoined(PlayerRef player)
        {
            // Only spawn the avatar we own. Other clients spawn their own.
            if (player != Runner.LocalPlayer)
                return;

            var spawn = GetSpawnPoint(player);
            Runner.Spawn(
                playerPrefab,
                spawn.position,
                spawn.rotation,
                player); // inputAuthority = this player
        }

        public void PlayerLeft(PlayerRef player)
        {
            // In Shared Mode the leaving client's objects are released automatically.
            // Hook left here if you need custom cleanup (scoreboard, etc.).
        }

        (Vector3 position, Quaternion rotation) GetSpawnPoint(PlayerRef player)
        {
            if (spawnPoints != null && spawnPoints.Length > 0)
            {
                // Deterministic, collision-free slot per player index.
                var t = spawnPoints[player.PlayerId % spawnPoints.Length];
                return (t.position, t.rotation);
            }
            return (transform.position, transform.rotation);
        }
    }
}

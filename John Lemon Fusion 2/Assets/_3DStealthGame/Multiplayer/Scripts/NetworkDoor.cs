using Fusion;
using UnityEngine;

namespace StealthGame.Multiplayer
{
    /// <summary>
    /// Networked replacement for Door. A player that owns the matching key (checked in
    /// NetworkPlayerController) calls RPC_Open, which runs on the door's State Authority
    /// (Master Client) to despawn the door for everyone.
    ///
    /// Put this on a door prefab/scene object with a NetworkObject. Give it (or a child) a
    /// TRIGGER collider so the player's CharacterController fires OnTriggerEnter; keep the
    /// solid collider too if the door should physically block until opened.
    /// </summary>
    public class NetworkDoor : NetworkBehaviour
    {
        public string KeyName = "key1";

        [Networked] public bool Opened { get; set; }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void RPC_Open()
        {
            if (Opened)
                return;

            Opened = true;
            Runner.Despawn(Object);
        }
    }
}

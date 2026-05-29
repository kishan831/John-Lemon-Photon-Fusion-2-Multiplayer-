using Fusion;
using UnityEngine;

namespace StealthGame.Multiplayer
{
    /// <summary>
    /// Networked replacement for Key. The picking player (detected in NetworkPlayerController)
    /// calls RPC_Collect, which runs on the key's State Authority (Master Client) to flip the
    /// networked Collected flag and despawn the key for everyone.
    ///
    /// Put this on a key prefab/scene object with a NetworkObject and a trigger Collider.
    /// </summary>
    public class NetworkKey : NetworkBehaviour
    {
        public string KeyName = "key1";

        [Networked] public bool Collected { get; set; }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void RPC_Collect()
        {
            if (Collected)
                return;

            Collected = true;
            Runner.Despawn(Object);
        }
    }
}

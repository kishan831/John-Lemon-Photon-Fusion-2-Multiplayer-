using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;

namespace StealthGame.Multiplayer
{
    /// <summary>
    /// Networked replacement for the single-player PlayerMovement. Uses Fusion's built-in
    /// NetworkCharacterController for authoritative, synced movement.
    ///
    /// Shared Mode rules:
    ///   - HasStateAuthority is true on the client that owns THIS avatar -> it reads input
    ///     and drives the simulation. Everyone else sees a synced proxy.
    ///   - Animation params are synced by a NetworkMecanimAnimator component on the prefab.
    ///   - Footstep audio is driven from the synced velocity so every client hears everyone.
    /// </summary>
    [RequireComponent(typeof(NetworkCharacterController))]
    public class NetworkPlayerController : NetworkBehaviour
    {
        [Header("Input")]
        public InputAction MoveAction;

        [Header("Local-only objects")]
        [Tooltip("Camera rig + AudioListener etc. Enabled only on the client that owns this player.")]
        public GameObject[] localOnlyObjects;

        [Header("Audio")]
        [Tooltip("Speed (units/s) above which footstep audio plays.")]
        public float footstepThreshold = 0.1f;

        // Per-player keys, replicated to every client.
        [Networked, Capacity(8)]
        NetworkLinkedList<NetworkString<_16>> OwnedKeys => default;

        NetworkCharacterController m_Cc;
        Animator m_Animator;
        AudioSource m_AudioSource;

        public override void Spawned()
        {
            m_Cc = GetComponent<NetworkCharacterController>();
            m_Animator = GetComponentInChildren<Animator>();
            m_AudioSource = GetComponent<AudioSource>();

            bool isLocal = HasStateAuthority;

            // Only the owning client gets a camera and live input.
            if (localOnlyObjects != null)
            {
                foreach (var go in localOnlyObjects)
                    if (go != null) go.SetActive(isLocal);
            }

            // The AudioListener usually sits on the player root, so toggle the component
            // (not the GameObject) — only one listener may be active per scene.
            var listener = GetComponentInChildren<AudioListener>(true);
            if (listener != null)
                listener.enabled = isLocal;

            if (isLocal)
                MoveAction.Enable();
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (HasStateAuthority)
                MoveAction.Disable();
        }

        public override void FixedUpdateNetwork()
        {
            // Only the owner simulates movement; proxies are driven by replicated state.
            if (!HasStateAuthority)
                return;

            Vector2 input = MoveAction.ReadValue<Vector2>();
            Vector3 direction = new Vector3(input.x, 0f, input.y);

            // NetworkCharacterController handles rotation toward 'direction',
            // acceleration, gravity and grounding. Tune speeds on the component.
            m_Cc.Move(direction);

            bool isWalking = direction.sqrMagnitude > 0.0001f;
            if (m_Animator != null)
                m_Animator.SetBool("IsWalking", isWalking);
        }

        public override void Render()
        {
            // Drive footstep audio from synced velocity so every client hears every player.
            if (m_AudioSource == null)
                return;

            Vector3 v = m_Cc.Velocity;
            v.y = 0f;
            bool moving = v.magnitude > footstepThreshold;

            if (moving && !m_AudioSource.isPlaying)
                m_AudioSource.Play();
            else if (!moving && m_AudioSource.isPlaying)
                m_AudioSource.Stop();
        }

        // --- Keys -------------------------------------------------------------
        // Adding requires State Authority, which the owning client always has.
        public void AddKey(string keyName)
        {
            if (HasStateAuthority)
                OwnedKeys.Add(keyName);
        }

        public bool OwnKey(string keyName)
        {
            foreach (var k in OwnedKeys)
                if (k.ToString() == keyName)
                    return true;
            return false;
        }

        // --- Interactables ----------------------------------------------------
        // CharacterController fires OnTriggerEnter against trigger colliders.
        void OnTriggerEnter(Collider other)
        {
            // Only the owner processes its own pickups/doors.
            if (!HasStateAuthority)
                return;

            var key = other.GetComponentInParent<NetworkKey>();
            if (key != null && !key.Collected)
            {
                AddKey(key.KeyName);
                key.RPC_Collect();
                return;
            }

            var door = other.GetComponentInParent<NetworkDoor>();
            if (door != null && !door.Opened && OwnKey(door.KeyName))
                door.RPC_Open();
        }
    }
}

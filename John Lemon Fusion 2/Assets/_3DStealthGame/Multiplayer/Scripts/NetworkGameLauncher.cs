using System.Threading.Tasks;
using Fusion;
using UnityEngine;

namespace StealthGame.Multiplayer
{
    /// <summary>
    /// Bootstraps a Photon Fusion 2 session in Shared Mode and (optionally) shows a tiny
    /// host/join GUI for quick testing. Put one of these in the gameplay scene.
    ///
    /// Shared Mode: there is no dedicated server. The Photon cloud relays the session and
    /// each client has State Authority over the objects it spawns (its own player). One
    /// client is elected "Shared Mode Master Client" and owns the scene objects (enemies,
    /// game manager, keys, doors).
    /// </summary>
    [RequireComponent(typeof(NetworkRunner))]
    public class NetworkGameLauncher : MonoBehaviour
    {
        [Tooltip("Lobby/room name. Everyone using the same name lands in the same game.")]
        public string sessionName = "JohnLemonCoop";

        [Tooltip("Maximum players allowed in a session.")]
        public int maxPlayers = 4;

        [Tooltip("If true, the launcher connects automatically on Start instead of showing the GUI.")]
        public bool autoConnect = false;

        NetworkRunner m_Runner;
        bool m_Connecting;
        string m_Status = "Not connected";

        void Awake()
        {
            m_Runner = GetComponent<NetworkRunner>();
            // We read input locally in the player controller (shared-mode pattern),
            // so the runner does not need to provide input.
            m_Runner.ProvideInput = false;
        }

        async void Start()
        {
            if (autoConnect)
                await Connect();
        }

        public async Task Connect()
        {
            if (m_Connecting || (m_Runner != null && m_Runner.IsRunning))
                return;

            m_Connecting = true;
            m_Status = "Connecting...";

            // Make sure a scene manager exists so Fusion can enroll the scene objects
            // (enemies, GameManager, keys, doors) that are already placed in this scene.
            var sceneManager = GetComponent<NetworkSceneManagerDefault>();
            if (sceneManager == null)
                sceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>();

            var result = await m_Runner.StartGame(new StartGameArgs
            {
                GameMode = GameMode.Shared,
                SessionName = sessionName,
                PlayerCount = maxPlayers,
                SceneManager = sceneManager,
            });

            m_Connecting = false;
            m_Status = result.Ok ? $"Connected to '{sessionName}'" : $"Failed: {result.ShutdownReason}";
        }

        // --- Minimal runtime GUI for quick local testing -------------------------
        // Replace this with the existing UI Toolkit menu when ready (call Connect()).
        void OnGUI()
        {
            if (m_Runner != null && m_Runner.IsRunning)
            {
                GUI.Label(new Rect(10, 10, 400, 20), m_Status);
                return;
            }

            GUILayout.BeginArea(new Rect(10, 10, 260, 140), GUI.skin.box);
            GUILayout.Label("John Lemon Co-op (Fusion 2)");
            sessionName = GUILayout.TextField(sessionName);
            GUI.enabled = !m_Connecting;
            if (GUILayout.Button("Host / Join Shared Session"))
                _ = Connect();
            GUI.enabled = true;
            GUILayout.Label(m_Status);
            GUILayout.EndArea();
        }
    }
}

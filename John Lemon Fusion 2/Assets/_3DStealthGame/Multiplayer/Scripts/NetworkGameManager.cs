using System.Collections.Generic;
using System.Linq;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace StealthGame.Multiplayer
{
    public enum GamePhase : byte { Playing = 0, Won = 1, Lost = 2 }

    /// <summary>
    /// Networked replacement for GameEnding. Holds the shared co-op win/lose state.
    ///
    /// Co-op rules (tweak to taste):
    ///   - LOSE: if ANY player is caught, everyone loses (shared fate).
    ///   - WIN:  when ALL active players have reached the exit.
    ///
    /// State is mutated only by the Shared Mode Master Client (State Authority of this
    /// scene object); the [Networked] phase replicates to every client, which then plays
    /// its own end-screen fade. Place this on a scene NetworkObject and add a PlayerSpawner
    /// to the same object.
    /// </summary>
    public class NetworkGameManager : NetworkBehaviour
    {
        [Header("UI")]
        public UIDocument uiDocument;
        public float fadeDuration = 1f;
        public float displayImageDuration = 1f;

        [Header("Audio")]
        public AudioSource exitAudio;
        public AudioSource caughtAudio;

        [Networked] public GamePhase Phase { get; set; }
        [Networked] public float GameTime { get; set; }

        // Master-client-only bookkeeping (not networked: only the authority touches it).
        readonly HashSet<PlayerRef> m_PlayersAtExit = new();

        VisualElement m_EndScreen;
        VisualElement m_CaughtScreen;
        Label m_TimerLabel;
        float m_FadeTimer;
        bool m_AudioPlayed;
        bool m_Restarted;

        public override void Spawned()
        {
            if (uiDocument != null)
            {
                var root = uiDocument.rootVisualElement;
                m_EndScreen = root.Q<VisualElement>("EndScreen");
                m_CaughtScreen = root.Q<VisualElement>("CaughtScreen");
                m_TimerLabel = root.Q<Label>("Demo_TimerLabel");
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority)
                return;

            if (Phase == GamePhase.Playing)
                GameTime += Runner.DeltaTime;
        }

        // --- Authoritative state changes (called by Observer / ExitZone) ----------
        public void ReportPlayerCaught(PlayerRef player)
        {
            if (!HasStateAuthority || Phase != GamePhase.Playing)
                return;

            Phase = GamePhase.Lost;
        }

        public void ReportPlayerAtExit(PlayerRef player)
        {
            if (!HasStateAuthority || Phase != GamePhase.Playing)
                return;

            m_PlayersAtExit.Add(player);

            int activePlayers = Runner.ActivePlayers.Count();
            if (m_PlayersAtExit.Count >= activePlayers)
                Phase = GamePhase.Won;
        }

        // --- Presentation (runs on every client) ----------------------------------
        public override void Render()
        {
            if (m_TimerLabel != null)
                m_TimerLabel.text = GameTime.ToString("0.00");

            if (Phase == GamePhase.Won)
                PlayEndScreen(m_EndScreen, exitAudio);
            else if (Phase == GamePhase.Lost)
                PlayEndScreen(m_CaughtScreen, caughtAudio);
        }

        void PlayEndScreen(VisualElement element, AudioSource audioSource)
        {
            if (!m_AudioPlayed && audioSource != null)
            {
                audioSource.Play();
                m_AudioPlayed = true;
            }

            m_FadeTimer += Time.deltaTime;
            if (element != null)
                element.style.opacity = m_FadeTimer / fadeDuration;

            // After the fade, the Master Client restarts the session for everyone (once).
            if (!m_Restarted && m_FadeTimer > fadeDuration + displayImageDuration && HasStateAuthority)
            {
                m_Restarted = true;
                // Reload the current gameplay scene through Fusion so all clients follow.
                // (Alternative: Runner.Shutdown() and return to a menu scene.)
                Runner.LoadScene(SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex));
            }
        }
    }
}

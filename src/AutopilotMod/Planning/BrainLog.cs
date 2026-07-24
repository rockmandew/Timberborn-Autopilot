using System.Collections.Generic;
using Timberborn.QuickNotificationSystem;
using UnityEngine;

namespace TimberbornAutopilot.Planning
{
    /// <summary>
    /// The brain's voice. Short intent messages so the player always knows what
    /// the autopilot is doing and why, and can work in tandem with it.
    /// Major messages show as in-game notifications; everything lands in the
    /// log and the /api/autopilot/brain feed.
    /// </summary>
    public class BrainLog
    {
        private const int MaxMessages = 200;

        private readonly QuickNotificationService _notifications;
        private readonly List<string> _messages = new List<string>();

        public BrainLog(QuickNotificationService notifications)
        {
            _notifications = notifications;
        }

        public IReadOnlyList<string> Recent => _messages;

        /// <summary>An action or decision the player should see (in-game toast).</summary>
        public void Announce(string message)
        {
            Record(message);
            _notifications.SendNotification("[Autopilot] " + message);
        }

        /// <summary>A recommendation — the brain never acts on these itself.
        /// The player is the final say.</summary>
        public void Suggest(string message)
        {
            Record("SUGGESTION: " + message);
            _notifications.SendNotification("[Autopilot suggests] " + message);
        }

        /// <summary>Detail for the log/dashboard only — no toast, no bloat.</summary>
        public void Note(string message)
        {
            Record(message);
        }

        private void Record(string message)
        {
            Debug.Log("[Autopilot:Brain] " + message);
            _messages.Add(message);
            if (_messages.Count > MaxMessages)
            {
                _messages.RemoveAt(0);
            }
        }
    }
}

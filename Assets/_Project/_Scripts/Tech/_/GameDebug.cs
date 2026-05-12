using System.Diagnostics;
using UnityEngine;

namespace KNLVN
{
    /// <summary>
    /// Drop-in replacement for UnityEngine.Debug.Log that is:
    ///   • Stripped entirely from non-development builds via [Conditional].
    ///   • Togglable at runtime with <see cref="Enabled"/> (Editor / dev builds only).
    ///
    /// Usage:
    ///   GameDebug.Log("msg");
    ///   GameDebug.LogWarning("msg");
    ///   GameDebug.LogError("msg");      ← errors are always shown, even in builds
    ///
    /// To silence all game logs at runtime:
    ///   GameDebug.Enabled = false;
    /// </summary>
    public static class GameDebug
    {
        /// <summary>
        /// Toggle all Log / LogWarning calls. Only has effect in Editor / development builds.
        /// LogError is always printed regardless of this flag.
        /// </summary>
        public static bool Enabled = true;

        // ── Log ───────────────────────────────────────────────────────────────

        /// <summary>Prints a message. Stripped from release builds automatically.</summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Log(object message)
        {
            if (Enabled) UnityEngine.Debug.Log(message);
        }

        /// <summary>Prints a message with context. Stripped from release builds automatically.</summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Log(object message, Object context)
        {
            if (Enabled) UnityEngine.Debug.Log(message, context);
        }

        // ── LogWarning ────────────────────────────────────────────────────────

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void LogWarning(object message)
        {
            if (Enabled) UnityEngine.Debug.LogWarning(message);
        }

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void LogWarning(object message, Object context)
        {
            if (Enabled) UnityEngine.Debug.LogWarning(message, context);
        }

        // ── LogError ─────────────────────────────────────────────────────────
        // Errors are always printed — do NOT apply [Conditional] here.

        public static void LogError(object message)                        => UnityEngine.Debug.LogError(message);
        public static void LogError(object message, Object context)        => UnityEngine.Debug.LogError(message, context);
    }
}

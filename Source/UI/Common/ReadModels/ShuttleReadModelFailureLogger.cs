using System;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.ReadModels
{
    internal static class ShuttleReadModelFailureLogger
    {
        internal static void LogOnce(string actionName, Exception exception)
        {
            if (!Prefs.DevMode || exception == null)
            {
                return;
            }

            Log.WarningOnce(
                "[CeleTech Shuttle] DevMode Shuttle Control read action '" +
                (actionName ?? "unknown") +
                "' failed and used its fallback path. Exception: " + exception,
                MakeHash(actionName, exception));
        }

        private static int MakeHash(string actionName, Exception exception)
        {
            unchecked
            {
                int hash = 41;
                hash = (hash * 37) + (actionName != null ? actionName.GetHashCode() : 0);
                hash = (hash * 37) + (exception != null && exception.GetType() != null
                    ? exception.GetType().FullName.GetHashCode()
                    : 0);
                hash = (hash * 37) + (exception != null && exception.Message != null
                    ? exception.Message.GetHashCode()
                    : 0);
                return hash;
            }
        }
    }
}

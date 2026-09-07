using System.Collections.Generic;
using Verse;

namespace CeleTech.ShuttleExtension.AdditionalModule
{
    internal static class AdditionalModuleLog
    {
        private const string Prefix = "[CeleTech Shuttle Integrations] ";
        private static readonly HashSet<string> WarnedKeys = new HashSet<string>();
        private static readonly HashSet<string> ErroredKeys = new HashSet<string>();

        public static void Message(string message)
        {
            Log.Message(Prefix + message);
        }

        public static void Warning(string message)
        {
            Log.Warning(Prefix + message);
        }

        public static void WarningOnce(string key, string message)
        {
            if (string.IsNullOrEmpty(key) || WarnedKeys.Add(key))
            {
                Warning(message);
            }
        }

        public static void ErrorOnce(string key, string message)
        {
            if (string.IsNullOrEmpty(key) || ErroredKeys.Add(key))
            {
                Log.Error(Prefix + message);
            }
        }

        public static void Dev(string message)
        {
            if (Prefs.DevMode)
            {
                Message(message);
            }
        }
    }
}

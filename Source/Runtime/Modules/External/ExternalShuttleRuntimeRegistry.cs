using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.API.Runtime;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.External
{
    internal static class ExternalShuttleRuntimeRegistry
    {
        internal const string LogPrefix = "[CeleTech ShuttleExtension] Runtime system ";

        private static readonly Dictionary<string, ExternalRuntimeRegistration> registrations =
            new Dictionary<string, ExternalRuntimeRegistration>(StringComparer.Ordinal);

        private static readonly HashSet<string> nonNamespacedKeyWarnings =
            new HashSet<string>(StringComparer.Ordinal);

        private static readonly HashSet<string> missingMetadataWarnings =
            new HashSet<string>(StringComparer.Ordinal);

        private static int revision;

        internal static int Revision
        {
            get
            {
                return revision;
            }
        }

        internal static bool Register(
            string ownerPackageId,
            string localRuntimeKey,
            IShuttleExternalModuleRuntimeSystem system,
            string runtimeLabelKey,
            string runtimeDescriptionKey)
        {
            string normalizedOwner = NormalizeKey(ownerPackageId);
            if (normalizedOwner == null)
            {
                Log.Warning(LogPrefix + "registration rejected: ownerPackageId is empty.");
                return false;
            }

            string normalizedLocalKey = NormalizeKey(localRuntimeKey);
            if (normalizedLocalKey == null)
            {
                Log.Warning(LogPrefix + "registration rejected: localRuntimeKey is empty for owner '" +
                    normalizedOwner + "'.");
                return false;
            }

            if (system == null)
            {
                Log.Warning(LogPrefix + "registration rejected: system is null for owner '" +
                    normalizedOwner + "' and local key '" + normalizedLocalKey + "'.");
                return false;
            }

            string fullRuntimeKey = normalizedOwner + "/" + normalizedLocalKey;
            WarnIfKeyIsNotNamespaced(fullRuntimeKey);
            string normalizedLabelKey = NormalizeOptionalKey(runtimeLabelKey);
            string normalizedDescriptionKey = NormalizeOptionalKey(runtimeDescriptionKey);
            WarnIfRuntimeMetadataIsMissing(fullRuntimeKey, normalizedLabelKey, normalizedDescriptionKey);

            if (registrations.ContainsKey(fullRuntimeKey))
            {
                Log.Warning(LogPrefix + "registration rejected for duplicate key '" +
                    fullRuntimeKey + "'. First registration wins.");
                return false;
            }

            registrations.Add(
                fullRuntimeKey,
                new ExternalRuntimeRegistration(
                    normalizedOwner,
                    normalizedLocalKey,
                    fullRuntimeKey,
                    system,
                    normalizedLabelKey,
                    normalizedDescriptionKey));
            revision++;
            return true;
        }

        internal static bool TryResolve(
            string fullRuntimeKey,
            out ExternalRuntimeRegistration registration)
        {
            registration = null;
            string normalizedKey = ShuttleRuntimeSystemKeyUtility.Normalize(fullRuntimeKey);
            if (normalizedKey == null)
            {
                return false;
            }

            WarnIfKeyIsNotNamespaced(normalizedKey);
            return registrations.TryGetValue(normalizedKey, out registration);
        }

        internal static List<ExternalRuntimeRegistration> GetRegistrationsSnapshot()
        {
            List<string> keys = new List<string>(registrations.Keys);
            keys.Sort(StringComparer.Ordinal);

            List<ExternalRuntimeRegistration> snapshot =
                new List<ExternalRuntimeRegistration>(keys.Count);
            for (int i = 0; i < keys.Count; i++)
            {
                snapshot.Add(registrations[keys[i]]);
            }

            return snapshot;
        }

        internal static string NormalizeKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return null;
            }

            return key.Trim();
        }

        private static string NormalizeOptionalKey(string key)
        {
            return string.IsNullOrWhiteSpace(key) ? null : key.Trim();
        }

        internal static void WarnIfKeyIsNotNamespaced(string key)
        {
            if (!Prefs.DevMode ||
                string.IsNullOrEmpty(key) ||
                ShuttleRuntimeSystemKeyUtility.IsNamespaced(key))
            {
                return;
            }

            if (nonNamespacedKeyWarnings.Add(key))
            {
                Log.WarningOnce(
                    LogPrefix + "key '" + key +
                    "' is not namespaced. Use package.id/local-key for third-party runtime systems.",
                    StableStringHash(LogPrefix + "|non-namespaced|" + key));
            }
        }

        private static int StableStringHash(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return 0;
            }

            unchecked
            {
                int hash = 29;
                for (int i = 0; i < value.Length; i++)
                {
                    hash = (hash * 397) ^ value[i];
                }

                return hash;
            }
        }

        private static void WarnIfRuntimeMetadataIsMissing(
            string fullRuntimeKey,
            string runtimeLabelKey,
            string runtimeDescriptionKey)
        {
            if (!Prefs.DevMode || string.IsNullOrEmpty(fullRuntimeKey))
            {
                return;
            }

            if (!string.IsNullOrEmpty(runtimeLabelKey) &&
                !string.IsNullOrEmpty(runtimeDescriptionKey))
            {
                return;
            }

            string warningKey = fullRuntimeKey + "|metadata";
            if (!missingMetadataWarnings.Add(warningKey))
            {
                return;
            }

            Log.Warning(LogPrefix + "registration '" + fullRuntimeKey +
                "' is missing player-facing metadata keys. labelKey='" +
                (runtimeLabelKey ?? "(none)") + "', descriptionKey='" +
                (runtimeDescriptionKey ?? "(none)") + "'.");
        }
    }
}

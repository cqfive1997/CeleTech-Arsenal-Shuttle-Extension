using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.API.Commands;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Commands.External
{
    internal static class ExternalShuttleCommandRegistry
    {
        internal const string LogPrefix = "[CeleTech ShuttleExtension] Command ";

        private static readonly Dictionary<string, ExternalCommandRegistration> registrations =
            new Dictionary<string, ExternalCommandRegistration>(StringComparer.Ordinal);

        private static readonly HashSet<string> nonNamespacedKeyWarnings =
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
            string localCommandKey,
            IShuttleExternalCommandHandler handler)
        {
            string normalizedOwner = NormalizeKey(ownerPackageId);
            if (normalizedOwner == null)
            {
                Log.Warning(LogPrefix + "registration rejected: ownerPackageId is empty.");
                return false;
            }

            string normalizedLocalKey = NormalizeKey(localCommandKey);
            if (normalizedLocalKey == null)
            {
                Log.Warning(LogPrefix + "registration rejected: localCommandKey is empty for owner '" +
                    normalizedOwner + "'.");
                return false;
            }

            if (handler == null)
            {
                Log.Warning(LogPrefix + "registration rejected: handler is null for owner '" +
                    normalizedOwner + "' and local key '" + normalizedLocalKey + "'.");
                return false;
            }

            string fullCommandKey = normalizedOwner + "/" + normalizedLocalKey;
            WarnIfKeyIsNotNamespaced(fullCommandKey);

            if (registrations.ContainsKey(fullCommandKey))
            {
                Log.Warning(LogPrefix + "registration rejected for duplicate key '" +
                    fullCommandKey + "'. First registration wins.");
                return false;
            }

            registrations.Add(
                fullCommandKey,
                new ExternalCommandRegistration(
                    normalizedOwner,
                    normalizedLocalKey,
                    fullCommandKey,
                    handler));
            revision++;
            return true;
        }

        internal static bool TryResolve(
            string fullCommandKey,
            out ExternalCommandRegistration registration)
        {
            registration = null;
            string normalizedKey = NormalizeKey(fullCommandKey);
            if (normalizedKey == null)
            {
                return false;
            }

            WarnIfKeyIsNotNamespaced(normalizedKey);
            return registrations.TryGetValue(normalizedKey, out registration);
        }

        internal static List<ExternalCommandRegistration> GetRegistrationsSnapshot()
        {
            List<string> keys = new List<string>(registrations.Keys);
            keys.Sort(StringComparer.Ordinal);

            List<ExternalCommandRegistration> snapshot =
                new List<ExternalCommandRegistration>(keys.Count);
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

        internal static void WarnIfKeyIsNotNamespaced(string key)
        {
            if (string.IsNullOrEmpty(key) || key.IndexOf('/') >= 0)
            {
                return;
            }

            if (nonNamespacedKeyWarnings.Add(key))
            {
                Log.Warning(LogPrefix + "key '" + key +
                    "' is not namespaced. Use package.id/local-key for third-party commands.");
            }
        }
    }
}

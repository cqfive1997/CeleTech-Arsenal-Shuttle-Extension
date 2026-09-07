using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.API.UI;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.External
{
    internal static class ExternalModulePanelRegistry
    {
        private const string LogPrefix = "[CeleTech ShuttleExtension] UI ";

        private static readonly Dictionary<string, ExternalModulePanelRegistration> registrations =
            new Dictionary<string, ExternalModulePanelRegistration>(System.StringComparer.Ordinal);
        private static readonly HashSet<string> nonNamespacedWarnings =
            new HashSet<string>(System.StringComparer.Ordinal);
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
            string localPanelKey,
            IShuttleExternalModulePanelProvider provider)
        {
            string owner = NormalizeKey(ownerPackageId);
            string local = NormalizeKey(localPanelKey);
            if (owner == null)
            {
                Log.Warning(LogPrefix + "panel provider registration failed: ownerPackageId is empty.");
                return false;
            }

            if (local == null)
            {
                Log.Warning(LogPrefix + "panel provider registration failed for owner " + owner +
                    ": localPanelKey is empty.");
                return false;
            }

            if (provider == null)
            {
                Log.Warning(LogPrefix + "panel provider registration failed for " + owner + "/" + local +
                    ": provider is null.");
                return false;
            }

            string runtimeKey;
            try
            {
                runtimeKey = ShuttleRuntimeSystemKeyUtility.Normalize(provider.RuntimeSystemKey);
            }
            catch (System.Exception exception)
            {
                Log.Warning(LogPrefix + "panel provider registration failed for " + owner + "/" + local +
                    ": RuntimeSystemKey property threw an exception. Exception: " + exception);
                return false;
            }

            if (runtimeKey == null)
            {
                Log.Warning(LogPrefix + "panel provider registration failed for " + owner + "/" + local +
                    ": RuntimeSystemKey is empty.");
                return false;
            }

            if (!ShuttleRuntimeSystemKeyUtility.IsNamespaced(runtimeKey))
            {
                Log.Warning(LogPrefix + "panel provider registration failed for " + owner + "/" + local +
                    ": RuntimeSystemKey must be namespaced as package.id/local-key. Value: " + runtimeKey);
                return false;
            }

            if (!runtimeKey.StartsWith(owner + "/", System.StringComparison.Ordinal))
            {
                Log.Warning(LogPrefix + "panel provider registration failed for " + owner + "/" + local +
                    ": RuntimeSystemKey must be owned by the same package in UI-R1b. RuntimeSystemKey=" +
                    runtimeKey + ".");
                return false;
            }

            string fullKey = owner + "/" + local;
            WarnIfKeyIsNotNamespaced(fullKey);
            if (registrations.ContainsKey(fullKey))
            {
                Log.Warning(LogPrefix + "panel provider duplicate key ignored: " + fullKey +
                    ". First registration wins.");
                return false;
            }

            registrations.Add(
                fullKey,
                new ExternalModulePanelRegistration(
                    owner,
                    local,
                    fullKey,
                    runtimeKey,
                    provider));
            unchecked
            {
                revision++;
            }

            return true;
        }

        internal static bool TryResolve(
            string fullPanelKey,
            out ExternalModulePanelRegistration registration)
        {
            registration = null;
            string key = NormalizeKey(fullPanelKey);
            if (key == null)
            {
                return false;
            }

            WarnIfKeyIsNotNamespaced(key);
            return registrations.TryGetValue(key, out registration);
        }

        internal static List<ExternalModulePanelRegistration> GetRegistrationsSnapshot()
        {
            List<ExternalModulePanelRegistration> snapshot =
                new List<ExternalModulePanelRegistration>(registrations.Values);
            snapshot.Sort(CompareRegistrations);
            return snapshot;
        }

        internal static List<ExternalModulePanelRegistration> GetRegistrationsForRuntimeKey(
            string runtimeSystemKey)
        {
            string key = ShuttleRuntimeSystemKeyUtility.Normalize(runtimeSystemKey);
            List<ExternalModulePanelRegistration> result = new List<ExternalModulePanelRegistration>();
            if (key == null)
            {
                return result;
            }

            foreach (ExternalModulePanelRegistration registration in registrations.Values)
            {
                if (registration != null && registration.RuntimeSystemKey == key)
                {
                    result.Add(registration);
                }
            }

            result.Sort(CompareRegistrations);
            return result;
        }

        private static int CompareRegistrations(
            ExternalModulePanelRegistration left,
            ExternalModulePanelRegistration right)
        {
            string leftKey = left != null ? left.FullPanelKey : string.Empty;
            string rightKey = right != null ? right.FullPanelKey : string.Empty;
            return System.StringComparer.Ordinal.Compare(leftKey, rightKey);
        }

        private static string NormalizeKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return null;
            }

            return key.Trim();
        }

        private static void WarnIfKeyIsNotNamespaced(string key)
        {
            if (!Prefs.DevMode ||
                string.IsNullOrEmpty(key) ||
                ShuttleRuntimeSystemKeyUtility.IsNamespaced(key))
            {
                return;
            }

            if (nonNamespacedWarnings.Add(key))
            {
                Log.WarningOnce(
                    LogPrefix + "panel provider key is not namespaced. Recommended format is package.id/local-key. Key: " +
                    key,
                    StableStringHash(LogPrefix + "|non-namespaced-panel|" + key));
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
                int hash = 31;
                for (int i = 0; i < value.Length; i++)
                {
                    hash = (hash * 397) ^ value[i];
                }

                return hash;
            }
        }
    }
}

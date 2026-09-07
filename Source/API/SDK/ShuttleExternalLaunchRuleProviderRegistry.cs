using System;
using System.Collections.Generic;

namespace CeleTech.ShuttleExtension.ModularShuttle.API.SDK
{
    public static class ShuttleExternalLaunchRuleProviderRegistry
    {
        private static readonly object syncRoot = new object();

        private static readonly Dictionary<string, ShuttleExternalLaunchRuleProviderRegistration>
            registrations =
                new Dictionary<string, ShuttleExternalLaunchRuleProviderRegistration>(
                    StringComparer.Ordinal);

        private static int revision;

        public static int RegisteredProviderCount
        {
            get
            {
                lock (syncRoot)
                {
                    return registrations.Count;
                }
            }
        }

        internal static int Revision
        {
            get
            {
                lock (syncRoot)
                {
                    return revision;
                }
            }
        }

        public static bool TryRegister(
            IShuttleExternalLaunchRuleProvider provider,
            out string rejectionReason)
        {
            rejectionReason = null;
            if (provider == null)
            {
                rejectionReason = "provider is null";
                return false;
            }

            ShuttleExternalLaunchRuleProviderInfo info;
            try
            {
                info = provider.GetProviderInfo();
            }
            catch (Exception exception)
            {
                rejectionReason = "provider info failed: " +
                    (exception != null ? exception.GetType().Name : "unknown");
                rejectionReason =
                    ShuttleExternalLaunchRuleText.ClampOptionalMessage(rejectionReason);
                return false;
            }

            ShuttleExternalLaunchRuleProviderInfo sanitizedInfo = SanitizeInfo(info);
            if (sanitizedInfo == null)
            {
                rejectionReason = "provider info is invalid";
                return false;
            }

            string providerKey = sanitizedInfo.ProviderKey;
            lock (syncRoot)
            {
                if (registrations.ContainsKey(providerKey))
                {
                    rejectionReason = "duplicate provider key: " + providerKey;
                    rejectionReason =
                        ShuttleExternalLaunchRuleText.ClampOptionalMessage(rejectionReason);
                    return false;
                }

                registrations.Add(
                    providerKey,
                    new ShuttleExternalLaunchRuleProviderRegistration(
                        provider,
                        sanitizedInfo));
                revision++;
            }

            return true;
        }

        public static IReadOnlyList<ShuttleExternalLaunchRuleProviderInfo>
            GetRegisteredProviders()
        {
            List<ShuttleExternalLaunchRuleProviderRegistration> snapshot =
                GetRegistrationsSnapshotForEvaluation();
            List<ShuttleExternalLaunchRuleProviderInfo> result =
                new List<ShuttleExternalLaunchRuleProviderInfo>(snapshot.Count);

            for (int i = 0; i < snapshot.Count; i++)
            {
                if (snapshot[i] != null && snapshot[i].Info != null)
                {
                    result.Add(snapshot[i].Info);
                }
            }

            return ShuttleExternalSDKCollections.Copy(result);
        }

        internal static List<ShuttleExternalLaunchRuleProviderRegistration>
            GetRegistrationsSnapshotForEvaluation()
        {
            lock (syncRoot)
            {
                List<string> keys = new List<string>(registrations.Keys);
                keys.Sort(StringComparer.Ordinal);

                List<ShuttleExternalLaunchRuleProviderRegistration> snapshot =
                    new List<ShuttleExternalLaunchRuleProviderRegistration>(keys.Count);
                for (int i = 0; i < keys.Count; i++)
                {
                    snapshot.Add(registrations[keys[i]]);
                }

                return snapshot;
            }
        }

        private static ShuttleExternalLaunchRuleProviderInfo SanitizeInfo(
            ShuttleExternalLaunchRuleProviderInfo info)
        {
            if (info == null ||
                string.IsNullOrEmpty(info.OwnerPackageId) ||
                string.IsNullOrEmpty(info.LocalProviderKey))
            {
                return null;
            }

            ShuttleExternalLaunchRuleProviderInfo sanitized =
                new ShuttleExternalLaunchRuleProviderInfo(
                    info.OwnerPackageId,
                    info.LocalProviderKey,
                    info.DisplayName,
                    info.Version,
                    info.EnabledByDefault,
                    info.Description);

            return string.IsNullOrEmpty(sanitized.ProviderKey) ? null : sanitized;
        }
    }

    internal sealed class ShuttleExternalLaunchRuleProviderRegistration
    {
        internal ShuttleExternalLaunchRuleProviderRegistration(
            IShuttleExternalLaunchRuleProvider provider,
            ShuttleExternalLaunchRuleProviderInfo info)
        {
            this.Provider = provider;
            this.Info = info;
        }

        internal IShuttleExternalLaunchRuleProvider Provider { get; private set; }
        internal ShuttleExternalLaunchRuleProviderInfo Info { get; private set; }

        internal string ProviderKey
        {
            get
            {
                return this.Info != null ? this.Info.ProviderKey : null;
            }
        }
    }
}

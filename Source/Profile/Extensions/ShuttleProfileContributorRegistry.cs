using System;
using System.Collections.Generic;

namespace CeleTech.ShuttleExtension.ModularShuttle.Profile.Extensions
{
    /// <summary>
    /// Explicit registry for profile contributors. It does not scan assemblies during
    /// profile builds; callers register known contributors during setup.
    /// Duplicate keys are deterministic: the first registration wins.
    /// </summary>
    internal sealed class ShuttleProfileContributorRegistry
    {
        private readonly Dictionary<string, IShuttleProfileContributor> contributors;

        internal ShuttleProfileContributorRegistry()
        {
            this.contributors = new Dictionary<string, IShuttleProfileContributor>(StringComparer.Ordinal);
        }

        public int Count
        {
            get
            {
                return this.contributors.Count;
            }
        }

        internal bool Register(string key, IShuttleProfileContributor contributor)
        {
            string normalizedKey = NormalizeKey(key);
            if (normalizedKey == null || contributor == null)
            {
                return false;
            }

            if (this.contributors.ContainsKey(normalizedKey))
            {
                return false;
            }

            this.contributors.Add(normalizedKey, contributor);
            return true;
        }

        internal bool TryResolve(string key, out IShuttleProfileContributor contributor)
        {
            contributor = null;
            string normalizedKey = NormalizeKey(key);
            if (normalizedKey == null)
            {
                return false;
            }

            return this.contributors.TryGetValue(normalizedKey, out contributor);
        }

        internal IShuttleProfileContributor ResolveOrNull(string key)
        {
            IShuttleProfileContributor contributor;
            return this.TryResolve(key, out contributor) ? contributor : null;
        }

        internal List<string> GetRegisteredKeysSnapshot()
        {
            List<string> keys = new List<string>(this.contributors.Keys);
            keys.Sort(StringComparer.Ordinal);
            return keys;
        }

        internal static string NormalizeKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return null;
            }

            return key.Trim();
        }
    }

    /// <summary>
    /// No-op contributor for optional/default/fallback contributor paths.
    /// </summary>
    public sealed class NullShuttleProfileContributor : IShuttleProfileContributor
    {
        private static readonly NullShuttleProfileContributor instance = new NullShuttleProfileContributor();

        private NullShuttleProfileContributor()
        {
        }

        public static NullShuttleProfileContributor Instance
        {
            get
            {
                return instance;
            }
        }

        public void Contribute(ShuttleProfileContributionContext context)
        {
        }
    }
}

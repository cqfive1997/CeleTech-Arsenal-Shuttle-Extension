using System.Collections.Generic;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Extensions
{
    /// <summary>
    /// Optional XML binding from a module Def to a registered profile contributor key.
    /// The key is static Def data only; contributor behavior still comes from code
    /// registered through the public ShuttleProfileAPI facade.
    /// This extension is not a class-name activation hook.
    /// </summary>
    public sealed class ShuttleProfileContributorDefExtension : DefModExtension
    {
        // Legacy single-key XML field. Prefer package.id/local-key values.
        public string contributorKey;

        // Optional ordered multi-key field. Values are merged after contributorKey.
        public List<string> contributorKeys;

        public List<string> GetContributorKeys()
        {
            List<string> keys = new List<string>();
            HashSet<string> seen = new HashSet<string>();

            this.AddKey(keys, seen, this.contributorKey);

            if (this.contributorKeys != null)
            {
                for (int i = 0; i < this.contributorKeys.Count; i++)
                {
                    this.AddKey(keys, seen, this.contributorKeys[i]);
                }
            }

            return keys;
        }

        private void AddKey(List<string> keys, HashSet<string> seen, string key)
        {
            if (keys == null || seen == null || string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            string normalizedKey = key.Trim();
            if (normalizedKey.Length == 0 || seen.Contains(normalizedKey))
            {
                return;
            }

            seen.Add(normalizedKey);
            keys.Add(normalizedKey);
        }
    }
}

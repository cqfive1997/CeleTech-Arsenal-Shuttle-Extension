using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Extensions
{
    /// <summary>
    /// Optional XML binding from a module Def to registered runtime system keys.
    /// The keys are static Def data only; runtime behavior still comes from code registered
    /// through ShuttleRuntimeAPI. This extension is not a class-name activation hook and
    /// never instantiates runtime systems from XML.
    /// </summary>
    public sealed class ShuttleRuntimeSystemDefExtension : DefModExtension
    {
        // Legacy single-key XML field. Prefer package.id/local-key values.
        public string runtimeSystemKey;

        // Optional ordered multi-key field. Values are merged after runtimeSystemKey.
        public List<string> runtimeSystemKeys;

        public List<string> GetRuntimeSystemKeys()
        {
            List<string> keys = new List<string>();
            HashSet<string> seen = new HashSet<string>();

            this.AddKey(keys, seen, this.runtimeSystemKey);

            if (this.runtimeSystemKeys != null)
            {
                for (int i = 0; i < this.runtimeSystemKeys.Count; i++)
                {
                    this.AddKey(keys, seen, this.runtimeSystemKeys[i]);
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

            string normalizedKey = ShuttleRuntimeSystemKeyUtility.Normalize(key);
            if (normalizedKey.Length == 0 || seen.Contains(normalizedKey))
            {
                return;
            }

            seen.Add(normalizedKey);
            keys.Add(normalizedKey);
        }
    }
}

using System.Collections.Generic;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Utilities
{
    internal sealed class ShuttleLogThrottle
    {
        internal const int DefaultIntervalTicks = 2500;
        private const int MaxTrackedKeys = 1024;

        internal static readonly ShuttleLogThrottle Global = new ShuttleLogThrottle();

        private readonly Dictionary<string, int> nextLogTickByKey =
            new Dictionary<string, int>();

        internal bool ShouldLog(string key, int intervalTicks = DefaultIntervalTicks)
        {
            int ticks = Find.TickManager != null ? Find.TickManager.TicksGame : -1;
            if (ticks < 0)
            {
                return true;
            }

            if (intervalTicks <= 0)
            {
                intervalTicks = DefaultIntervalTicks;
            }

            string normalizedKey = key ?? string.Empty;
            int nextTick;
            if (this.nextLogTickByKey.TryGetValue(normalizedKey, out nextTick) &&
                ticks < nextTick)
            {
                return false;
            }

            this.TrimIfNeeded(normalizedKey, ticks);
            this.nextLogTickByKey[normalizedKey] = ticks + intervalTicks;
            return true;
        }

        internal void Clear()
        {
            this.nextLogTickByKey.Clear();
        }

        private void TrimIfNeeded(string keyBeingLogged, int ticks)
        {
            if (this.nextLogTickByKey.Count < MaxTrackedKeys ||
                this.nextLogTickByKey.ContainsKey(keyBeingLogged))
            {
                return;
            }

            List<string> expiredKeys = null;
            foreach (KeyValuePair<string, int> pair in this.nextLogTickByKey)
            {
                if (ticks < pair.Value)
                {
                    continue;
                }

                if (expiredKeys == null)
                {
                    expiredKeys = new List<string>();
                }

                expiredKeys.Add(pair.Key);
            }

            if (expiredKeys != null)
            {
                for (int i = 0; i < expiredKeys.Count; i++)
                {
                    this.nextLogTickByKey.Remove(expiredKeys[i]);
                }
            }

            if (this.nextLogTickByKey.Count >= MaxTrackedKeys)
            {
                this.nextLogTickByKey.Clear();
            }
        }
    }
}

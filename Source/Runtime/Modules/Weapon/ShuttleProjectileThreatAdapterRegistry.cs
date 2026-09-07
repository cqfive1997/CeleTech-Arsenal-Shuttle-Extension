using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon
{
    /// <summary>
    /// Stores stateless projectile-family adapters only. It owns no map, target, assignment,
    /// weapon, or runtime-state data.
    /// </summary>
    internal sealed class ShuttleProjectileThreatAdapterRegistry
    {
        internal static readonly ShuttleProjectileThreatAdapterRegistry Shared =
            new ShuttleProjectileThreatAdapterRegistry();

        private readonly object sync = new object();
        private IShuttleProjectileThreatAdapter[] adapters;

        private ShuttleProjectileThreatAdapterRegistry()
        {
            this.adapters = new IShuttleProjectileThreatAdapter[]
            {
                new ShuttleVanillaProjectileThreatAdapter()
            };
        }

        internal bool Register(IShuttleProjectileThreatAdapter adapter)
        {
            if (adapter == null || string.IsNullOrEmpty(adapter.AdapterId))
            {
                return false;
            }

            lock (this.sync)
            {
                for (int i = 0; i < this.adapters.Length; i++)
                {
                    if (this.adapters[i].AdapterId == adapter.AdapterId)
                    {
                        return false;
                    }
                }

                IShuttleProjectileThreatAdapter[] next =
                    new IShuttleProjectileThreatAdapter[this.adapters.Length + 1];
                for (int i = 0; i < this.adapters.Length; i++)
                {
                    next[i] = this.adapters[i];
                }

                next[next.Length - 1] = adapter;
                this.adapters = next;
                return true;
            }
        }

        internal bool TryCreateThreat(Thing thing, out ShuttleProjectileThreat threat)
        {
            threat = null;
            if (thing == null || thing.Destroyed)
            {
                return false;
            }

            IShuttleProjectileThreatAdapter[] current = this.adapters;
            for (int i = 0; i < current.Length; i++)
            {
                if (current[i].TryCreateThreat(thing, out threat) && threat != null)
                {
                    return true;
                }
            }

            threat = null;
            return false;
        }
    }
}

using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Bridges
{
    /// <summary>
    /// Typed access to vanilla/Odyssey host comps.
    /// This is a small lookup snapshot, not a RimWorld ThingComp and not a behavior owner.
    /// This keeps external comp lookup out of UI and profile code.
    /// </summary>
    public sealed class ShuttleHostComps
    {
        public ThingWithComps Host;
        public CompTransporter Transporter;
        public CompShuttle Shuttle;

        public static ShuttleHostComps From(ThingWithComps host)
        {
            ShuttleHostComps comps = new ShuttleHostComps();
            comps.Host = host;

            if (host == null)
            {
                return comps;
            }

            comps.Transporter = host.TryGetComp<CompTransporter>();
            comps.Shuttle = host.TryGetComp<CompShuttle>();
            return comps;
        }
    }
}

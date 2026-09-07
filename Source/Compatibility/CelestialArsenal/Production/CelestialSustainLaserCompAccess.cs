using Verse;

namespace CeleTech.ShuttleExtension.Compatibility.CelestialArsenal
{
    /// <summary>
    /// Runtime-only typed lookup cache for the particle-lance data Comp owned by the hidden gun.
    /// </summary>
    internal sealed class CelestialSustainLaserCompAccess
    {
        private ThingWithComps cachedSource;
        private CompShuttleSustainLaserData data;

        internal CompShuttleSustainLaserData GetData(ThingWithComps source)
        {
            this.EnsureSource(source);
            return this.data;
        }

        private void EnsureSource(ThingWithComps source)
        {
            if (ReferenceEquals(this.cachedSource, source))
            {
                return;
            }

            this.cachedSource = source;
            this.data = source != null
                ? source.GetComp<CompShuttleSustainLaserData>()
                : null;
        }
    }
}

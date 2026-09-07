using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Shield.Surface
{
    /// <summary>
    /// Simple damage DTO for future host hook call sites. It carries damage context only and
    /// never exposes controller or runtime-state ownership.
    /// The Phase 3 service currently uses ref DamageInfo directly; this stays as the
    /// Phase 4 host-hook and Phase 6 visual pipeline seam.
    /// </summary>
    internal sealed class ShuttleSurfaceShieldDamageRequest
    {
        internal ShuttleSurfaceShieldDamageRequest(
            DamageInfo damageInfo,
            Thing host,
            int ticksGame)
        {
            this.DamageInfo = damageInfo;
            this.Host = host;
            this.TicksGame = ticksGame;
        }

        public DamageInfo DamageInfo;
        public Thing Host;
        public int TicksGame;
    }
}

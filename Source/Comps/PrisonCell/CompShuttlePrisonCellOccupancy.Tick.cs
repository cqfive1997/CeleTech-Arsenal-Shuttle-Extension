using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    public sealed partial class CompShuttlePrisonCellOccupancy
    {
        public override void CompTick()
        {
            base.CompTick();
            this.EnsureInitialized();
            if (this.prisonCellHeldThings == null || this.prisonCellHeldThings.Count == 0)
            {
                return;
            }

            // ThingWithComps does not tick IThingHolder contents automatically. Prisoners
            // must keep their ordinary Pawn/Need ticks while contained, matching Habitat
            // occupants and Medical Bay patients without moving them out of the holder.
            this.prisonCellHeldThings.DoTick();
        }
    }
}

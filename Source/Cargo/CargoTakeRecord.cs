using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Cargo
{
    internal sealed class CargoTakeRecord
    {
        internal CargoTakeRecord(
            CompTransporter sourceTransporter,
            ThingOwner sourceContents,
            Thing thing)
        {
            this.SourceTransporter = sourceTransporter;
            this.SourceContents = sourceContents;
            this.Thing = thing;
        }

        internal CompTransporter SourceTransporter { get; private set; }
        internal ThingOwner SourceContents { get; private set; }
        internal Thing Thing { get; private set; }
    }
}

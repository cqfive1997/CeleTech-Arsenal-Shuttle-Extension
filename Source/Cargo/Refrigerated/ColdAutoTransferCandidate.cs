using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Cargo.Refrigerated
{
    internal sealed class ColdAutoTransferCandidate
    {
        internal ColdAutoTransferCandidate(
            Thing thing,
            ThingOwner sourceOwner,
            CompTransporter sourceTransporter,
            int transporterIndex,
            int loadedIndex,
            int originalStackCount,
            float massKg)
        {
            this.Thing = thing;
            this.SourceOwner = sourceOwner;
            this.SourceTransporter = sourceTransporter;
            this.TransporterIndex = transporterIndex;
            this.LoadedIndex = loadedIndex;
            this.OriginalStackCount = originalStackCount;
            this.MassKg = massKg;
        }

        internal Thing Thing { get; private set; }
        internal ThingOwner SourceOwner { get; private set; }
        internal CompTransporter SourceTransporter { get; private set; }
        internal int TransporterIndex { get; private set; }
        internal int LoadedIndex { get; private set; }
        internal int OriginalStackCount { get; private set; }
        internal float MassKg { get; private set; }
        internal bool Consumed { get; set; }
    }
}

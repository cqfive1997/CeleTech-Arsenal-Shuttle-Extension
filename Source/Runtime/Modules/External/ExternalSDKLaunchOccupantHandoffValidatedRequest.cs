using CeleTech.ShuttleExtension.ModularShuttle.API.SDK;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.External
{
    internal sealed class ExternalSDKLaunchOccupantHandoffValidatedRequest
    {
        internal ExternalSDKLaunchOccupantHandoffValidatedRequest(
            ShuttleExternalLaunchOccupantHandoffSource source,
            Pawn pawn,
            ThingOwner sourceOwner)
        {
            this.Source = source;
            this.Pawn = pawn;
            this.SourceOwner = sourceOwner;
            this.PawnThingIdNumber = pawn != null ? pawn.thingIDNumber : -1;
            this.PawnThingId = pawn != null ? pawn.ThingID : null;
            this.PawnLabel = pawn != null ? pawn.LabelShort : null;
        }

        internal ShuttleExternalLaunchOccupantHandoffSource Source { get; private set; }
        internal Pawn Pawn { get; private set; }
        internal ThingOwner SourceOwner { get; private set; }
        internal int PawnThingIdNumber { get; private set; }
        internal string PawnThingId { get; private set; }
        internal string PawnLabel { get; private set; }
    }
}

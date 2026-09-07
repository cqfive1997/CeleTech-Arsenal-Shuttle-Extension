using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.External
{
    internal sealed class ExternalSDKLaunchOccupantHandoffPlan
    {
        internal ExternalSDKLaunchOccupantHandoffPlan(
            ExternalSDKLaunchOccupantHandoffValidatedRequest request,
            CompTransporter targetTransporter,
            ThingOwner targetContents,
            float affectedMassKg)
        {
            this.Request = request;
            this.TargetTransporter = targetTransporter;
            this.TargetContents = targetContents;
            this.AffectedMassKg = affectedMassKg;
        }

        internal ExternalSDKLaunchOccupantHandoffValidatedRequest Request { get; private set; }
        internal CompTransporter TargetTransporter { get; private set; }
        internal ThingOwner TargetContents { get; private set; }
        internal float AffectedMassKg { get; private set; }
    }
}

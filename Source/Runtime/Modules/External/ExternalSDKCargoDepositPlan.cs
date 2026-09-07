using System.Collections.Generic;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.External
{
    internal sealed class ExternalSDKCargoDepositPlan
    {
        internal ExternalSDKCargoDepositPlan(
            ExternalSDKCargoValidatedDepositRequest request,
            List<ExternalSDKCargoDepositTarget> targets,
            int affectedCount,
            float affectedMassKg,
            int cargoRegionIndex)
        {
            this.Request = request;
            this.Targets = targets ?? new List<ExternalSDKCargoDepositTarget>();
            this.AffectedCount = affectedCount > 0 ? affectedCount : 0;
            this.AffectedMassKg = affectedMassKg > 0f ? affectedMassKg : 0f;
            this.CargoRegionIndex = cargoRegionIndex;
        }

        internal ExternalSDKCargoValidatedDepositRequest Request { get; private set; }
        internal List<ExternalSDKCargoDepositTarget> Targets { get; private set; }
        internal int AffectedCount { get; private set; }
        internal float AffectedMassKg { get; private set; }
        internal int CargoRegionIndex { get; private set; }

        internal int MatchedCount
        {
            get
            {
                return this.AffectedCount;
            }
        }

        internal int AffectedStackCount
        {
            get
            {
                return this.Targets != null ? this.Targets.Count : 0;
            }
        }
    }

    internal sealed class ExternalSDKCargoDepositTarget
    {
        internal ExternalSDKCargoDepositTarget(
            int transporterIndex,
            int stackCount,
            float massKg)
        {
            this.TransporterIndex = transporterIndex;
            this.StackCount = stackCount;
            this.MassKg = massKg > 0f ? massKg : 0f;
        }

        internal int TransporterIndex { get; private set; }
        internal int StackCount { get; private set; }
        internal float MassKg { get; private set; }
    }

    internal sealed class ExternalSDKCargoCreatedRecord
    {
        internal ExternalSDKCargoCreatedRecord(
            ExternalSDKCargoDepositTarget target,
            Thing thing)
        {
            this.Target = target;
            this.Thing = thing;
        }

        internal ExternalSDKCargoDepositTarget Target { get; private set; }
        internal Thing Thing { get; private set; }
    }
}

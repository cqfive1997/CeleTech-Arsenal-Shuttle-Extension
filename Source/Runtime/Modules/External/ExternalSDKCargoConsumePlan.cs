using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.External
{
    internal sealed class ExternalSDKCargoConsumePlan
    {
        internal ExternalSDKCargoConsumePlan(
            ExternalSDKCargoValidatedConsumeRequest request,
            List<ExternalSDKCargoConsumeCandidate> candidates,
            int matchedCount,
            int affectedCount,
            float affectedMassKg)
        {
            this.Request = request;
            this.Candidates = candidates ?? new List<ExternalSDKCargoConsumeCandidate>();
            this.MatchedCount = matchedCount;
            this.AffectedCount = affectedCount;
            this.AffectedMassKg = affectedMassKg > 0f ? affectedMassKg : 0f;
        }

        internal ExternalSDKCargoValidatedConsumeRequest Request { get; private set; }
        internal List<ExternalSDKCargoConsumeCandidate> Candidates { get; private set; }
        internal int MatchedCount { get; private set; }
        internal int AffectedCount { get; private set; }
        internal float AffectedMassKg { get; private set; }

        internal int AffectedStackCount
        {
            get
            {
                return this.Candidates != null ? this.Candidates.Count : 0;
            }
        }
    }

    internal sealed class ExternalSDKCargoConsumeCandidate
    {
        internal ExternalSDKCargoConsumeCandidate(
            CargoStackRef stackRef,
            int takeCount,
            float affectedMassKg)
        {
            this.StackRef = stackRef;
            this.TakeCount = takeCount;
            this.AffectedMassKg = affectedMassKg > 0f ? affectedMassKg : 0f;
        }

        internal CargoStackRef StackRef { get; private set; }
        internal int TakeCount { get; private set; }
        internal float AffectedMassKg { get; private set; }
    }
}

using System.Collections.Generic;

namespace CeleTech.ShuttleExtension.ModularShuttle.Cargo.Refrigerated
{
    internal sealed class ColdAutoTransferCandidateSnapshot
    {
        internal ColdAutoTransferCandidateSnapshot(
            int createdTick,
            List<ColdAutoTransferCandidate> candidates,
            int scannedStackCount)
        {
            this.CreatedTick = createdTick;
            this.Candidates = candidates ?? new List<ColdAutoTransferCandidate>();
            this.ScannedStackCount = scannedStackCount;
        }

        internal int CreatedTick { get; private set; }
        internal List<ColdAutoTransferCandidate> Candidates { get; private set; }
        internal int ScannedStackCount { get; private set; }
    }
}

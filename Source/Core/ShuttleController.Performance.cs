using CeleTech.ShuttleExtension.ModularShuttle.Presentation;

namespace CeleTech.ShuttleExtension.ModularShuttle.Core
{
    public sealed partial class ShuttleController
    {
        internal int BeginPerformanceCapture()
        {
            this.GetProfileForRead();
            return this.tickProfiler.BeginDashboardCapture(
                this.profileRevision,
                this.GetTicksGameSafe());
        }

        internal void EndPerformanceCapture(int captureLeaseID)
        {
            this.tickProfiler.EndDashboardCapture(
                captureLeaseID,
                this.GetTicksGameSafe());
        }

        internal void ResetPerformanceCapture(int captureLeaseID)
        {
            this.GetProfileForRead();
            this.tickProfiler.ResetDashboardCapture(
                captureLeaseID,
                this.profileRevision,
                this.GetTicksGameSafe());
        }

        internal ShuttlePerformanceDashboardReadModel GetPerformanceDashboardReadModel()
        {
            return this.tickProfiler.GetDashboardSnapshot();
        }
    }
}

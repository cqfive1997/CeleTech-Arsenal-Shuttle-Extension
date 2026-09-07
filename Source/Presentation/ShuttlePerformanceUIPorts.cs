namespace CeleTech.ShuttleExtension.ModularShuttle.Presentation
{
    internal interface IShuttlePerformanceCaptureControlPort
    {
        int BeginPerformanceCapture();

        void EndPerformanceCapture(int captureLeaseID);

        void ResetPerformanceCapture(int captureLeaseID);
    }

    internal interface IShuttlePerformanceDashboardReadPort
    {
        ShuttlePerformanceDashboardReadModel GetPerformanceDashboardReadModel();
    }
}

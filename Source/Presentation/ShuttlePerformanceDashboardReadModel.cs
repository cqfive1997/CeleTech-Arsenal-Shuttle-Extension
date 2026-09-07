using System.Collections.Generic;

namespace CeleTech.ShuttleExtension.ModularShuttle.Presentation
{
    internal sealed class ShuttlePerformanceDashboardReadModel
    {
        internal static readonly ShuttlePerformanceDashboardReadModel Empty =
            new ShuttlePerformanceDashboardReadModel
            {
                ModuleMetrics = new List<ShuttlePerformanceModuleMetricReadModel>(),
                ControllerPhases = new List<ShuttlePerformanceControllerPhaseReadModel>(),
                RecentControllerTickMs = new List<float>()
            };

        internal bool IsCapturing;
        internal bool HasCachedSamples;
        internal int ProfileRevision;
        internal int ActiveTickSamples;
        internal int SnapshotTick;
        internal float ControllerAverageMsPerTick;
        internal float ControllerPeakMs;
        internal float ModuleAverageMsPerTick;
        internal float CoreAverageMsPerTick;
        internal float ModuleShare01;
        internal IReadOnlyList<ShuttlePerformanceModuleMetricReadModel> ModuleMetrics;
        internal IReadOnlyList<ShuttlePerformanceControllerPhaseReadModel> ControllerPhases;
        internal IReadOnlyList<float> RecentControllerTickMs;
    }

    internal sealed class ShuttlePerformanceModuleMetricReadModel
    {
        internal string ModuleInstanceID;
        internal string ModuleLabel;
        internal string RuntimeSystemKey;
        internal int TickInterval;
        internal long CallCount;
        internal float AverageMsPerTick;
        internal float AverageMsPerCall;
        internal float PeakMs;
        internal float ControllerShare01;
    }

    internal sealed class ShuttlePerformanceControllerPhaseReadModel
    {
        internal string SectionKey;
        internal long CallCount;
        internal float AverageMsPerTick;
        internal float AverageMsPerCall;
        internal float PeakMs;
        internal float ControllerShare01;
    }
}

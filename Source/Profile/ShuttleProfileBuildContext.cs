using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Profile.ExternalExpressions;
using CeleTech.ShuttleExtension.ModularShuttle.Profile.Sections;

namespace CeleTech.ShuttleExtension.ModularShuttle.Profile
{
    internal sealed class ShuttleProfileBuildContext
    {
        internal readonly List<SegmentLayoutEntry> SegmentEntries =
            new List<SegmentLayoutEntry>();

        internal readonly List<ModuleLayoutEntry> ModuleEntries =
            new List<ModuleLayoutEntry>();

        internal readonly List<ProfileBuildIssue> Issues =
            new List<ProfileBuildIssue>();

        internal readonly ShuttleProfileAccumulator Accumulator =
            new ShuttleProfileAccumulator();

        internal readonly ShuttleExternalProfileExpressionCollector ExternalExpressions =
            new ShuttleExternalProfileExpressionCollector();

        internal readonly ShuttleProfileTuning Tuning;

        internal readonly ShuttlePrisonCellSupplyConfigState PrisonCellSupplyConfig;

        internal int SegmentSlotCount;
        internal int InstalledSegmentCount;
        internal int ModuleSlotCount;
        internal int InstalledModuleCount;

        internal ShuttleProfileBuildContext(
            ShuttleProfileTuning tuning,
            ShuttlePrisonCellSupplyConfigState prisonCellSupplyConfig)
        {
            this.Tuning = tuning ?? ShuttleProfileTuning.Fallback();
            this.PrisonCellSupplyConfig = prisonCellSupplyConfig ??
                new ShuttlePrisonCellSupplyConfigState();
            this.PrisonCellSupplyConfig.EnsureInitialized();
        }
    }
}

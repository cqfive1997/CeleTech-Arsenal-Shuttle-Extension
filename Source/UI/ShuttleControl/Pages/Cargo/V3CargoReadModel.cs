using System.Collections.Generic;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Cargo
{
    internal enum V3CargoCategory
    {
        None,
        Food,
        Manufactured,
        RawResources,
        Items,
        Weapons,
        Apparel,
        Buildings,
        Chunks,
        Plants,
        Corpses
    }

    internal enum V3CargoStackSourceKind
    {
        Unknown,
        LoadedCargo,
        RefrigeratedCargo
    }

    internal sealed class V3CargoPageReadModel
    {
        internal readonly List<V3CargoBayCardModel> Bays =
            new List<V3CargoBayCardModel>();

        internal readonly List<V3CargoCategorySummaryModel> CategorySummaries =
            new List<V3CargoCategorySummaryModel>();

        internal readonly List<V3CargoStatLineModel> ItemStats =
            new List<V3CargoStatLineModel>();

        internal readonly List<V3CargoLogisticsConnectionModel> LogisticsConnections =
            new List<V3CargoLogisticsConnectionModel>();

        internal bool HasRefrigeratedCapacitySummary;
        internal string RefrigeratedCapacitySummary;
        internal string RefrigeratedCapacityTooltip;

        internal bool HasCargoLogisticsModule;
        internal bool CargoLogisticsModuleEnabled;
        internal bool CargoLogisticsSupportsItemTransfer;
        internal bool CargoLogisticsSupportsItemConsumption;
        internal bool CargoLogisticsSupportsItemDeposit;
        internal bool CargoLogisticsInternalBusPowered;
        internal string CargoLogisticsCapabilitySummary;
        internal string CargoLogisticsStatusLabel;
        internal string CargoLogisticsDescription;
    }

    internal sealed class V3CargoBayCardModel
    {
        internal string BayKey;
        internal string Label;
        internal string FilterSummary;
        internal bool IsFilterSummaryPlaceholder;
        internal bool HasPawnFilterDetails;
        internal bool AllowHumans;
        internal bool AllowAnimals;
        internal bool AllowMechs;
        internal string ItemFilterSummary;
        internal bool IsEnabled = true;
        internal bool IsRefrigerated;
        internal bool IsRecoveryBay;
        internal bool CoolingActive;
        internal string StatusText;
        internal bool HasAutoTransferDetails;
        internal bool AutoTransferEnabled;
        internal string AutoTransferFilterSummary;
        internal string InactiveReason;
        internal int RegionIndex = -1;
        internal string ModuleInstanceID;
        internal bool HasCustomAutoTransferFilter;
        internal ThingFilter ItemFilter;
        internal ThingFilter AutoTransferFilter;
        internal float UsedMassKg;
        internal float CapacityKg;
        internal int ItemCount;
        internal string CountSummary;
        internal string CountMassSummary;
        internal string MassSummary;
        internal string ModeSummary;
        internal string FilterDisplayText;
        internal string HeaderTooltip;
        internal string Tooltip;
        internal readonly List<V3CargoStackCardModel> Items =
            new List<V3CargoStackCardModel>();
    }

    internal sealed class V3CargoStackCardModel
    {
        internal string Label;
        internal string DefName;
        internal int StackCount;
        internal float MassKg;
        internal V3CargoCategory Category;
        internal Thing DisplayThing;
        internal V3CargoStackSourceKind SourceKind;
        internal int TransporterIndex = -1;
        internal int LoadedIndex = -1;
        internal int ThingIDNumber;
        internal string ModuleInstanceID;
        internal int ColdIndex = -1;
        internal int VanillaSortThingCategory = int.MaxValue;
        internal float VanillaSortListPriority = float.MaxValue;
        internal int VanillaSortCategoryIndex = int.MaxValue;
        internal float VanillaSortMarketValue = float.MaxValue;
        internal readonly List<V3CargoStackMemberModel> Members =
            new List<V3CargoStackMemberModel>();
    }

    internal sealed class V3CargoStackMemberModel
    {
        internal V3CargoStackSourceKind SourceKind;
        internal string DefName;
        internal int StackCount;
        internal int TransporterIndex = -1;
        internal int LoadedIndex = -1;
        internal int ThingIDNumber;
        internal string ModuleInstanceID;
        internal int ColdIndex = -1;
    }

    internal sealed class V3CargoCategorySummaryModel
    {
        internal V3CargoCategory Category;
        internal string Label;
        internal int ThingCount;
    }

    internal sealed class V3CargoStatLineModel
    {
        internal V3CargoCategory Category;
        internal string Label;
        internal string DefName;
        internal int StackCount;
        internal int ThingCount;
        internal Thing DisplayThing;
    }

    internal sealed class V3CargoLogisticsConnectionModel
    {
        internal string Key;
        internal string Label;
        internal string IconKey;
        internal string StatusKey;
        internal string StatusLabel;
        internal string Description;
        internal string Tooltip;
        internal string ResolvedIconKey;
        internal bool Installed;
        internal bool Enabled;
        internal bool Connected;
        internal bool IsPlaceholder;
    }
}

using System.Collections.Generic;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Cargo
{
    internal enum ShuttleCargoActionCategory
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

    internal enum ShuttleCargoStackActionSourceKind
    {
        Unknown,
        LoadedCargo,
        RefrigeratedCargo
    }

    internal sealed class ShuttleCargoStackActionTarget
    {
        internal string Label;
        internal string DefName;
        internal int StackCount;
        internal float MassKg;
        internal ShuttleCargoActionCategory Category;
        internal Thing DisplayThing;
        internal ShuttleCargoStackActionSourceKind SourceKind;
        internal int TransporterIndex = -1;
        internal int LoadedIndex = -1;
        internal int ThingIDNumber;
        internal string ModuleInstanceID;
        internal int ColdIndex = -1;
        internal readonly List<ShuttleCargoStackMemberActionTarget> Members =
            new List<ShuttleCargoStackMemberActionTarget>();
    }

    internal sealed class ShuttleCargoStackMemberActionTarget
    {
        internal ShuttleCargoStackActionSourceKind SourceKind;
        internal string DefName;
        internal int StackCount;
        internal int TransporterIndex = -1;
        internal int LoadedIndex = -1;
        internal int ThingIDNumber;
        internal string ModuleInstanceID;
        internal int ColdIndex = -1;
    }

    internal sealed class ShuttleCargoBayActionTarget
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
        internal readonly List<ShuttleCargoStackActionTarget> Items =
            new List<ShuttleCargoStackActionTarget>();
    }

    internal sealed class ShuttleCargoPageActionContext
    {
        internal readonly List<ShuttleCargoBayActionTarget> Bays =
            new List<ShuttleCargoBayActionTarget>();

        internal bool HasCargoLogisticsModule;
        internal bool CargoLogisticsModuleEnabled;
        internal bool CargoLogisticsSupportsItemTransfer;
        internal bool CargoLogisticsSupportsItemConsumption;
        internal bool CargoLogisticsSupportsItemDeposit;
        internal bool CargoLogisticsInternalBusPowered;
        internal string CargoLogisticsCapabilitySummary;
    }
}

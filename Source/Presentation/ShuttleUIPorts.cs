using CeleTech.ShuttleExtension.ModularShuttle.Bridges;
using CeleTech.ShuttleExtension.ModularShuttle.API.UI;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation.External;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using System.Collections.Generic;
using RimWorld;
using Verse;
using System;

namespace CeleTech.ShuttleExtension.ModularShuttle.Presentation
{
    [Flags]
    internal enum ShuttleControlReadDetail
    {
        None = 0,
        MedicalActions = 1,
        PrisonCellActions = 2,
        All = MedicalActions | PrisonCellActions
    }

    public interface IShuttleControlReadPort
    {
        ShuttleControlReadModel BuildControlReadModel();
    }

    internal interface IShuttleCachedControlReadPort
    {
        ShuttleControlReadModel BuildControlReadModel(
            ShuttleCargoSnapshot cargoSnapshot,
            ShuttleControlReadDetail detail);
    }

    internal interface IShuttleExternalModuleUIReadPort
    {
        IReadOnlyList<ExternalModuleUIReadModel> BuildExternalModuleUIBadgeReadModels();

        IReadOnlyList<ExternalModuleUIReadModel> BuildExternalModuleUIReadModels();

        ExternalModuleUIRuntimeSummary BuildExternalModuleUIRuntimeSummary();

        ShuttleExternalModulePanelContext BuildExternalModulePanelContext(
            string moduleInstanceID,
            string runtimeSystemKey,
            string panelKey);
    }

    public interface IShuttleModuleInstallEligibilityReadPort
    {
        bool CanInstallModuleForUI(
            string segmentInstanceID,
            string moduleSlotID,
            ShuttleModuleBaseDef moduleDef,
            bool allowOccupiedSlot,
            out string failureReason);
    }

    public interface IShuttleMedicalBayActionPort
    {
        ThingWithComps GetMedicalBayActionHost();
    }

    public interface IShuttleWeaponBayReadPort
    {
        ShuttleWeaponBayReadModel BuildWeaponBayReadModel();
    }

    public interface IShuttleAutoWorkTableReadPort
    {
        ShuttleAutoWorkTableReadModel BuildAutoWorkTableReadModel();
    }

    public interface IShuttleCargoReadPort
    {
        ShuttleCargoSnapshot BuildCargoSnapshot();

        ShuttleCargoRegionReadModel GetCargoRegionSettings(int regionIndex, int regionCount);
    }

    public interface IShuttleLoadCargoReadPort
    {
        ShuttleLoadCargoReadModel BuildLoadCargoReadModel();

        ShuttleLoadCargoReadModel RefreshLoadCargoReadModel();

        bool HasQueuedLoads();

        float GetAvailableMass();

        int GetSelectedCount(
            List<TransferableOneWay> passengerTransferables,
            List<TransferableOneWay> cargoTransferables);

        float GetSelectedMassKg(
            List<TransferableOneWay> passengerTransferables,
            List<TransferableOneWay> cargoTransferables);
    }

    internal interface IShuttleCargoUnloadReadPort
    {
        ShuttleCargoUnloadProgressSnapshot BuildCargoUnloadProgressSnapshot();
    }

}

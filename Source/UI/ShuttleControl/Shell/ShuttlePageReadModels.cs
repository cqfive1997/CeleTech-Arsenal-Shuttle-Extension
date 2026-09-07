using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation.External;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Issues;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.ReadModels.Pawns;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Medical;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell
{
    internal sealed class ShuttlePageReadModels
    {
        internal ShuttleControlReadModel ControlModel;
        internal ShuttleCargoSnapshot CargoSnapshot;
        internal IReadOnlyList<ShuttleCargoRegionReadModel> CargoRegionSettings;
        internal ShuttleWeaponBayReadModel WeaponBayModel;
        internal V3MedicalPageReadModel MedicalModel;
        internal ShuttleAutoWorkTableReadModel WorkTableModel;
        internal IReadOnlyList<ShuttleIssueReadModel> CurrentIssues;
        internal IReadOnlyList<ShuttleIssueReadModel> LaunchDiagnostics;
        internal IReadOnlyList<ExternalModuleUIReadModel> ExternalModuleModels;
        internal ShuttlePawnPresenceSnapshot PawnPresenceSnapshot;
        internal ShuttlePawnDynamicStatusSnapshot PawnDynamicStatusSnapshot;
        internal int CargoSnapshotRevision;
    }
}

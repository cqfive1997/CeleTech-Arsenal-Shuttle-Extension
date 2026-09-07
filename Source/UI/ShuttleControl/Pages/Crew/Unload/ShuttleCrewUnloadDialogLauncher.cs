using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Commands;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Crew;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Actions.Crew;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Crew
{
    internal sealed class ShuttleCrewUnloadDialogLauncher :
        IShuttleCrewUnloadDialogActions
    {
        private readonly V3CrewReadModelBuilder crewReadModelBuilder =
            new V3CrewReadModelBuilder();
        private readonly V3CrewUnloadSelectionBuilder selectionBuilder =
            new V3CrewUnloadSelectionBuilder();

        public void Open(
            V3CrewPageData pageData,
            IShuttleCrewLoadedCrewUIActions loadedCrewActions,
            IShuttleCrewHabitatUIActions habitatActions,
            IShuttleCrewMechChargerUIActions mechChargerActions)
        {
            List<V3CrewUnloadOption> options =
                this.selectionBuilder.BuildCrew(
                    pageData,
                    loadedCrewActions,
                    habitatActions,
                    mechChargerActions);
            if (options.Count == 0)
            {
                ShuttleUICommandFeedback.ShowNeutral(
                    ShuttleUIText.Tr("CT_Shuttle_Crew_UnloadNoCandidates"));
                return;
            }

            Find.WindowStack.Add(new Dialog_ShuttleCrewUnloadV3(
                options,
                loadedCrewActions,
                habitatActions,
                mechChargerActions));
        }

        internal void OpenCrewFromPorts(
            IShuttleControlReadPort controlReadPort,
            IShuttleCargoReadPort cargoReadPort,
            IShuttleCommandExecutor commandExecutor)
        {
            ShuttleCargoSnapshot cargoSnapshot = cargoReadPort != null
                ? cargoReadPort.BuildCargoSnapshot()
                : new ShuttleCargoSnapshot();
            IShuttleCachedControlReadPort cachedControlReadPort =
                controlReadPort as IShuttleCachedControlReadPort;
            ShuttleControlReadModel controlModel = cachedControlReadPort != null
                ? cachedControlReadPort.BuildControlReadModel(
                    cargoSnapshot,
                    ShuttleControlReadDetail.All)
                : (controlReadPort != null
                    ? controlReadPort.BuildControlReadModel()
                    : new ShuttleControlReadModel());
            V3CrewPageData pageData =
                this.crewReadModelBuilder.Build(controlModel, cargoSnapshot);

            this.Open(
                pageData,
                new ShuttleCrewLoadedCrewUIActions(commandExecutor),
                new ShuttleCrewHabitatUIActions(commandExecutor),
                new ShuttleCrewMechChargerUIActions(commandExecutor));
        }
    }
}

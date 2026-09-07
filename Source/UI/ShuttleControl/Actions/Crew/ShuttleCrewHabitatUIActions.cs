using CeleTech.ShuttleExtension.ModularShuttle.Commands;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Crew;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Actions.Crew
{
    internal sealed class ShuttleCrewHabitatUIActions : IShuttleCrewHabitatUIActions
    {
        private readonly IShuttleCommandExecutor commandExecutor;

        internal ShuttleCrewHabitatUIActions(IShuttleCommandExecutor commandExecutor)
        {
            this.commandExecutor = commandExecutor;
        }

        public bool CanEjectHabitatOccupants(ShuttleControlReadModel model)
        {
            return this.commandExecutor != null &&
                model != null &&
                model.Habitat != null &&
                model.Habitat.HasAnyOccupants &&
                model.Habitat.CanEjectOccupants;
        }

        public bool EjectHabitatOccupants(ShuttleControlReadModel model)
        {
            if (!this.CanEjectHabitatOccupants(model))
            {
                this.ShowReject(this.GetHabitatEjectTooltip(model));
                return false;
            }

            ShuttleCommandResult result = this.commandExecutor.Execute(
                new EjectHabitatOccupantsCommand());
            return this.ShowResult(result);
        }

        public string GetHabitatEjectTooltip(ShuttleControlReadModel model)
        {
            if (this.commandExecutor == null)
            {
                return this.Tr("CT_Shuttle_Command_ExecutorUnavailable");
            }

            if (model == null || model.Habitat == null || !model.Habitat.HasHabitat)
            {
                return this.Tr("CT_Shuttle_HabitatPanel_NotInstalled");
            }

            if (!model.Habitat.HasAnyOccupants)
            {
                return this.Tr("CT_Shuttle_HabitatPanel_NoOccupants");
            }

            if (!model.Habitat.CanEjectOccupants)
            {
                return this.Tr("CT_Shuttle_HabitatPanel_CannotEject");
            }

            return this.Tr("CT_Shuttle_Habitat_EjectOccupants");
        }

        public bool CanEjectHabitatOccupant(int pawnThingID)
        {
            return this.commandExecutor != null && pawnThingID > 0;
        }

        public bool EjectHabitatOccupant(int pawnThingID)
        {
            if (!this.CanEjectHabitatOccupant(pawnThingID))
            {
                this.ShowReject(this.GetHabitatOccupantEjectTooltip(pawnThingID));
                return false;
            }

            ShuttleCommandResult result = this.commandExecutor.Execute(
                new EjectHabitatOccupantCommand(pawnThingID));
            return this.ShowResult(result);
        }

        public string GetHabitatOccupantEjectTooltip(int pawnThingID)
        {
            if (this.commandExecutor == null)
            {
                return this.Tr("CT_Shuttle_Command_ExecutorUnavailable");
            }

            if (pawnThingID <= 0)
            {
                return this.Tr("CT_Shuttle_Crew_EjectHabitatOccupantInvalidTooltip");
            }

            return this.Tr("CT_Shuttle_Crew_EjectHabitatOccupantTooltip");
        }

        private bool ShowResult(ShuttleCommandResult result)
        {
            return ShuttleUICommandFeedback.ShowResult(result);
        }

        private void ShowReject(string message)
        {
            ShuttleUICommandFeedback.ShowReject(message);
        }

        private string Tr(string key)
        {
            return ShuttleUIText.Tr(key);
        }
    }
}

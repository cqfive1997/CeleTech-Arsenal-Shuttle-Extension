using CeleTech.ShuttleExtension.ModularShuttle.Commands;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Crew;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Actions.Crew
{
    internal sealed class ShuttleCrewMechChargerUIActions :
        IShuttleCrewMechChargerUIActions
    {
        private readonly IShuttleCommandExecutor commandExecutor;

        internal ShuttleCrewMechChargerUIActions(IShuttleCommandExecutor commandExecutor)
        {
            this.commandExecutor = commandExecutor;
        }

        public bool CanEjectChargingMech(int mechThingID)
        {
            return this.commandExecutor != null && mechThingID > 0;
        }

        public bool EjectChargingMech(int mechThingID)
        {
            if (!this.CanEjectChargingMech(mechThingID))
            {
                this.ShowReject(this.GetChargingMechEjectTooltip(mechThingID));
                return false;
            }

            ShuttleCommandResult result = this.commandExecutor.Execute(
                new EjectMechChargerOccupantCommand(mechThingID));
            return this.ShowResult(result);
        }

        public string GetChargingMechEjectTooltip(int mechThingID)
        {
            if (this.commandExecutor == null)
            {
                return this.Tr("CT_Shuttle_Command_ExecutorUnavailable");
            }

            if (mechThingID <= 0)
            {
                return this.Tr("CT_Shuttle_Crew_EjectChargingMechInvalidTooltip");
            }

            return this.Tr("CT_Shuttle_Crew_EjectChargingMechTooltip");
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

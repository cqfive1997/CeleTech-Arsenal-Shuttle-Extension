using CeleTech.ShuttleExtension.ModularShuttle.Commands;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Crew;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Actions.Crew
{
    internal sealed class ShuttleCrewMedicalPatientUIActions :
        IShuttleCrewMedicalPatientUIActions
    {
        private readonly IShuttleCommandExecutor commandExecutor;

        internal ShuttleCrewMedicalPatientUIActions(IShuttleCommandExecutor commandExecutor)
        {
            this.commandExecutor = commandExecutor;
        }

        public bool CanEjectMedicalPatient(int patientThingID)
        {
            return this.commandExecutor != null && patientThingID > 0;
        }

        public bool EjectMedicalPatient(int patientThingID)
        {
            if (!this.CanEjectMedicalPatient(patientThingID))
            {
                this.ShowReject(this.GetMedicalPatientEjectTooltip(patientThingID));
                return false;
            }

            ShuttleCommandResult result = this.commandExecutor.Execute(
                new EjectMedicalBayPatientsCommand(patientThingID));
            return this.ShowResult(result);
        }

        public string GetMedicalPatientEjectTooltip(int patientThingID)
        {
            if (this.commandExecutor == null)
            {
                return this.Tr("CT_Shuttle_Command_ExecutorUnavailable");
            }

            if (patientThingID <= 0)
            {
                return this.Tr("CT_Shuttle_Crew_EjectMedicalPatientInvalidTooltip");
            }

            return this.Tr("CT_Shuttle_Crew_EjectMedicalPatientTooltip");
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

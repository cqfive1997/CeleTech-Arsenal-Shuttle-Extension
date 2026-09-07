using CeleTech.ShuttleExtension.ModularShuttle.Commands;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Medical;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Actions.Medical
{
    internal sealed class ShuttleMedicalOccupantUIActions : IShuttleMedicalOccupantUIActions
    {
        private readonly IShuttleCommandExecutor commandExecutor;

        internal ShuttleMedicalOccupantUIActions(IShuttleCommandExecutor commandExecutor)
        {
            this.commandExecutor = commandExecutor;
        }

        public bool CanEjectPatient(ShuttleMedicalPatientActionTarget patient)
        {
            return this.commandExecutor != null &&
                patient != null &&
                patient.PatientThingID > 0 &&
                !patient.IsActiveProcedurePatient;
        }

        public bool EjectPatient(ShuttleMedicalPatientActionTarget patient)
        {
            if (!this.CanEjectPatient(patient))
            {
                this.ShowReject(this.GetEjectPatientTooltip(patient));
                return false;
            }

            ShuttleCommandResult result = this.commandExecutor.Execute(
                new EjectMedicalBayPatientsCommand(patient.PatientThingID));
            return this.ShowResult(result);
        }

        public string GetEjectPatientTooltip(ShuttleMedicalPatientActionTarget patient)
        {
            if (this.commandExecutor == null)
            {
                return this.Tr("CT_Shuttle_Command_ExecutorUnavailable");
            }

            if (patient == null || patient.PatientThingID <= 0)
            {
                return this.Tr("CT_Shuttle_MedicalBay_InvalidPatient");
            }

            if (patient.IsActiveProcedurePatient)
            {
                return this.Tr("CT_Shuttle_MedicalProcedure_PatientActive");
            }

            return this.Tr("CT_Shuttle_Medical_RemoveFromBay");
        }

        public bool CanEjectAllPatients(ShuttleMedicalPageActionContext pageContext)
        {
            return this.commandExecutor != null &&
                pageContext != null &&
                pageContext.HasMedicalBay &&
                !pageContext.HasActiveProcedure &&
                pageContext.PatientCount > 0;
        }

        public bool EjectAllPatients(ShuttleMedicalPageActionContext pageContext)
        {
            if (!this.CanEjectAllPatients(pageContext))
            {
                this.ShowReject(this.GetAllPatientsEjectTooltip(pageContext));
                return false;
            }

            ShuttleCommandResult result = this.commandExecutor.Execute(
                new EjectMedicalBayPatientsCommand());
            return this.ShowResult(result);
        }

        public string GetAllPatientsEjectTooltip(ShuttleMedicalPageActionContext pageContext)
        {
            if (this.commandExecutor == null)
            {
                return this.Tr("CT_Shuttle_Command_ExecutorUnavailable");
            }

            if (pageContext == null || !pageContext.HasMedicalBay)
            {
                return this.Tr("CT_Shuttle_Medical_NotInstalled");
            }

            if (pageContext.HasActiveProcedure)
            {
                return this.Tr("CT_Shuttle_MedicalProcedure_TreatmentBlocked");
            }

            if (pageContext.PatientCount <= 0)
            {
                return this.Tr("CT_Shuttle_Medical_NoPatients");
            }

            return this.Tr("CT_Shuttle_Medical_EjectAll");
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

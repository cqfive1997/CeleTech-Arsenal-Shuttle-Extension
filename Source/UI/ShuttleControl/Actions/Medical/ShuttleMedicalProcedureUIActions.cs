using CeleTech.ShuttleExtension.ModularShuttle.Commands;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Medical;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Actions.Medical
{
    internal sealed class ShuttleMedicalProcedureUIActions : IShuttleMedicalProcedureUIActions
    {
        private readonly IShuttleCommandExecutor commandExecutor;

        internal ShuttleMedicalProcedureUIActions(IShuttleCommandExecutor commandExecutor)
        {
            this.commandExecutor = commandExecutor;
        }

        public bool CanCancelMedicalProcedure(ShuttleMedicalProcedureActionTarget procedure)
        {
            return this.commandExecutor != null &&
                procedure != null &&
                procedure.HasActiveProcedure;
        }

        public bool CancelMedicalProcedure(ShuttleMedicalProcedureActionTarget procedure)
        {
            if (!this.CanCancelMedicalProcedure(procedure))
            {
                this.ShowReject(this.GetCancelMedicalProcedureTooltip(procedure));
                return false;
            }

            int procedureID = procedure.ActiveProcedureID > 0
                ? procedure.ActiveProcedureID
                : -1;
            ShuttleCommandResult result = this.commandExecutor.Execute(
                new CancelMedicalBayProcedureCommand(procedureID));
            return this.ShowResult(result);
        }

        public string GetCancelMedicalProcedureTooltip(
            ShuttleMedicalProcedureActionTarget procedure)
        {
            if (this.commandExecutor == null)
            {
                return this.Tr("CT_Shuttle_Command_ExecutorUnavailable");
            }

            if (procedure == null || !procedure.HasActiveProcedure)
            {
                return this.Tr("CT_Shuttle_MedicalProcedure_Status_None");
            }

            return procedure.ActiveProcedureStatus == "RecoveryRequired"
                ? this.Tr("CT_Shuttle_MedicalProcedure_RecoverTooltip")
                : this.Tr("CT_Shuttle_MedicalProcedure_CancelTooltip");
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

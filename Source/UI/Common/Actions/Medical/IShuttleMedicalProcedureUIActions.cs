namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Medical
{
    internal interface IShuttleMedicalProcedureUIActions
    {
        bool CanCancelMedicalProcedure(ShuttleMedicalProcedureActionTarget procedure);

        bool CancelMedicalProcedure(ShuttleMedicalProcedureActionTarget procedure);

        string GetCancelMedicalProcedureTooltip(ShuttleMedicalProcedureActionTarget procedure);
    }
}

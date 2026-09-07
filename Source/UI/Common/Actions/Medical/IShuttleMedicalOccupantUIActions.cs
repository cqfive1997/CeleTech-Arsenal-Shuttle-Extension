namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Medical
{
    internal interface IShuttleMedicalOccupantUIActions
    {
        bool CanEjectPatient(ShuttleMedicalPatientActionTarget patient);

        bool EjectPatient(ShuttleMedicalPatientActionTarget patient);

        string GetEjectPatientTooltip(ShuttleMedicalPatientActionTarget patient);

        bool CanEjectAllPatients(ShuttleMedicalPageActionContext pageContext);

        bool EjectAllPatients(ShuttleMedicalPageActionContext pageContext);

        string GetAllPatientsEjectTooltip(ShuttleMedicalPageActionContext pageContext);
    }
}

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Medical
{
    internal interface IShuttleMedicalSurgeryUIActions
    {
        bool CanScheduleSurgery(ShuttleMedicalPatientActionTarget patient);

        void OpenSurgeryMenu(ShuttleMedicalPatientActionTarget patient);

        string GetScheduleSurgeryTooltip(ShuttleMedicalPatientActionTarget patient);

        void OpenSurgeryDoctorMenu(
            ShuttleMedicalPatientActionTarget patient,
            ShuttleMedicalSurgeryOptionActionTarget surgeryOption);
    }
}

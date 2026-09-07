namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Medical
{
    internal interface IShuttleMedicalTreatmentUIActions
    {
        bool CanTreatPatient(ShuttleMedicalPatientActionTarget patient);

        void OpenDoctorMenu(
            ShuttleMedicalPatientActionTarget patient,
            bool? useAvailableMedicineFilter);

        string GetTreatPatientTooltip(ShuttleMedicalPatientActionTarget patient);
    }
}

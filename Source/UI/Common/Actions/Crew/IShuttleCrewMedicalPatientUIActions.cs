namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Crew
{
    internal interface IShuttleCrewMedicalPatientUIActions
    {
        bool CanEjectMedicalPatient(int patientThingID);

        bool EjectMedicalPatient(int patientThingID);

        string GetMedicalPatientEjectTooltip(int patientThingID);
    }
}

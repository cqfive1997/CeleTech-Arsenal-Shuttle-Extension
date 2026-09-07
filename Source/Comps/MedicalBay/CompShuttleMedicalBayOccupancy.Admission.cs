using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    public sealed partial class CompShuttleMedicalBayOccupancy
    {
        internal bool TryAdmitPatientForTestOrCommand(
            Pawn patient,
            string admissionMode,
            out string failureReason)
        {
            return MedicalBayOccupancyAdmissionService.TryAdmitPatientForTestOrCommand(
                this,
                patient,
                admissionMode,
                out failureReason);
        }

        internal bool TryAdmitSelfPatient(Pawn patient, out string failureReason)
        {
            return MedicalBayOccupancyAdmissionService.TryAdmitSelfPatient(
                this,
                patient,
                out failureReason);
        }

        internal bool TryAdmitCarriedPatient(Pawn carrier, Pawn patient, out string failureReason)
        {
            return MedicalBayOccupancyAdmissionService.TryAdmitCarriedPatient(
                this,
                carrier,
                patient,
                out failureReason);
        }
    }
}

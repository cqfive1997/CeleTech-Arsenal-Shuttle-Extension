using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    public sealed partial class CompShuttleMedicalBayOccupancy
    {
        internal static class MedicalBayOccupancyReconciler
        {
            internal static void ReconcilePatientRecordsToHeldPawns(CompShuttleMedicalBayOccupancy owner)
            {
                owner.EnsureInitialized();
                SharedPatientRecordService.ReconcilePatientRecordsToHeldPawns(
                    owner.medicalHeldThings,
                    owner.patientRecords,
                    owner.ReleaseAllReservationsForPatient,
                    ShuttleTickUtility.TicksGameOrMinusOne());
            }

            internal static void RemoveInvalidPatientRecords(CompShuttleMedicalBayOccupancy owner)
            {
                SharedPatientRecordService.RemoveInvalidPatientRecords(
                    owner.medicalHeldThings,
                    owner.patientRecords,
                    owner.ReleaseAllReservationsForPatient);
            }
        }
    }
}

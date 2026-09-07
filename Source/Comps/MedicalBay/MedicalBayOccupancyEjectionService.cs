using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    public sealed partial class CompShuttleMedicalBayOccupancy
    {
        internal static class MedicalBayOccupancyEjectionService
        {
            internal static bool TryEjectPatient(
                CompShuttleMedicalBayOccupancy owner,
                Pawn patient,
                out string failureReason)
            {
                failureReason = null;
                if (patient == null)
                {
                    failureReason = "CT_Shuttle_MedicalBay_InvalidPatient".Translate().ToString();
                    return false;
                }

                Map map = owner.parent != null ? owner.parent.Map : null;
                return TryEjectPatient(owner, patient, map, out failureReason);
            }

            internal static bool TryEjectAllPatients(
                CompShuttleMedicalBayOccupancy owner,
                out string failureReason)
            {
                Map map = owner.parent != null ? owner.parent.Map : null;
                return TryEjectAllPatients(owner, map, out failureReason);
            }

            internal static bool TryEjectAllPatients(
                CompShuttleMedicalBayOccupancy owner,
                Map map,
                out string failureReason)
            {
                failureReason = null;
                if (map == null)
                {
                    failureReason = "CT_Shuttle_MedicalBay_EjectFailed".Translate().ToString();
                    return false;
                }

                owner.EnsureInitialized();
                owner.ReconcilePatientRecordsToHeldPawns();

                bool allEjected = true;
                for (int i = owner.patientRecords.Count - 1; i >= 0; i--)
                {
                    ShuttleMedicalPatientRecord record = owner.patientRecords[i];
                    Pawn patient = record != null ? record.Pawn : null;
                    if (patient == null || !owner.IsHeldPawn(patient))
                    {
                        owner.ReleaseAllReservationsForPatient(record != null ? record.PawnThingID : -1);
                        owner.patientRecords.RemoveAt(i);
                        continue;
                    }

                    string patientFailure;
                    if (!TryEjectPatient(owner, patient, map, out patientFailure))
                    {
                        allEjected = false;
                        failureReason = patientFailure;
                    }
                }

                if (!allEjected && string.IsNullOrEmpty(failureReason))
                {
                    failureReason = "CT_Shuttle_MedicalBay_EjectFailed".Translate().ToString();
                }

                return allEjected && !owner.HasPatients;
            }

            internal static void EjectAllPatientsSafely(
                CompShuttleMedicalBayOccupancy owner,
                Map map)
            {
                string failureReason;
                if (owner.HasPatients &&
                    !TryEjectAllPatients(owner, map, out failureReason) &&
                    Prefs.DevMode &&
                    ShuttleLogThrottle.Global.ShouldLog(owner.GetMedicalBayLogKey("safe-eject", failureReason)))
                {
                    Log.Warning("[CeleTech Shuttle] Medical Bay safe eject did not complete: " + failureReason);
                }
            }

            internal static void PreserveDestroyedHolderContents(
                CompShuttleMedicalBayOccupancy owner,
                DestroyMode mode)
            {
                owner.EnsureInitialized();
                if (owner.HasPatients)
                {
                    Log.Error(
                        "[CeleTech Shuttle] Shuttle Medical Bay occupancy was destroyed without a map while patients were inside. " +
                        "Preserving contained pawns instead of killing them. destroyMode=" + mode);
                    return;
                }

                owner.patientRecords.Clear();
            }

            internal static bool TryEjectPatient(
                CompShuttleMedicalBayOccupancy owner,
                Pawn patient,
                Map map,
                out string failureReason)
            {
                failureReason = null;
                owner.EnsureInitialized();
                if (!owner.IsHeldPawn(patient))
                {
                    owner.RemovePatientRecord(patient);
                    owner.ReleaseAllReservationsForPatient(patient != null ? patient.thingIDNumber : -1);
                    return true;
                }

                if (map == null)
                {
                    failureReason = "CT_Shuttle_MedicalBay_EjectFailed".Translate().ToString();
                    return false;
                }

                Thing resultingThing;
                if (!owner.medicalHeldThings.TryDrop(
                    patient,
                    owner.GetEjectCell(map),
                    map,
                    ThingPlaceMode.Near,
                    out resultingThing,
                    null,
                    null))
                {
                    failureReason = "CT_Shuttle_MedicalBay_EjectFailed".Translate().ToString();
                    return false;
                }

                owner.RemovePatientRecord(patient);
                owner.ReleaseAllReservationsForPatient(patient.thingIDNumber);
                return true;
            }
        }
    }
}

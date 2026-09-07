using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Medical;
using CeleTech.ShuttleExtension.ModularShuttle.Profile.Sections;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    public sealed partial class CompShuttleMedicalBayOccupancy
    {
        internal static class MedicalBayOccupancyAdmissionService
        {
            internal static bool TryAdmitPatientForTestOrCommand(
                CompShuttleMedicalBayOccupancy owner,
                Pawn patient,
                string admissionMode,
                out string failureReason)
            {
                // M3A only accepts already-spawned patients through a test/command seam.
                // Carried admission uses TryAdmitCarriedPatient; downed/carried patients
                // must not reuse this spawned-only path.
                failureReason = null;
                if (!CanAdmitPatient(owner, patient, out failureReason))
                {
                    return false;
                }

                owner.EnsureInitialized();
                if (owner.ContainsPatient(patient))
                {
                    return true;
                }

                if (!TryPreparePassengerForAdmission(owner, patient, out failureReason))
                {
                    return false;
                }

                Map map = patient.Map;
                IntVec3 fallbackCell = owner.GetEjectCell(map);
                CompShuttleHolderLaunchTransferState transferState = owner.GetHolderTransferState();
                PawnHolderTransferResult transferResult =
                    ShuttlePawnHolderTransferUtility.TryMoveSpawnedPawnIntoHolderSafely(
                    patient,
                    owner.medicalHeldThings,
                    map,
                    fallbackCell,
                    transferState != null ? transferState.EmergencyRecoveryThings : null,
                    "MedicalBay admit patient mode=" +
                        (admissionMode ?? "null"));
                if (transferResult.Status != PawnHolderTransferStatus.MovedToDestination)
                {
                    HandleFailedPawnHolderTransfer(owner, patient, transferResult, "MedicalBay admit patient");
                    failureReason = GetPawnHolderTransferFailureReason(
                        transferResult,
                        "CT_Shuttle_MedicalBay_AdmitFailed".Translate().ToString());
                    return false;
                }

                owner.patientRecords.Add(new ShuttleMedicalPatientRecord(
                    patient,
                    string.IsNullOrEmpty(admissionMode) ? "TestOrCommand" : admissionMode,
                    ShuttleTickUtility.TicksGameOrMinusOne(),
                    string.Empty,
                    string.Empty));
                owner.ReleaseAdmissionReservation(patient);

                if (transferResult.WasSelected)
                {
                    Find.Selector.Select(owner.parent, false, false);
                }

                return true;
            }

            internal static bool TryAdmitSelfPatient(
                CompShuttleMedicalBayOccupancy owner,
                Pawn patient,
                out string failureReason)
            {
                failureReason = null;
                string useFailureReason;
                if (!MedicalBayAdmissionValidator.CanUseMedicalBayForSelfAdmit(
                    patient,
                    owner.parent,
                    out useFailureReason))
                {
                    failureReason = useFailureReason;
                    return false;
                }

                return TryAdmitPatientForTestOrCommand(owner, patient, "SelfAdmit", out failureReason);
            }

            internal static bool TryAdmitCarriedPatient(
                CompShuttleMedicalBayOccupancy owner,
                Pawn carrier,
                Pawn patient,
                out string failureReason)
            {
                failureReason = null;
                string useFailureReason;
                if (!MedicalBayAdmissionValidator.CanAdmitCarriedPatient(
                    carrier,
                    patient,
                    owner.parent,
                    out useFailureReason))
                {
                    failureReason = useFailureReason;
                    return false;
                }

                owner.EnsureInitialized();
                owner.ReconcilePatientRecordsToHeldPawns();
                if (owner.ContainsPatient(patient))
                {
                    return true;
                }

                if (!TryPreparePassengerForAdmission(owner, patient, out failureReason))
                {
                    return false;
                }

                Map fallbackMap = carrier != null ? carrier.Map : (owner.parent != null ? owner.parent.Map : null);
                IntVec3 fallbackCell = owner.GetEjectCell(fallbackMap);
                bool added = owner.medicalHeldThings.TryAddOrTransfer(patient, true);
                if (!added)
                {
                    // TryAddOrTransfer should leave the patient in the carrier's carryTracker on
                    // failure. If a future RimWorld API change detaches it anyway, recover to map
                    // or the shuttle transfer emergency owner.
                    bool patientSecured =
                        MedicalBayAdmissionValidator.IsCarrierCarryingPatient(carrier, patient) ||
                        (patient != null && (patient.Spawned || patient.holdingOwner != null));
                    if (!MedicalBayAdmissionValidator.IsCarrierCarryingPatient(carrier, patient) &&
                        patient != null &&
                        !patient.Destroyed &&
                        !patient.Spawned &&
                        fallbackMap != null &&
                        fallbackCell.IsValid)
                    {
                        GenSpawn.Spawn(patient, fallbackCell, fallbackMap, WipeMode.Vanish);
                        patientSecured = patient.Spawned;
                    }

                    if (!patientSecured &&
                        patient != null &&
                        !patient.Destroyed)
                    {
                        CompShuttleHolderLaunchTransferState transferState = owner.GetHolderTransferState();
                        ShuttleTransferRecoveryStatus recoveryStatus;
                        string recoveryFailureReason = null;
                        if (transferState != null &&
                            transferState.TryRecoverTransferThing(
                                patient,
                                "MedicalBay carried admit failed after carrier handoff.",
                                null,
                                null,
                                fallbackMap,
                                fallbackCell,
                                out recoveryStatus,
                                out recoveryFailureReason))
                        {
                            Log.Warning("[CeleTech Shuttle] MedicalBay carried admit recovered patient after holder add failure. status=" +
                                recoveryStatus +
                                " patient=" +
                                patient +
                                " reason=" +
                                (recoveryFailureReason ?? "null"));
                        }
                        else
                        {
                            Log.Error("[CeleTech Shuttle] MedicalBay carried admit failed and patient final owner could not be confirmed. patient=" +
                                patient +
                                " recovery=" +
                                (recoveryFailureReason ?? "transfer state unavailable"));
                        }
                    }

                    failureReason = "CT_Shuttle_MedicalBay_CarriedAdmitFailed".Translate().ToString();
                    return false;
                }

                owner.patientRecords.Add(new ShuttleMedicalPatientRecord(
                    patient,
                    "CarriedAdmit",
                    ShuttleTickUtility.TicksGameOrMinusOne(),
                    MedicalEvacuationUtility.GetMedevacReasonLabel(patient),
                    string.Empty));
                owner.ReleaseAdmissionReservation(patient);

                return true;
            }

            private static void HandleFailedPawnHolderTransfer(
                CompShuttleMedicalBayOccupancy owner,
                Pawn pawn,
                PawnHolderTransferResult transferResult,
                string operation)
            {
                if (transferResult == null)
                {
                    Log.Error("[CeleTech Shuttle] MedicalBay pawn holder transfer returned null result. operation=" +
                        (operation ?? "null"));
                    return;
                }

                if (transferResult.Status == PawnHolderTransferStatus.FatalOwnerless)
                {
                    Log.Error("[CeleTech Shuttle] MedicalBay pawn holder transfer fatal. operation=" +
                        (operation ?? "null") +
                        " context=" +
                        (transferResult.DebugContext ?? "null") +
                        " reason=" +
                        (transferResult.FailureReason ?? "null"));
                    return;
                }

                if (transferResult.Status == PawnHolderTransferStatus.RecoveredToEmergencyOwner)
                {
                    Log.Warning("[CeleTech Shuttle] MedicalBay pawn holder transfer recovered pawn to emergency owner; no patient record was created. operation=" +
                        (operation ?? "null") +
                        " context=" +
                        (transferResult.DebugContext ?? "null") +
                        " reason=" +
                        (transferResult.FailureReason ?? "null"));
                    if (transferResult.WasSelected && owner.parent != null)
                    {
                        Find.Selector.Select(owner.parent, false, false);
                    }

                    return;
                }

                if (transferResult.Status == PawnHolderTransferStatus.RecoveredToMap &&
                    transferResult.WasSelected &&
                    pawn != null &&
                    pawn.Spawned)
                {
                    Find.Selector.Select(pawn, false, false);
                }
            }

            private static string GetPawnHolderTransferFailureReason(
                PawnHolderTransferResult transferResult,
                string defaultReason)
            {
                if (transferResult != null && !string.IsNullOrEmpty(transferResult.FailureReason))
                {
                    return transferResult.FailureReason;
                }

                return defaultReason;
            }

            private static bool CanAdmitPatient(
                CompShuttleMedicalBayOccupancy owner,
                Pawn patient,
                out string failureReason)
            {
                failureReason = null;
                if (patient == null || patient.Destroyed || patient.Dead)
                {
                    failureReason = "CT_Shuttle_MedicalBay_InvalidPatient".Translate().ToString();
                    return false;
                }

                if (!MedicalBayPatientAccessPolicy.CanReceiveCare(
                    patient,
                    out failureReason))
                {
                    return false;
                }

                if (owner.parent == null || !owner.parent.Spawned || owner.parent.Map == null)
                {
                    failureReason = "CT_Shuttle_MedicalBay_NoMedicalBay".Translate().ToString();
                    return false;
                }

                owner.EnsureInitialized();
                owner.ReconcilePatientRecordsToHeldPawns();

                MedicalBayProfile medicalBay;
                if (!owner.TryGetMedicalBayProfile(out medicalBay) || !medicalBay.HasMedicalBay)
                {
                    failureReason = "CT_Shuttle_MedicalBay_NoMedicalBay".Translate().ToString();
                    return false;
                }

                if (medicalBay.MedicalPatientSlots <= 0)
                {
                    failureReason = "CT_Shuttle_MedicalBay_NoSlots".Translate().ToString();
                    return false;
                }

                if (owner.ContainsPatient(patient))
                {
                    failureReason = "CT_Shuttle_MedicalBay_PatientAlreadyHeld".Translate().ToString();
                    return false;
                }

                if (!patient.Spawned || patient.Map != owner.parent.Map)
                {
                    failureReason = "CT_Shuttle_MedicalBay_PatientAlreadyHeld".Translate().ToString();
                    return false;
                }

                if (owner.PatientCount + owner.CountAdmissionReservationsExcluding(patient.thingIDNumber) >= medicalBay.MedicalPatientSlots)
                {
                    failureReason = "CT_Shuttle_MedicalBay_Full".Translate().ToString();
                    return false;
                }

                return true;
            }

            private static bool TryPreparePassengerForAdmission(
                CompShuttleMedicalBayOccupancy owner,
                Pawn patient,
                out string failureReason)
            {
                failureReason = null;
                ShuttleController controller;
                if (owner == null ||
                    !MedicalBayAdmissionValidator.TryGetShuttleController(
                        owner.parent,
                        out controller))
                {
                    failureReason =
                        "CT_Shuttle_MedicalBay_NoMedicalBay".Translate().ToString();
                    return false;
                }

                return controller.TryPreparePassengerForMedicalAdmission(
                    patient,
                    out failureReason);
            }
        }
    }
}

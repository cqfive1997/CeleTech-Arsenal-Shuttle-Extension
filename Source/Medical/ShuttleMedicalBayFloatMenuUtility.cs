using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Profile.Sections;
using RimWorld;
using Verse;
using Verse.AI;

namespace CeleTech.ShuttleExtension.ModularShuttle.Medical
{
    /// <summary>
    /// Shuttle-owned float-menu entries for Medical Bay admission jobs. These create jobs only;
    /// admission still happens through job/service/occupancy boundaries.
    /// </summary>
    internal static class ShuttleMedicalBayFloatMenuUtility
    {
        private const string DevTendSpikeOptionLabel = "[DEV] Test tending contained MedicalBay patient";
        private const string DevInventoryMedicineTendSpikeOptionLabel =
            "[DEV] Test tending contained MedicalBay patient with doctor inventory medicine";
        private const string DevLoadedCargoMedicineTendSpikeOptionLabel =
            "[DEV] Test tending contained MedicalBay patient with loaded cargo medicine";

        private static readonly MedicalBayTreatmentService TreatmentService =
            new MedicalBayTreatmentService();
        private static readonly ShuttleMedicalBayOccupancyService OccupancyService =
            new ShuttleMedicalBayOccupancyService();

        internal static IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selectedPawn, Thing shuttleHost)
        {
            if (selectedPawn == null || shuttleHost == null)
            {
                yield break;
            }

            ShuttleProfile profile;
            MedicalBayProfile medicalBay;
            if (!MedicalBayAdmissionValidator.TryGetMedicalBayProfile(shuttleHost, out profile, out medicalBay) ||
                medicalBay == null ||
                !medicalBay.HasMedicalBay)
            {
                yield break;
            }

            foreach (FloatMenuOption devOption in GetDevTendSpikeOptions(selectedPawn, shuttleHost))
            {
                yield return devOption;
            }

            foreach (FloatMenuOption devOption in GetDevInventoryMedicineTendSpikeOptions(selectedPawn, shuttleHost))
            {
                yield return devOption;
            }

            foreach (FloatMenuOption devOption in GetDevLoadedCargoMedicineTendSpikeOptions(selectedPawn, shuttleHost))
            {
                yield return devOption;
            }

            foreach (FloatMenuOption tendOption in GetTendPatientOptions(selectedPawn, shuttleHost))
            {
                yield return tendOption;
            }

            string failReason;
            JobDef selfAdmitJobDef = DefDatabase<JobDef>.GetNamedSilentFail(MedicalBayAdmissionValidator.EnterMedicalBayJobDefName);
            if (MedicalBayAdmissionValidator.CanUseMedicalBayForSelfAdmit(selectedPawn, shuttleHost, out failReason))
            {
                if (selfAdmitJobDef == null)
                {
                    yield return new FloatMenuOption("CT_Shuttle_MedicalBay_AdmitFailed".Translate().ToString(), null);
                }
                else
                {
                    yield return new FloatMenuOption("CT_Shuttle_MedicalBay_Enter".Translate().ToString(), delegate
                    {
                        string orderFailure;
                        if (!OccupancyService.TryAssignSelfAdmissionJob(
                            shuttleHost as ThingWithComps,
                            selectedPawn,
                            out orderFailure))
                        {
                            Messages.Message(orderFailure, MessageTypeDefOf.RejectInput, false);
                        }
                    });
                }
            }
            else if (MedicalBayAdmissionValidator.PatientRequiresCarriedAdmission(selectedPawn))
            {
                yield return new FloatMenuOption(failReason, null);
            }

            JobDef carryJobDef = DefDatabase<JobDef>.GetNamedSilentFail(MedicalBayAdmissionValidator.CarryPatientToMedicalBayJobDefName);
            List<Pawn> carryablePatients = FindCarryablePatients(selectedPawn, shuttleHost);
            if (carryablePatients.Count == 0)
            {
                if (CanShowNoCarryablePatients(selectedPawn))
                {
                    yield return new FloatMenuOption("CT_Shuttle_MedicalBay_NoCarryablePatients".Translate().ToString(), null);
                }

                yield break;
            }

            if (carryJobDef == null)
            {
                yield return new FloatMenuOption("CT_Shuttle_MedicalBay_CarriedAdmitFailed".Translate().ToString(), null);
                yield break;
            }

            for (int i = 0; i < carryablePatients.Count; i++)
            {
                Pawn patient = carryablePatients[i];
                if (patient == null)
                {
                    continue;
                }

                yield return new FloatMenuOption(
                    "CT_Shuttle_MedicalBay_CarryPatientOption".Translate(patient.LabelShortCap).ToString(),
                    delegate
                    {
                        string orderFailure;
                        if (!OccupancyService.TryAssignCarryAdmissionJob(
                            shuttleHost as ThingWithComps,
                            selectedPawn,
                            patient,
                            out orderFailure))
                        {
                            Messages.Message(orderFailure, MessageTypeDefOf.RejectInput, false);
                        }
                    });
            }
        }

        private static List<Pawn> FindCarryablePatients(Pawn carrier, Thing shuttleHost)
        {
            List<Pawn> patients = new List<Pawn>();
            if (carrier == null || carrier.Map == null || shuttleHost == null || shuttleHost.Map != carrier.Map)
            {
                return patients;
            }

            IReadOnlyList<Pawn> spawnedPawns = carrier.Map.mapPawns != null
                ? carrier.Map.mapPawns.AllPawnsSpawned
                : null;
            if (spawnedPawns == null)
            {
                return patients;
            }

            for (int i = 0; i < spawnedPawns.Count; i++)
            {
                Pawn patient = spawnedPawns[i];
                if (patient == null || patient == carrier)
                {
                    continue;
                }

                string failReason;
                if (MedicalBayAdmissionValidator.CanContinueCarryAdmission(
                    carrier,
                    patient,
                    shuttleHost,
                    out failReason))
                {
                    patients.Add(patient);
                }
            }

            patients.Sort(ComparePatientsForMedicalBayCarry);
            return patients;
        }

        private static IEnumerable<FloatMenuOption> GetTendPatientOptions(Pawn doctor, Thing shuttleHost)
        {
            ThingWithComps shuttleWithComps = shuttleHost as ThingWithComps;
            if (doctor == null || shuttleWithComps == null)
            {
                yield break;
            }

            CompShuttleMedicalBayOccupancy occupancy;
            if (!MedicalBayAdmissionValidator.TryGetMedicalBayOccupancy(shuttleHost, out occupancy) ||
                occupancy == null ||
                !occupancy.HasPatients)
            {
                yield break;
            }

            JobDef noMedicineTendJobDef = DefDatabase<JobDef>.GetNamedSilentFail(
                MedicalBayAdmissionValidator.TendMedicalBayPatientJobDefName);
            JobDef withMedicineTendJobDef = DefDatabase<JobDef>.GetNamedSilentFail(
                MedicalBayAdmissionValidator.TendMedicalBayPatientWithMedicineJobDefName);
            List<Pawn> patients = occupancy.HeldPatients;
            for (int i = 0; i < patients.Count; i++)
            {
                Pawn patient = patients[i];
                if (patient == null)
                {
                    continue;
                }

                string failReason;
                if (!TreatmentService.CanTendContainedPatientNoMedicine(
                    shuttleWithComps,
                    doctor,
                    patient.thingIDNumber,
                    out failReason))
                {
                    continue;
                }

                int patientThingID = patient.thingIDNumber;
                string patientLabel = patient.LabelShortCap;
                if (TreatmentService.CanTendContainedPatientWithAnyAllowedMedicine(
                    shuttleWithComps,
                    doctor,
                    patientThingID,
                    out failReason))
                {
                    if (withMedicineTendJobDef == null)
                    {
                        yield return new FloatMenuOption(
                            "CT_Shuttle_MedicalBay_TreatmentFailed".Translate("CT_Shuttle_MedicalBay_TendPatientWithMedicineJobLabel".Translate()).ToString(),
                            null);
                    }
                    else
                    {
                        yield return new FloatMenuOption(
                            "CT_Shuttle_MedicalBay_TendPatientWithMedicineOption".Translate(patientLabel).ToString(),
                            delegate
                            {
                                Job job = JobMaker.MakeJob(withMedicineTendJobDef, shuttleHost);
                                job.count = patientThingID;
                                doctor.jobs.TryTakeOrderedJob(job);
                            });
                    }
                }
                else if (TreatmentService.HasAnyDoctorInventoryMedicine(doctor) ||
                    TreatmentService.HasAnyLoadedCargoMedicine(shuttleWithComps))
                {
                    yield return new FloatMenuOption(
                        "CT_Shuttle_MedicalBay_TendPatientWithMedicineOption".Translate(patientLabel).ToString() +
                        " - " +
                        (string.IsNullOrEmpty(failReason)
                            ? "CT_Shuttle_MedicalBay_NoAvailableMedicine".Translate().ToString()
                            : failReason),
                        null);
                }

                if (noMedicineTendJobDef == null)
                {
                    yield return new FloatMenuOption(
                        "CT_Shuttle_MedicalBay_TreatmentFailed".Translate("CT_Shuttle_MedicalBay_TendPatientJobLabel".Translate()).ToString(),
                        null);
                    continue;
                }

                yield return new FloatMenuOption(
                    "CT_Shuttle_MedicalBay_TendPatientOption".Translate(patientLabel).ToString(),
                    delegate
                    {
                        Job job = JobMaker.MakeJob(noMedicineTendJobDef, shuttleHost);
                        job.count = patientThingID;
                        doctor.jobs.TryTakeOrderedJob(job);
                    });
            }
        }

        private static int ComparePatientsForMedicalBayCarry(Pawn left, Pawn right)
        {
            int leftPriority = MedicalEvacuationUtility.GetMedevacPriority(left);
            int rightPriority = MedicalEvacuationUtility.GetMedevacPriority(right);
            int priorityCompare = rightPriority.CompareTo(leftPriority);
            if (priorityCompare != 0)
            {
                return priorityCompare;
            }

            string leftLabel = left != null ? left.LabelShort : string.Empty;
            string rightLabel = right != null ? right.LabelShort : string.Empty;
            return string.Compare(leftLabel, rightLabel, System.StringComparison.Ordinal);
        }

        private static bool CanShowNoCarryablePatients(Pawn carrier)
        {
            if (carrier == null || carrier.Destroyed || carrier.Dead || !carrier.Spawned)
            {
                return false;
            }

            return !carrier.Downed && !carrier.Drafted && carrier.MentalState == null;
        }

        private static IEnumerable<FloatMenuOption> GetDevTendSpikeOptions(Pawn doctor, Thing shuttleHost)
        {
            if (!Prefs.DevMode)
            {
                yield break;
            }

            ThingWithComps shuttleWithComps = shuttleHost as ThingWithComps;
            if (shuttleWithComps == null)
            {
                yield break;
            }

            CompShuttleMedicalBayOccupancy occupancy;
            if (!MedicalBayAdmissionValidator.TryGetMedicalBayOccupancy(shuttleHost, out occupancy) ||
                occupancy == null ||
                !occupancy.HasPatients)
            {
                yield break;
            }

            List<Pawn> patients = occupancy.HeldPatients;
            for (int i = 0; i < patients.Count; i++)
            {
                Pawn patient = patients[i];
                if (patient == null)
                {
                    continue;
                }

                int patientThingID = patient.thingIDNumber;
                string patientLabel = patient.LabelShortCap;
                yield return new FloatMenuOption(
                    DevTendSpikeOptionLabel + ": " + patientLabel,
                    delegate
                    {
                        TryRunDevTendSpike(shuttleWithComps, doctor, patientThingID, patientLabel);
                    });
            }
        }

        private static void TryRunDevTendSpike(
            ThingWithComps shuttleHost,
            Pawn doctor,
            int patientThingID,
            string patientLabel)
        {
            string failReason;
            if (TreatmentService.TryTendContainedPatientNoMedicineForSpike(
                shuttleHost,
                doctor,
                patientThingID,
                out failReason))
            {
                const string successMessage = "MedicalBay tend test succeeded: contained patient tended and remained held.";
                Messages.Message(successMessage, MessageTypeDefOf.PositiveEvent, false);
                Log.Message("[CeleTech Shuttle] " + successMessage + " patient=" + patientLabel);
                return;
            }

            string message = "MedicalBay tend test failed: " +
                (string.IsNullOrEmpty(failReason) ? "unknown reason" : failReason);
            Messages.Message(message, MessageTypeDefOf.RejectInput, false);
            Log.Warning("[CeleTech Shuttle] " + message + " patient=" + patientLabel);
        }

        private static IEnumerable<FloatMenuOption> GetDevInventoryMedicineTendSpikeOptions(
            Pawn doctor,
            Thing shuttleHost)
        {
            if (!Prefs.DevMode)
            {
                yield break;
            }

            ThingWithComps shuttleWithComps = shuttleHost as ThingWithComps;
            if (shuttleWithComps == null)
            {
                yield break;
            }

            CompShuttleMedicalBayOccupancy occupancy;
            if (!MedicalBayAdmissionValidator.TryGetMedicalBayOccupancy(shuttleHost, out occupancy) ||
                occupancy == null ||
                !occupancy.HasPatients)
            {
                yield break;
            }

            List<Pawn> patients = occupancy.HeldPatients;
            for (int i = 0; i < patients.Count; i++)
            {
                Pawn patient = patients[i];
                if (patient == null)
                {
                    continue;
                }

                string failReason;
                if (!TreatmentService.CanTendContainedPatientNoMedicine(
                    shuttleWithComps,
                    doctor,
                    patient.thingIDNumber,
                    out failReason))
                {
                    continue;
                }

                int patientThingID = patient.thingIDNumber;
                string patientLabel = patient.LabelShortCap;
                yield return new FloatMenuOption(
                    DevInventoryMedicineTendSpikeOptionLabel + ": " + patientLabel,
                    delegate
                    {
                        TryRunDevInventoryMedicineTendSpike(
                            shuttleWithComps,
                            doctor,
                            patientThingID,
                            patientLabel);
                    });
            }
        }

        private static void TryRunDevInventoryMedicineTendSpike(
            ThingWithComps shuttleHost,
            Pawn doctor,
            int patientThingID,
            string patientLabel)
        {
            string failReason;
            if (TreatmentService.TryTendContainedPatientWithDoctorInventoryMedicineForSpike(
                shuttleHost,
                doctor,
                patientThingID,
                out failReason))
            {
                string successMessage =
                    "MedicalBay inventory medicine tend test succeeded: contained patient tended and remained held. Check log for medicine stack/destroy details.";
                Messages.Message(successMessage, MessageTypeDefOf.PositiveEvent, false);
                Log.Message("[CeleTech Shuttle] " + successMessage + " patient=" + patientLabel);
                return;
            }

            string message = "MedicalBay inventory medicine tend test failed: " +
                (string.IsNullOrEmpty(failReason) ? "unknown reason" : failReason);
            Messages.Message(message, MessageTypeDefOf.RejectInput, false);
            Log.Warning("[CeleTech Shuttle] " + message + " patient=" + patientLabel);
        }

        private static IEnumerable<FloatMenuOption> GetDevLoadedCargoMedicineTendSpikeOptions(
            Pawn doctor,
            Thing shuttleHost)
        {
            if (!Prefs.DevMode)
            {
                yield break;
            }

            ThingWithComps shuttleWithComps = shuttleHost as ThingWithComps;
            if (shuttleWithComps == null)
            {
                yield break;
            }

            CompShuttleMedicalBayOccupancy occupancy;
            if (!MedicalBayAdmissionValidator.TryGetMedicalBayOccupancy(shuttleHost, out occupancy) ||
                occupancy == null ||
                !occupancy.HasPatients)
            {
                yield break;
            }

            List<Pawn> patients = occupancy.HeldPatients;
            for (int i = 0; i < patients.Count; i++)
            {
                Pawn patient = patients[i];
                if (patient == null)
                {
                    continue;
                }

                string failReason;
                if (!TreatmentService.CanTendContainedPatientNoMedicine(
                    shuttleWithComps,
                    doctor,
                    patient.thingIDNumber,
                    out failReason))
                {
                    continue;
                }

                int patientThingID = patient.thingIDNumber;
                string patientLabel = patient.LabelShortCap;
                yield return new FloatMenuOption(
                    DevLoadedCargoMedicineTendSpikeOptionLabel + ": " + patientLabel,
                    delegate
                    {
                        TryRunDevLoadedCargoMedicineTendSpike(
                            shuttleWithComps,
                            doctor,
                            patientThingID,
                            patientLabel);
                    });
            }
        }

        private static void TryRunDevLoadedCargoMedicineTendSpike(
            ThingWithComps shuttleHost,
            Pawn doctor,
            int patientThingID,
            string patientLabel)
        {
            string failReason;
            if (TreatmentService.TryTendContainedPatientWithLoadedCargoMedicineForSpike(
                shuttleHost,
                doctor,
                patientThingID,
                out failReason))
            {
                string successMessage =
                    "MedicalBay loaded cargo medicine tend test succeeded: contained patient tended and remained held. Check log for cargo medicine stack/destroy details.";
                Messages.Message(successMessage, MessageTypeDefOf.PositiveEvent, false);
                Log.Message("[CeleTech Shuttle] " + successMessage + " patient=" + patientLabel);
                return;
            }

            string message = "MedicalBay loaded cargo medicine tend test failed: " +
                (string.IsNullOrEmpty(failReason) ? "unknown reason" : failReason);
            Messages.Message(message, MessageTypeDefOf.RejectInput, false);
            Log.Warning("[CeleTech Shuttle] " + message + " patient=" + patientLabel);
        }
    }
}

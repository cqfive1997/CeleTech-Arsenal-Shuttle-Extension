using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.AI;
using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Profile.Sections;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using RimWorld;
using Verse;
using Verse.AI;

namespace CeleTech.ShuttleExtension.ModularShuttle.Medical
{
    /// <summary>
    /// Shared, read-only Medical Bay admission validator used by commands, services,
    /// occupancy, jobs, and float-menu helpers. It does not move pawns or mutate medical state.
    /// </summary>
    internal static class MedicalBayAdmissionValidator
    {
        internal const string EnterMedicalBayJobDefName = "CT_Shuttle_EnterMedicalBay";
        internal const string CarryPatientToMedicalBayJobDefName = "CT_Shuttle_CarryPatientToMedicalBay";
        internal const string TendMedicalBayPatientJobDefName = "CT_Shuttle_TendMedicalBayPatient";
        internal const string TendMedicalBayPatientWithMedicineJobDefName =
            "CT_Shuttle_TendMedicalBayPatientWithMedicine";

        internal static bool TryGetShuttleController(Thing shuttleHost, out ShuttleController controller)
        {
            controller = null;
            ThingWithComps shuttleWithComps = shuttleHost as ThingWithComps;
            if (shuttleWithComps == null)
            {
                return false;
            }

            CompModularShuttleCore core = shuttleWithComps.TryGetComp<CompModularShuttleCore>();
            if (core == null || core.Controller == null)
            {
                return false;
            }

            controller = core.Controller;
            return true;
        }

        internal static bool TryGetMedicalBayOccupancy(
            Thing shuttleHost,
            out CompShuttleMedicalBayOccupancy occupancy)
        {
            occupancy = null;
            ThingWithComps shuttleWithComps = shuttleHost as ThingWithComps;
            if (shuttleWithComps == null)
            {
                return false;
            }

            occupancy = shuttleWithComps.TryGetComp<CompShuttleMedicalBayOccupancy>();
            return occupancy != null;
        }

        internal static bool TryGetMedicalBayProfile(
            Thing shuttleHost,
            out ShuttleProfile profile,
            out MedicalBayProfile medicalBay)
        {
            profile = null;
            medicalBay = null;

            ShuttleController controller;
            if (!TryGetShuttleController(shuttleHost, out controller))
            {
                return false;
            }

            profile = controller.GetProfileForRead();
            medicalBay = profile != null ? profile.MedicalBay : null;
            return medicalBay != null && medicalBay.HasMedicalBay;
        }

        internal static bool IsMedicalBayPowered(Thing shuttleHost)
        {
            ShuttleController controller;
            if (!TryGetShuttleController(shuttleHost, out controller))
            {
                return false;
            }

            ShuttlePowerRuntimeSnapshot powerSnapshot = controller.BuildPowerRuntimeSnapshot();
            return powerSnapshot != null && powerSnapshot.InternalBusPowered;
        }

        internal static bool TryGetPoweredMedicalBayProfile(
            Thing shuttleHost,
            out ShuttleProfile profile,
            out MedicalBayProfile medicalBay)
        {
            if (!TryGetMedicalBayProfile(shuttleHost, out profile, out medicalBay))
            {
                return false;
            }

            return IsMedicalBayPowered(shuttleHost);
        }

        internal static bool CanUseMedicalBayForSelfAdmit(
            Pawn patient,
            Thing shuttleHost,
            out string failReason)
        {
            failReason = null;
            if (patient == null || patient.Destroyed || patient.Dead)
            {
                failReason = "CT_Shuttle_MedicalBay_InvalidPatient".Translate().ToString();
                return false;
            }

            if (!MedicalBayPatientAccessPolicy.CanReceiveCare(patient, out failReason) ||
                !ShuttleOnboardDeviceUsePolicy.AllowsColonistFacilityUse(
                    patient,
                    shuttleHost,
                    out failReason))
            {
                return false;
            }

            if (shuttleHost == null ||
                !shuttleHost.Spawned ||
                shuttleHost.Map == null ||
                shuttleHost.Faction != Faction.OfPlayer)
            {
                failReason = "CT_Shuttle_MedicalBay_NoMedicalBay".Translate().ToString();
                return false;
            }

            if (!patient.Spawned || patient.Map == null || patient.Map != shuttleHost.Map)
            {
                failReason = "CT_Shuttle_MedicalBay_PatientAlreadyHeld".Translate().ToString();
                return false;
            }

            if (patient.Downed)
            {
                failReason = "CT_Shuttle_MedicalBay_CannotSelfAdmitDowned".Translate().ToString();
                return false;
            }

            if (patient.Drafted || patient.MentalState != null)
            {
                failReason = "CT_Shuttle_MedicalBay_CannotSelfAdmitUnavailable".Translate().ToString();
                return false;
            }

            if (!CanMove(patient))
            {
                failReason = "CT_Shuttle_MedicalBay_CannotSelfAdmitMovingImpaired".Translate().ToString();
                return false;
            }

            ShuttleProfile profile;
            MedicalBayProfile medicalBay;
            if (!TryGetMedicalBayProfile(shuttleHost, out profile, out medicalBay))
            {
                failReason = "CT_Shuttle_MedicalBay_NoMedicalBay".Translate().ToString();
                return false;
            }

            if (!IsMedicalBayPowered(shuttleHost))
            {
                failReason = "CT_Shuttle_MedicalBay_Unpowered".Translate().ToString();
                return false;
            }

            if (medicalBay.MedicalPatientSlots <= 0)
            {
                failReason = "CT_Shuttle_MedicalBay_NoSlots".Translate().ToString();
                return false;
            }

            CompShuttleMedicalBayOccupancy occupancy;
            if (!TryGetMedicalBayOccupancy(shuttleHost, out occupancy))
            {
                failReason = "CT_Shuttle_MedicalBay_NoMedicalBay".Translate().ToString();
                return false;
            }

            if (occupancy.ContainsPatient(patient))
            {
                failReason = "CT_Shuttle_MedicalBay_PatientAlreadyHeld".Translate().ToString();
                return false;
            }

            if (CountCurrentMedicalBaySelfAdmitUsers(shuttleHost, patient) >= medicalBay.MedicalPatientSlots)
            {
                failReason = "CT_Shuttle_MedicalBay_Full".Translate().ToString();
                return false;
            }

            if (!patient.CanReach(shuttleHost, PathEndMode.InteractionCell, Danger.Deadly, false, false, TraverseMode.ByPawn))
            {
                failReason = "CT_Shuttle_MedicalBay_CannotSelfAdmitUnreachable".Translate().ToString();
                return false;
            }

            if (!patient.CanReserve(shuttleHost, Max(1, medicalBay.MedicalPatientSlots), 1, null, false))
            {
                failReason = "CT_Shuttle_MedicalBay_CannotSelfAdmitUnreachable".Translate().ToString();
                return false;
            }

            return true;
        }

        internal static bool CanCarryPatientToMedicalBay(
            Pawn carrier,
            Pawn patient,
            Thing shuttleHost,
            out string failReason,
            Pawn ignoredJobPawn = null)
        {
            failReason = null;
            if (!CanUseCarrier(carrier, out failReason))
            {
                return false;
            }

            if (!CanUsePatientForCarriedAdmission(
                carrier,
                patient,
                shuttleHost,
                out failReason))
            {
                return false;
            }

            Thing carriedThing = carrier.carryTracker.CarriedThing;
            if (carriedThing != null && carriedThing != patient)
            {
                failReason = "CT_Shuttle_MedicalBay_CarrierUnavailable".Translate().ToString();
                return false;
            }

            bool carrierAlreadyCarriesPatient = carriedThing == patient;
            if (!CanUsePatientLocationForCarriedAdmission(patient, carrierAlreadyCarriesPatient, out failReason))
            {
                return false;
            }

            if (!CanUseShuttleForMedicalAdmission(shuttleHost, out MedicalBayProfile medicalBay, out failReason))
            {
                return false;
            }

            CompShuttleMedicalBayOccupancy occupancy;
            if (!TryGetMedicalBayOccupancy(shuttleHost, out occupancy))
            {
                failReason = "CT_Shuttle_MedicalBay_NoMedicalBay".Translate().ToString();
                return false;
            }

            if (occupancy.ContainsPatient(patient))
            {
                failReason = "CT_Shuttle_MedicalBay_PatientAlreadyHeld".Translate().ToString();
                return false;
            }

            if (CountCurrentMedicalBayAdmissionUsers(shuttleHost, patient, ignoredJobPawn) >= medicalBay.MedicalPatientSlots)
            {
                failReason = "CT_Shuttle_MedicalBay_Full".Translate().ToString();
                return false;
            }

            if (carrier.Map != shuttleHost.Map)
            {
                failReason = "CT_Shuttle_MedicalBay_CannotCarryPatient".Translate().ToString();
                return false;
            }

            if (!carrierAlreadyCarriesPatient)
            {
                if (patient.Map == null || patient.Map != carrier.Map)
                {
                    failReason = "CT_Shuttle_MedicalBay_InvalidPatient".Translate().ToString();
                    return false;
                }

                if (!carrier.CanReach(patient, PathEndMode.Touch, Danger.Deadly, false, false, TraverseMode.ByPawn))
                {
                    failReason = "CT_Shuttle_MedicalBay_CannotSelfAdmitUnreachable".Translate().ToString();
                    return false;
                }

                if (!carrier.CanReserve(patient, 1, -1, null, false))
                {
                    failReason = "CT_Shuttle_MedicalBay_PatientReserved".Translate().ToString();
                    return false;
                }
            }

            if (!carrier.CanReach(shuttleHost, PathEndMode.InteractionCell, Danger.Deadly, false, false, TraverseMode.ByPawn))
            {
                failReason = "CT_Shuttle_MedicalBay_CannotSelfAdmitUnreachable".Translate().ToString();
                return false;
            }

            if (!carrier.CanReserve(shuttleHost, Max(1, medicalBay.MedicalPatientSlots), 1, null, false))
            {
                failReason = "CT_Shuttle_MedicalBay_CannotSelfAdmitUnreachable".Translate().ToString();
                return false;
            }

            return true;
        }

        internal static bool CanContinueSelfAdmission(
            Pawn patient,
            Thing shuttleHost,
            out string failReason)
        {
            failReason = null;
            if (patient == null || patient.Destroyed || patient.Dead)
            {
                failReason = "CT_Shuttle_MedicalBay_InvalidPatient".Translate().ToString();
                return false;
            }

            if (!MedicalBayPatientAccessPolicy.CanReceiveCare(patient, out failReason) ||
                !ShuttleOnboardDeviceUsePolicy.AllowsColonistFacilityUse(
                    patient,
                    shuttleHost,
                    out failReason))
            {
                return false;
            }

            MedicalBayProfile medicalBay;
            if (!CanUseShuttleForMedicalAdmission(shuttleHost, out medicalBay, out failReason) ||
                !patient.Spawned ||
                patient.Map == null ||
                patient.Map != shuttleHost.Map ||
                patient.Downed ||
                patient.Drafted ||
                patient.MentalState != null ||
                !CanMove(patient))
            {
                if (string.IsNullOrEmpty(failReason))
                {
                    failReason = "CT_Shuttle_MedicalBay_CannotSelfAdmitUnavailable".Translate().ToString();
                }

                return false;
            }

            CompShuttleMedicalBayOccupancy occupancy;
            if (!TryGetMedicalBayOccupancy(shuttleHost, out occupancy) ||
                occupancy.ContainsPatient(patient))
            {
                failReason = "CT_Shuttle_MedicalBay_PatientAlreadyHeld".Translate().ToString();
                return false;
            }

            return true;
        }

        internal static bool CanContinueCarryAdmission(
            Pawn carrier,
            Pawn patient,
            Thing shuttleHost,
            out string failReason)
        {
            failReason = null;
            if (!CanUseCarrier(carrier, out failReason) ||
                !CanUsePatientForCarriedAdmission(
                    carrier,
                    patient,
                    shuttleHost,
                    out failReason))
            {
                return false;
            }

            Thing carriedThing = carrier.carryTracker.CarriedThing;
            if (carriedThing != null && carriedThing != patient)
            {
                failReason = "CT_Shuttle_MedicalBay_CarrierUnavailable".Translate().ToString();
                return false;
            }

            bool alreadyCarried = carriedThing == patient;
            if (!CanUsePatientLocationForCarriedAdmission(
                patient,
                alreadyCarried,
                out failReason))
            {
                return false;
            }

            MedicalBayProfile medicalBay;
            if (!CanUseShuttleForMedicalAdmission(shuttleHost, out medicalBay, out failReason) ||
                carrier.Map != shuttleHost.Map ||
                (!alreadyCarried && patient.Map != carrier.Map))
            {
                if (string.IsNullOrEmpty(failReason))
                {
                    failReason = "CT_Shuttle_MedicalBay_CannotCarryPatient".Translate().ToString();
                }

                return false;
            }

            CompShuttleMedicalBayOccupancy occupancy;
            if (!TryGetMedicalBayOccupancy(shuttleHost, out occupancy) ||
                occupancy.ContainsPatient(patient))
            {
                failReason = "CT_Shuttle_MedicalBay_PatientAlreadyHeld".Translate().ToString();
                return false;
            }

            // Current reservations and the Goto toils own route failure. Final
            // admission remains the exact capacity/holder authority.
            return true;
        }

        internal static bool CanAdmitCarriedPatient(
            Pawn carrier,
            Pawn patient,
            Thing shuttleHost,
            out string failReason)
        {
            failReason = null;
            if (!IsCarrierCarryingPatient(carrier, patient))
            {
                failReason = "CT_Shuttle_MedicalBay_CarriedAdmitFailed".Translate().ToString();
                return false;
            }

            return CanCarryPatientToMedicalBay(carrier, patient, shuttleHost, out failReason, carrier);
        }

        internal static bool CanUseDoctorForMedicalBayTending(
            Pawn doctor,
            Thing shuttleHost,
            out string failReason)
        {
            failReason = null;
            if (doctor == null ||
                doctor.Destroyed ||
                doctor.Dead ||
                !doctor.Spawned ||
                doctor.Map == null ||
                doctor.Downed ||
                doctor.MentalState != null)
            {
                failReason = "CT_Shuttle_MedicalBay_DoctorUnavailable".Translate().ToString();
                return false;
            }

            if (!IsPlayerControlledHumanlikeColonist(doctor) ||
                IsIncapableOfDoctoring(doctor) ||
                !CanManipulate(doctor))
            {
                failReason = "CT_Shuttle_MedicalBay_DoctorUnavailable".Translate().ToString();
                return false;
            }

            if (shuttleHost == null ||
                !shuttleHost.Spawned ||
                shuttleHost.Map == null ||
                doctor.Map != shuttleHost.Map)
            {
                failReason = "CT_Shuttle_MedicalBay_NoMedicalBay".Translate().ToString();
                return false;
            }

            if (!doctor.CanReach(shuttleHost, PathEndMode.InteractionCell, Danger.Deadly, false, false, TraverseMode.ByPawn))
            {
                failReason = "CT_Shuttle_MedicalBay_CannotSelfAdmitUnreachable".Translate().ToString();
                return false;
            }

            return true;
        }

        internal static int CountCurrentMedicalBaySelfAdmitUsers(Thing shuttleHost)
        {
            return CountCurrentMedicalBaySelfAdmitUsers(shuttleHost, null);
        }

        internal static int CountCurrentMedicalBaySelfAdmitUsers(Thing shuttleHost, Pawn ignoredPawn)
        {
            return CountCurrentMedicalBayAdmissionUsers(shuttleHost, ignoredPawn);
        }

        internal static int CountCurrentMedicalBayAdmissionUsers(Thing shuttleHost)
        {
            return CountCurrentMedicalBayAdmissionUsers(shuttleHost, null);
        }

        internal static int CountCurrentMedicalBayAdmissionUsers(Thing shuttleHost, Pawn ignoredPawn)
        {
            return CountCurrentMedicalBayAdmissionUsers(shuttleHost, ignoredPawn, null);
        }

        internal static int CountCurrentMedicalBayAdmissionUsers(
            Thing shuttleHost,
            Pawn ignoredPawn,
            Pawn ignoredJobPawn)
        {
            if (shuttleHost == null || !shuttleHost.Spawned || shuttleHost.Map == null)
            {
                return 0;
            }

            int count = 0;
            CompShuttleMedicalBayOccupancy occupancy;
            if (TryGetMedicalBayOccupancy(shuttleHost, out occupancy))
            {
                count += occupancy.PatientCount;
                if (ignoredPawn != null && occupancy.ContainsPatient(ignoredPawn))
                {
                    count--;
                }

                count += occupancy.CountPendingAdmissionReservations(ignoredPawn);
            }

            JobDef enterJobDef = DefDatabase<JobDef>.GetNamedSilentFail(EnterMedicalBayJobDefName);
            JobDef carryJobDef = DefDatabase<JobDef>.GetNamedSilentFail(CarryPatientToMedicalBayJobDefName);

            IReadOnlyList<Pawn> pawns = shuttleHost.Map.mapPawns != null
                ? shuttleHost.Map.mapPawns.AllPawnsSpawned
                : null;
            if (pawns == null)
            {
                return count < 0 ? 0 : count;
            }

            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn == null || pawn == ignoredPawn || pawn == ignoredJobPawn)
                {
                    continue;
                }

                Job job = pawn.CurJob;
                if (job == null)
                {
                    continue;
                }

                if (enterJobDef != null &&
                    job.def == enterJobDef &&
                    TargetReferences(job.GetTarget(TargetIndex.A), shuttleHost))
                {
                    if (occupancy != null &&
                        occupancy.HasAdmissionReservationForPatientID(pawn.thingIDNumber))
                    {
                        continue;
                    }

                    count++;
                }
                else if (carryJobDef != null &&
                    job.def == carryJobDef &&
                    TargetReferences(job.GetTarget(TargetIndex.B), shuttleHost))
                {
                    Pawn carriedPatient = job.GetTarget(TargetIndex.A).Pawn;
                    if (occupancy != null &&
                        carriedPatient != null &&
                        occupancy.HasAdmissionReservationForPatientID(carriedPatient.thingIDNumber))
                    {
                        continue;
                    }

                    count++;
                }
            }

            return count < 0 ? 0 : count;
        }

        internal static bool IsCarrierCarryingPatient(Pawn carrier, Pawn patient)
        {
            return carrier != null &&
                carrier.carryTracker != null &&
                patient != null &&
                carrier.carryTracker.CarriedThing == patient;
        }

        internal static bool PatientRequiresCarriedAdmission(Pawn patient)
        {
            return patient != null && (patient.Downed || !CanMove(patient));
        }

        private static bool CanUseCarrier(Pawn carrier, out string failReason)
        {
            failReason = null;
            if (carrier == null || carrier.Destroyed || carrier.Dead || !carrier.Spawned || carrier.Map == null)
            {
                failReason = "CT_Shuttle_MedicalBay_CarrierUnavailable".Translate().ToString();
                return false;
            }

            if (!IsPlayerControlledHumanlikeColonist(carrier))
            {
                failReason = "CT_Shuttle_MedicalBay_CarrierUnavailable".Translate().ToString();
                return false;
            }

            if (carrier.Downed || carrier.Drafted || carrier.MentalState != null)
            {
                failReason = "CT_Shuttle_MedicalBay_CarrierUnavailable".Translate().ToString();
                return false;
            }

            if (!CanMove(carrier) || !CanManipulate(carrier) || carrier.carryTracker == null)
            {
                failReason = "CT_Shuttle_MedicalBay_CarrierUnavailable".Translate().ToString();
                return false;
            }

            return true;
        }

        private static bool IsPlayerControlledHumanlikeColonist(Pawn pawn)
        {
            return pawn != null &&
                pawn.RaceProps != null &&
                pawn.RaceProps.Humanlike &&
                pawn.Faction == Faction.OfPlayer &&
                pawn.IsColonist;
        }

        private static bool IsIncapableOfDoctoring(Pawn pawn)
        {
            return pawn != null && pawn.WorkTagIsDisabled(WorkTags.Caring);
        }

        private static bool CanUsePatientForCarriedAdmission(
            Pawn carrier,
            Pawn patient,
            Thing shuttleHost,
            out string failReason)
        {
            failReason = null;
            if (patient == null || patient.Destroyed || patient.Dead || patient == carrier)
            {
                failReason = "CT_Shuttle_MedicalBay_InvalidPatient".Translate().ToString();
                return false;
            }

            if (!MedicalBayPatientAccessPolicy.CanReceiveCare(patient, out failReason))
            {
                return false;
            }

            if (!ShuttleOnboardDeviceUsePolicy.AllowsColonistFacilityUse(
                patient,
                shuttleHost,
                out failReason))
            {
                return false;
            }

            if (!PatientRequiresCarriedAdmission(patient))
            {
                failReason = "CT_Shuttle_MedicalBay_PatientNotDowned".Translate().ToString();
                return false;
            }

            return true;
        }

        private static bool CanUsePatientLocationForCarriedAdmission(
            Pawn patient,
            bool carrierAlreadyCarriesPatient,
            out string failReason)
        {
            failReason = null;
            if (!carrierAlreadyCarriesPatient && (!patient.Spawned || patient.Map == null))
            {
                failReason = "CT_Shuttle_MedicalBay_PatientAlreadyHeld".Translate().ToString();
                return false;
            }

            return true;
        }

        private static bool CanUseShuttleForMedicalAdmission(
            Thing shuttleHost,
            out MedicalBayProfile medicalBay,
            out string failReason)
        {
            medicalBay = null;
            failReason = null;
            if (shuttleHost == null ||
                !shuttleHost.Spawned ||
                shuttleHost.Map == null ||
                shuttleHost.Faction != Faction.OfPlayer)
            {
                failReason = "CT_Shuttle_MedicalBay_NoMedicalBay".Translate().ToString();
                return false;
            }

            ShuttleProfile profile;
            if (!TryGetMedicalBayProfile(shuttleHost, out profile, out medicalBay))
            {
                failReason = "CT_Shuttle_MedicalBay_NoMedicalBay".Translate().ToString();
                return false;
            }

            if (!IsMedicalBayPowered(shuttleHost))
            {
                failReason = "CT_Shuttle_MedicalBay_Unpowered".Translate().ToString();
                return false;
            }

            if (medicalBay.MedicalPatientSlots <= 0)
            {
                failReason = "CT_Shuttle_MedicalBay_NoSlots".Translate().ToString();
                return false;
            }

            return true;
        }

        private static bool CanMove(Pawn pawn)
        {
            return pawn != null &&
                pawn.health != null &&
                pawn.health.capacities != null &&
                pawn.health.capacities.CapableOf(PawnCapacityDefOf.Moving);
        }

        private static bool CanManipulate(Pawn pawn)
        {
            return pawn != null &&
                pawn.health != null &&
                pawn.health.capacities != null &&
                pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation);
        }

        private static bool TargetReferences(LocalTargetInfo target, Thing thing)
        {
            return thing != null && target.IsValid && target.Thing == thing;
        }

        private static int Max(int a, int b)
        {
            return a > b ? a : b;
        }
    }
}

using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Medical
{
    internal sealed class MedicalBayTreatmentQueryService
    {
        internal bool TryGetOccupancy(
            ThingWithComps shuttleHost,
            out CompShuttleMedicalBayOccupancy occupancy,
            out string failReason)
        {
            occupancy = null;
            failReason = null;
            if (shuttleHost == null)
            {
                failReason = "CT_Shuttle_MedicalBay_NoMedicalBay".Translate().ToString();
                return false;
            }

            occupancy = shuttleHost.TryGetComp<CompShuttleMedicalBayOccupancy>();
            if (occupancy == null)
            {
                failReason = "CT_Shuttle_MedicalBay_NoMedicalBay".Translate().ToString();
                return false;
            }

            return true;
        }

        internal Pawn FindContainedPatientByThingID(
            CompShuttleMedicalBayOccupancy occupancy,
            int patientThingID)
        {
            if (occupancy == null || patientThingID <= 0)
            {
                return null;
            }

            List<Pawn> patients = occupancy.HeldPatients;
            for (int i = 0; i < patients.Count; i++)
            {
                Pawn patient = patients[i];
                if (patient != null && patient.thingIDNumber == patientThingID)
                {
                    return patient;
                }
            }

            return null;
        }

        internal Pawn FindSpawnedPawnByThingID(Map map, int pawnThingID)
        {
            if (map == null || map.mapPawns == null || pawnThingID <= 0)
            {
                return null;
            }

            IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
            if (pawns == null)
            {
                return null;
            }

            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn != null && pawn.thingIDNumber == pawnThingID)
                {
                    return pawn;
                }
            }

            return null;
        }

        internal int CountUntendedTendableHediffs(
            ThingWithComps shuttleHost,
            int patientThingID,
            MedicalBayTreatmentPreflightService preflightService,
            out string failReason)
        {
            failReason = null;

            CompShuttleMedicalBayOccupancy occupancy;
            if (!this.TryGetOccupancy(shuttleHost, out occupancy, out failReason))
            {
                return 0;
            }

            Pawn patient = this.FindContainedPatientByThingID(occupancy, patientThingID);
            if (!preflightService.CanUseContainedPatientForTreatment(occupancy, patient, out failReason))
            {
                return 0;
            }

            return this.ReadTendState(patient).UntendedTendableCount;
        }

        internal bool PatientNeedsTend(
            ThingWithComps shuttleHost,
            int patientThingID,
            MedicalBayTreatmentPreflightService preflightService,
            out int untendedTendableHediffCount,
            out string failReason)
        {
            untendedTendableHediffCount = this.CountUntendedTendableHediffs(
                shuttleHost,
                patientThingID,
                preflightService,
                out failReason);
            if (!string.IsNullOrEmpty(failReason))
            {
                return false;
            }

            if (untendedTendableHediffCount <= 0)
            {
                failReason = "CT_Shuttle_MedicalBay_NoTendableHediffs".Translate().ToString();
                return false;
            }

            return true;
        }

        internal MedicalBayTendState ReadTendState(Pawn patient)
        {
            MedicalBayTendState state = new MedicalBayTendState();
            if (patient == null ||
                patient.health == null ||
                patient.health.hediffSet == null ||
                patient.health.hediffSet.hediffs == null)
            {
                return state;
            }

            for (int i = 0; i < patient.health.hediffSet.hediffs.Count; i++)
            {
                Hediff hediff = patient.health.hediffSet.hediffs[i];
                if (hediff == null)
                {
                    continue;
                }

                bool tended = this.IsHediffTended(hediff);
                bool tendable = this.IsHediffTendable(hediff);
                if (tendable)
                {
                    state.TendableCount++;
                }

                if (tended)
                {
                    state.TendedCount++;
                }
                else if (tendable)
                {
                    state.UntendedTendableCount++;
                }
            }

            return state;
        }

        internal bool IsHediffTendable(Hediff hediff)
        {
            if (hediff == null)
            {
                return false;
            }

            return hediff.TendableNow(false);
        }

        internal bool PatientHasVanillaTendNeed(Pawn patient)
        {
            return patient != null &&
                patient.health != null &&
                patient.health.HasHediffsNeedingTend(false);
        }

        internal bool IsHediffTended(Hediff hediff)
        {
            HediffComp_TendDuration tendComp = this.GetTendDurationComp(hediff);
            return tendComp != null && tendComp.IsTended;
        }

        internal HediffComp_TendDuration GetTendDurationComp(Hediff hediff)
        {
            HediffWithComps withComps = hediff as HediffWithComps;
            return withComps != null ? withComps.GetComp<HediffComp_TendDuration>() : null;
        }

        internal bool HasAnyDoctorInventoryMedicine(Pawn doctor)
        {
            if (doctor == null ||
                doctor.inventory == null ||
                doctor.inventory.innerContainer == null)
            {
                return false;
            }

            ThingOwner<Thing> inventory = doctor.inventory.innerContainer;
            for (int i = 0; i < inventory.Count; i++)
            {
                if (this.IsUsableMedicine(inventory[i] as Medicine))
                {
                    return true;
                }
            }

            return false;
        }

        internal bool TryFindSingleStackDoctorInventoryMedicine(
            Pawn doctor,
            Pawn patient,
            out Medicine medicine,
            out string failReason)
        {
            medicine = null;
            failReason = null;
            if (doctor == null ||
                doctor.inventory == null ||
                doctor.inventory.innerContainer == null)
            {
                failReason = "Medical Bay inventory medicine tend test failed: doctor has no inventory medicine.";
                return false;
            }

            bool foundMedicineInLargerStack = false;
            bool foundMedicineDisallowedByPolicy = false;
            ThingOwner<Thing> inventory = doctor.inventory.innerContainer;
            for (int i = 0; i < inventory.Count; i++)
            {
                Medicine candidate = inventory[i] as Medicine;
                if (!this.IsUsableMedicine(candidate))
                {
                    continue;
                }

                if (!this.PatientAllowsMedicine(patient, candidate))
                {
                    foundMedicineDisallowedByPolicy = true;
                    continue;
                }

                if (candidate.stackCount == 1)
                {
                    if (medicine == null || this.CompareMedicineForTreatment(candidate, medicine) < 0)
                    {
                        medicine = candidate;
                    }
                    continue;
                }

                if (candidate.stackCount > 1)
                {
                    foundMedicineInLargerStack = true;
                }
            }

            if (medicine != null)
            {
                return true;
            }

            failReason = foundMedicineDisallowedByPolicy
                ? "CT_Shuttle_MedicalBay_NoAllowedDoctorMedicine".Translate().ToString()
                : foundMedicineInLargerStack
                ? "Medical Bay inventory medicine tend test requires a single medicine stack. StackCount > 1 is intentionally not passed to TendUtility until SplitOff(1)+rollback is implemented."
                : "Medical Bay inventory medicine tend test failed: doctor has no inventory medicine.";
            return false;
        }

        internal bool TryFindDoctorInventoryMedicine(
            Pawn doctor,
            Pawn patient,
            out Medicine medicine,
            out string failReason)
        {
            medicine = null;
            failReason = null;
            if (doctor == null ||
                doctor.inventory == null ||
                doctor.inventory.innerContainer == null)
            {
                failReason = "CT_Shuttle_MedicalBay_NoDoctorMedicine".Translate().ToString();
                return false;
            }

            bool foundAnyMedicine = false;
            bool foundDisallowedMedicine = false;
            ThingOwner<Thing> inventory = doctor.inventory.innerContainer;
            for (int i = 0; i < inventory.Count; i++)
            {
                Medicine candidate = inventory[i] as Medicine;
                if (!this.IsUsableMedicine(candidate))
                {
                    continue;
                }

                foundAnyMedicine = true;
                if (!this.PatientAllowsMedicine(patient, candidate))
                {
                    foundDisallowedMedicine = true;
                    continue;
                }

                if (medicine == null || this.CompareMedicineForTreatment(candidate, medicine) < 0)
                {
                    medicine = candidate;
                }
            }

            if (medicine != null)
            {
                return true;
            }

            failReason = foundAnyMedicine && foundDisallowedMedicine
                ? "CT_Shuttle_MedicalBay_NoAllowedDoctorMedicine".Translate().ToString()
                : "CT_Shuttle_MedicalBay_NoDoctorMedicine".Translate().ToString();
            return false;
        }

        internal bool PatientAllowsMedicine(Pawn patient, Medicine medicine)
        {
            if (!this.IsUsableMedicine(medicine))
            {
                return false;
            }

            if (patient == null || patient.playerSettings == null)
            {
                return false;
            }

            return MedicalCareUtility.AllowsMedicine(patient.playerSettings.medCare, medicine.def);
        }

        internal int CompareMedicineForTreatment(Medicine left, Medicine right)
        {
            float leftPotency = this.GetMedicinePotency(left);
            float rightPotency = this.GetMedicinePotency(right);
            int potencyCompare = rightPotency.CompareTo(leftPotency);
            if (potencyCompare != 0)
            {
                return potencyCompare;
            }

            string leftLabel = this.GetMedicineLabel(left);
            string rightLabel = this.GetMedicineLabel(right);
            return string.Compare(leftLabel, rightLabel, StringComparison.Ordinal);
        }

        internal float GetMedicinePotency(Medicine medicine)
        {
            return medicine != null && medicine.def != null && StatDefOf.MedicalPotency != null
                ? medicine.def.GetStatValueAbstract(StatDefOf.MedicalPotency)
                : 0f;
        }

        internal bool IsUsableMedicine(Medicine medicine)
        {
            return medicine != null &&
                !medicine.Destroyed &&
                medicine.def != null &&
                medicine.def.IsMedicine &&
                medicine.stackCount > 0;
        }

        private string GetMedicineLabel(Medicine medicine)
        {
            return medicine != null && medicine.def != null
                ? medicine.def.LabelCap.ToString()
                : "CT_Shuttle_MedicalBay_DoctorInventoryMedicine".Translate().ToString();
        }
    }

    internal struct MedicalBayTendState
    {
        internal int TendableCount;
        internal int UntendedTendableCount;
        internal int TendedCount;
    }
}

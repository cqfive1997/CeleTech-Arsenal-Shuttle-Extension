using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Medical;
using CeleTech.ShuttleExtension.ModularShuttle.Profile.Sections;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using RimWorld;
using Verse;
using Verse.AI;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    public sealed partial class CompShuttleMedicalBayOccupancy
    {
        private void TickHeldPatients()
        {
            if (this.medicalHeldThings == null || this.medicalHeldThings.Count == 0)
            {
                return;
            }

            // ThingWithComps does not automatically tick IThingHolder comps. This preserves
            // normal pawn ticking while contained. Medical comfort is layered separately below.
            this.medicalHeldThings.DoTick();
        }

        private void TickPassiveComfortPatients()
        {
            if (this.medicalHeldThings == null ||
                this.medicalHeldThings.Count == 0)
            {
                return;
            }

            int ticksGame = ShuttleTickUtility.TicksGameOrMinusOne();
            if (ticksGame < 0 ||
                ticksGame % MedicalBayComfortUtility.ComfortTickIntervalTicks != 0)
            {
                return;
            }

            MedicalBayProfile medicalBay;
            if (!this.TryGetMedicalBayProfile(out medicalBay))
            {
                return;
            }

            bool medicalBayPowered = MedicalBayAdmissionValidator.IsMedicalBayPowered(this.parent);
            List<Pawn> patients = this.GetHeldPatientsForReading();
            for (int i = 0; i < patients.Count; i++)
            {
                string inactiveReason;
                MedicalBayComfortUtility.TryApplyPassiveComfortTick(
                    patients[i],
                    medicalBay,
                    medicalBayPowered,
                    out inactiveReason);
            }
        }

        private void AutoDischargeRecoveredPatientsIfPossible()
        {
            if (this.patientRecords == null || this.patientRecords.Count == 0)
            {
                return;
            }

            Map map = this.parent != null ? this.parent.Map : null;
            if (map == null)
            {
                return;
            }

            this.ReconcilePatientRecordsToHeldPawns();
            for (int i = this.patientRecords.Count - 1; i >= 0; i--)
            {
                ShuttleMedicalPatientRecord record = this.patientRecords[i];
                Pawn patient = record != null ? record.Pawn : null;
                if (!this.IsHeldPawn(patient) ||
                    this.IsPatientReservedForTreatment(patient.thingIDNumber) ||
                    this.IsPatientLockedByActiveMedicalProcedure(patient) ||
                    !this.ShouldAutoDischargePatient(patient))
                {
                    continue;
                }

                string failureReason;
                this.TryEjectPatient(patient, map, out failureReason);
            }
        }

        private bool IsPatientLockedByActiveMedicalProcedure(Pawn patient)
        {
            if (patient == null || patient.thingIDNumber <= 0)
            {
                return false;
            }

            ShuttleController controller;
            return MedicalBayAdmissionValidator.TryGetShuttleController(this.parent, out controller) &&
                controller != null &&
                controller.HasActiveMedicalProcedureForPatient(patient.thingIDNumber);
        }

        private bool ShouldAutoDischargePatient(Pawn patient)
        {
            if (patient == null ||
                patient.Destroyed ||
                patient.Dead ||
                patient.Spawned ||
                patient.Downed ||
                patient.health == null ||
                patient.health.hediffSet == null)
            {
                return false;
            }

            // Auto-discharge is intentionally conservative: the bay releases only
            // stable, mobile patients and leaves borderline cases for player control.
            return !this.PatientHasUntendedTendableHediff(patient) &&
                !this.PatientHasBleeding(patient) &&
                !this.PatientHasInfectionLikeHediff(patient) &&
                !this.PatientHasMajorInjury(patient) &&
                this.GetCapacityLevel(patient, PawnCapacityDefOf.Consciousness) >= SafeConsciousnessDischargeThreshold &&
                this.GetCapacityLevel(patient, PawnCapacityDefOf.Moving) >= SafeMovingDischargeThreshold &&
                this.GetPainTotal(patient) < SeverePainDischargeThreshold;
        }

        private void EjectInvalidPatientsIfPossible()
        {
            if (this.patientRecords == null || this.patientRecords.Count == 0)
            {
                return;
            }

            MedicalBayProfile medicalBay;
            bool medicalBayAvailable = this.TryGetMedicalBayProfile(out medicalBay) &&
                medicalBay != null &&
                medicalBay.HasMedicalBay &&
                medicalBay.MedicalPatientSlots > 0;
            Map map = this.parent != null ? this.parent.Map : null;

            for (int i = this.patientRecords.Count - 1; i >= 0; i--)
            {
                ShuttleMedicalPatientRecord record = this.patientRecords[i];
                Pawn patient = record != null ? record.Pawn : null;
                bool invalidPatient = patient == null || patient.Dead || patient.Destroyed || !this.IsHeldPawn(patient);
                if (!invalidPatient && medicalBayAvailable)
                {
                    continue;
                }

                if (patient != null && patient.Destroyed)
                {
                    this.ReleaseAllReservationsForPatient(patient.thingIDNumber);
                    this.medicalHeldThings.Remove(patient);
                    this.patientRecords.RemoveAt(i);
                    continue;
                }

                if (!this.IsHeldPawn(patient))
                {
                    this.ReleaseAllReservationsForPatient(record != null ? record.PawnThingID : -1);
                    this.patientRecords.RemoveAt(i);
                    continue;
                }

                if (map == null)
                {
                    this.LogPatientPreservedWithoutMap(record, !medicalBayAvailable);
                    continue;
                }

                string failureReason;
                this.TryEjectPatient(patient, map, out failureReason);
            }
        }

        private void MaybeReconcilePatientRecords()
        {
            if (!this.ShouldRunInterval(
                ref this.nextPatientRecordReconcileTick,
                PatientRecordReconcileIntervalTicks))
            {
                return;
            }

            this.ReconcilePatientRecordsToHeldPawns();
        }

        private void MaybeCleanupReservations()
        {
            if (!this.ShouldRunInterval(
                ref this.nextReservationCleanupTick,
                ReservationCleanupIntervalTicks))
            {
                return;
            }

            Dictionary<int, Pawn> spawnedPawnsByThingID = this.BuildSpawnedPawnLookup();
            this.ClearStaleAdmissionReservations(spawnedPawnsByThingID);
            this.ClearStaleTreatmentReservations(spawnedPawnsByThingID);
        }

        private void MaybeEjectInvalidPatients()
        {
            if (!this.ShouldRunInterval(
                ref this.nextInvalidPatientEjectTick,
                InvalidPatientEjectIntervalTicks))
            {
                return;
            }

            this.EjectInvalidPatientsIfPossible();
        }

        private void MaybeAutoDischargeRecoveredPatients()
        {
            if (!this.ShouldRunInterval(
                ref this.nextPatientAutoDischargeTick,
                PatientAutoDischargeIntervalTicks))
            {
                return;
            }

            this.AutoDischargeRecoveredPatientsIfPossible();
        }

        private bool ShouldRunInterval(ref int nextTick, int intervalTicks)
        {
            int ticksGame = ShuttleTickUtility.TicksGameOrMinusOne();
            if (ticksGame < 0)
            {
                return true;
            }

            if (nextTick < 0 ||
                ticksGame >= nextTick ||
                ticksGame < nextTick - intervalTicks * 4)
            {
                nextTick = ticksGame + intervalTicks;
                return true;
            }

            return false;
        }

        private void ResetTickIntervals()
        {
            this.nextReservationCleanupTick = -1;
            this.nextPatientRecordReconcileTick = -1;
            this.nextInvalidPatientEjectTick = -1;
            this.nextPatientAutoDischargeTick = -1;
        }
    }
}

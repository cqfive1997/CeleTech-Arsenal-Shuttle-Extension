using System;
using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using CeleTech.ShuttleExtension.ModularShuttle.Medical;
using RimWorld;
using Verse;
using Verse.AI;

namespace CeleTech.ShuttleExtension.ModularShuttle.Prisoners
{
    /// <summary>
    /// Command/job operation boundary for tending prisoners held inside shuttle
    /// Prison Cells. Medicine is drawn only from loaded shuttle cargo; when no
    /// cargo medicine is available, the service falls back to vanilla no-medicine
    /// tending.
    /// </summary>
    internal sealed class ShuttlePrisonerTreatmentService
    {
        private readonly PrisonCellTreatmentValidator validator =
            new PrisonCellTreatmentValidator();
        private readonly ShuttleLoadedCargoMedicineSupplySource medicineSource =
            new ShuttleLoadedCargoMedicineSupplySource();

        internal bool TryAssignTendPrisonerJob(
            ThingWithComps shuttleHost,
            Pawn doctor,
            int prisonerThingIDNumber,
            out string failReason)
        {
            failReason = null;
            if (this.HasActiveCareOrder(
                shuttleHost,
                prisonerThingIDNumber,
                PrisonCellTreatmentValidator.TendPrisonerJobDefName))
            {
                failReason = "CT_Shuttle_PrisonCell_CareAlreadyAssigned".Translate().ToString();
                return false;
            }

            Pawn prisoner = this.FindHeldPrisoner(shuttleHost, prisonerThingIDNumber);
            if (!this.validator.CanAssignTendJob(
                shuttleHost,
                doctor,
                prisoner,
                out failReason))
            {
                return false;
            }

            JobDef jobDef = DefDatabase<JobDef>.GetNamedSilentFail(
                PrisonCellTreatmentValidator.TendPrisonerJobDefName);
            if (jobDef == null || doctor == null || doctor.jobs == null)
            {
                failReason = "CT_Shuttle_PrisonCell_TendFailed".Translate().ToString();
                return false;
            }

            Job job = JobMaker.MakeJob(jobDef, shuttleHost);
            // job.count intentionally stores the held prisoner thingID because
            // the prisoner is inside a holder and is not a spawned job target.
            job.count = prisonerThingIDNumber;
            if (!doctor.jobs.TryTakeOrderedJob(job, JobTag.Misc))
            {
                failReason = "CT_Shuttle_PrisonCell_TendFailed".Translate().ToString();
                return false;
            }

            return true;
        }

        private bool HasActiveCareOrder(
            ThingWithComps shuttleHost,
            int prisonerThingIDNumber,
            string jobDefName)
        {
            JobDef jobDef = DefDatabase<JobDef>.GetNamedSilentFail(jobDefName);
            if (shuttleHost == null ||
                shuttleHost.Map == null ||
                shuttleHost.Map.mapPawns == null ||
                prisonerThingIDNumber <= 0 ||
                jobDef == null)
            {
                return false;
            }

            System.Collections.Generic.IReadOnlyList<Pawn> pawns =
                shuttleHost.Map.mapPawns.AllPawnsSpawned;
            for (int i = 0; pawns != null && i < pawns.Count; i++)
            {
                Job currentJob = pawns[i] != null ? pawns[i].CurJob : null;
                if (currentJob != null &&
                    currentJob.def == jobDef &&
                    currentJob.count == prisonerThingIDNumber &&
                    currentJob.GetTarget(TargetIndex.A).Thing == shuttleHost)
                {
                    return true;
                }
            }

            return false;
        }

        internal bool TryTendHeldPrisoner(
            ThingWithComps shuttleHost,
            Pawn doctor,
            int prisonerThingIDNumber,
            out string failReason)
        {
            failReason = null;
            Pawn prisoner = this.FindHeldPrisoner(shuttleHost, prisonerThingIDNumber);
            if (!this.validator.CanTendHeldPrisoner(
                shuttleHost,
                doctor,
                prisoner,
                out failReason))
            {
                return false;
            }

            TendState before = this.ReadTendState(prisoner);
            Medicine medicine = null;
            IShuttleMedicalSupplyWithdrawal withdrawal = null;
            string medicineFailure;
            bool hasMedicine = this.medicineSource != null &&
                this.medicineSource.TryTakeOneLoadedMedicineForPatient(
                    shuttleHost,
                    prisoner,
                    out medicine,
                    out withdrawal,
                    out medicineFailure,
                    allowMedicineWhenPatientSettingsMissing: true);

            string resultNotice;
            bool success = hasMedicine
                ? this.TryTendWithLoadedCargoMedicine(
                    shuttleHost,
                    doctor,
                    prisoner,
                    before,
                    medicine,
                    withdrawal,
                    out resultNotice,
                    out failReason)
                : this.TryTendNoMedicine(
                    shuttleHost,
                    doctor,
                    prisoner,
                    before,
                    out resultNotice,
                    out failReason);

            if (!success)
            {
                return false;
            }

            CompShuttlePrisonCellOccupancy occupancy;
            if (this.validator.TryGetPrisonCellOccupancy(shuttleHost, out occupancy) &&
                occupancy != null)
            {
                string medicineLabel = hasMedicine
                    ? this.GetMedicineLabel(medicine)
                    : "CT_Shuttle_PrisonCell_TendWithoutMedicine".Translate().ToString();
                occupancy.MarkPrisonerTendedForInternalUse(
                    prisonerThingIDNumber,
                    Find.TickManager != null ? Find.TickManager.TicksGame : -1,
                    medicineLabel);
            }

            Messages.Message(
                "CT_Shuttle_PrisonCell_TendSucceeded"
                    .Translate(!string.IsNullOrEmpty(resultNotice) ? resultNotice : string.Empty)
                    .ToString(),
                MessageTypeDefOf.PositiveEvent,
                false);
            return true;
        }

        private bool TryTendWithLoadedCargoMedicine(
            ThingWithComps shuttleHost,
            Pawn doctor,
            Pawn prisoner,
            TendState before,
            Medicine medicine,
            IShuttleMedicalSupplyWithdrawal withdrawal,
            out string resultNotice,
            out string failReason)
        {
            resultNotice = null;
            failReason = null;
            if (withdrawal == null || !this.IsUsableMedicine(medicine))
            {
                failReason = "CT_Shuttle_PrisonCell_NoMedicine".Translate().ToString();
                return false;
            }

            try
            {
                TendUtility.DoTend(doctor, prisoner, medicine);
            }
            catch (Exception exception)
            {
                string rollbackNotice;
                withdrawal.RollbackOrDrop(doctor, shuttleHost, out rollbackNotice);
                failReason = "CT_Shuttle_PrisonCell_TendFailed"
                    .Translate(exception.GetType().Name + ": " + exception.Message)
                    .ToString();
                failReason = this.AppendNotice(failReason, rollbackNotice);
                if (Prefs.DevMode)
                {
                    Log.Warning("[CeleTech Shuttle] " + failReason);
                }

                return false;
            }

            if (!this.ValidateHeldPrisonerAfterTend(shuttleHost, prisoner, out failReason))
            {
                string rollbackNotice;
                withdrawal.RollbackOrDrop(doctor, shuttleHost, out rollbackNotice);
                failReason = this.AppendNotice(failReason, rollbackNotice);
                return false;
            }

            TendState after = this.ReadTendState(prisoner);
            if (after.TendedCount <= before.TendedCount &&
                after.UntendedTendableCount >= before.UntendedTendableCount)
            {
                string rollbackNotice;
                withdrawal.RollbackOrDrop(doctor, shuttleHost, out rollbackNotice);
                failReason = "CT_Shuttle_PrisonCell_TendFailed"
                    .Translate("CT_Shuttle_PrisonCell_NoTendableInjury".Translate())
                    .ToString();
                failReason = this.AppendNotice(failReason, rollbackNotice);
                return false;
            }

            if (medicine != null &&
                !medicine.Destroyed &&
                medicine.stackCount > 0)
            {
                if (Prefs.DevMode)
                {
                    Log.Warning("[CeleTech Shuttle] PrisonCell loaded cargo medicine tend succeeded but the withdrawn medicine remained alive; consuming the withdrawn medicine to avoid a free medicine tend. medicine=" +
                        this.GetMedicineLabel(medicine));
                }

                medicine.Destroy(DestroyMode.Vanish);
            }

            withdrawal.CommitConsumed();
            resultNotice = "CT_Shuttle_PrisonCell_MedicineUsed".Translate(this.GetMedicineLabel(medicine)).ToString();
            return true;
        }

        private bool TryTendNoMedicine(
            ThingWithComps shuttleHost,
            Pawn doctor,
            Pawn prisoner,
            TendState before,
            out string resultNotice,
            out string failReason)
        {
            resultNotice = null;
            failReason = null;
            try
            {
                TendUtility.DoTend(doctor, prisoner, null);
            }
            catch (Exception exception)
            {
                failReason = "CT_Shuttle_PrisonCell_TendFailed"
                    .Translate(exception.GetType().Name + ": " + exception.Message)
                    .ToString();
                if (Prefs.DevMode)
                {
                    Log.Warning("[CeleTech Shuttle] " + failReason);
                }

                return false;
            }

            if (!this.ValidateHeldPrisonerAfterTend(shuttleHost, prisoner, out failReason))
            {
                return false;
            }

            TendState after = this.ReadTendState(prisoner);
            if (after.TendedCount <= before.TendedCount &&
                after.UntendedTendableCount >= before.UntendedTendableCount)
            {
                failReason = "CT_Shuttle_PrisonCell_TendFailed"
                    .Translate("CT_Shuttle_PrisonCell_NoTendableInjury".Translate())
                    .ToString();
                return false;
            }

            resultNotice = "CT_Shuttle_PrisonCell_TendWithoutMedicine".Translate().ToString();
            return true;
        }

        private bool ValidateHeldPrisonerAfterTend(
            ThingWithComps shuttleHost,
            Pawn prisoner,
            out string failReason)
        {
            failReason = null;
            CompShuttlePrisonCellOccupancy occupancy;
            if (!this.validator.TryGetPrisonCellOccupancy(shuttleHost, out occupancy) ||
                occupancy == null ||
                prisoner == null ||
                prisoner.Destroyed ||
                prisoner.Dead ||
                prisoner.Spawned ||
                !occupancy.ContainsPrisoner(prisoner))
            {
                failReason = "CT_Shuttle_PrisonCell_InvalidPrisoner".Translate().ToString();
                return false;
            }

            return true;
        }

        private Pawn FindHeldPrisoner(ThingWithComps shuttleHost, int prisonerThingIDNumber)
        {
            CompShuttlePrisonCellOccupancy occupancy = shuttleHost != null
                ? shuttleHost.TryGetComp<CompShuttlePrisonCellOccupancy>()
                : null;
            return occupancy != null
                ? occupancy.FindHeldPrisonerByThingID(prisonerThingIDNumber)
                : null;
        }

        private TendState ReadTendState(Pawn prisoner)
        {
            TendState state = new TendState();
            if (prisoner == null ||
                prisoner.health == null ||
                prisoner.health.hediffSet == null ||
                prisoner.health.hediffSet.hediffs == null)
            {
                return state;
            }

            for (int i = 0; i < prisoner.health.hediffSet.hediffs.Count; i++)
            {
                Hediff hediff = prisoner.health.hediffSet.hediffs[i];
                if (hediff == null)
                {
                    continue;
                }

                bool tended = this.IsHediffTended(hediff);
                bool tendable = hediff.TendableNow(false);
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

        private bool IsHediffTended(Hediff hediff)
        {
            HediffWithComps withComps = hediff as HediffWithComps;
            HediffComp_TendDuration tendComp = withComps != null
                ? withComps.GetComp<HediffComp_TendDuration>()
                : null;
            return tendComp != null && tendComp.IsTended;
        }

        private bool IsUsableMedicine(Medicine medicine)
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
                : "CT_Shuttle_PrisonCell_NoMedicine".Translate().ToString();
        }

        private string AppendNotice(string message, string notice)
        {
            if (string.IsNullOrEmpty(notice))
            {
                return message;
            }

            if (string.IsNullOrEmpty(message))
            {
                return notice;
            }

            return message + " " + notice;
        }

        private struct TendState
        {
            internal int TendableCount;
            internal int UntendedTendableCount;
            internal int TendedCount;
        }
    }
}

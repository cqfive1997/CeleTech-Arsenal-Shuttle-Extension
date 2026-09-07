using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Prisoners;
using CeleTech.ShuttleExtension.ModularShuttle.Profile.Sections;
using Verse;
using Verse.AI;

namespace CeleTech.ShuttleExtension.ModularShuttle.AI.PrisonCell
{
    public sealed class JobDriver_CarryPrisonerToShuttleCell : JobDriver
    {
        private readonly PrisonCellAdmissionValidator validator =
            new PrisonCellAdmissionValidator();
        private readonly ShuttlePrisonerOperationService operationService =
            new ShuttlePrisonerOperationService();
        private bool admittedToPrisonCell;
        private bool preparedGuestStatusChanged;

        private Pawn Prisoner
        {
            get { return this.job != null ? this.job.GetTarget(TargetIndex.A).Pawn : null; }
        }

        private Thing ShuttleHost
        {
            get { return this.job != null ? this.job.GetTarget(TargetIndex.B).Thing : null; }
        }

        public override void Notify_Starting()
        {
            base.Notify_Starting();
            this.AddFinishAction(this.TryDropCarriedPrisonerOnFailedExit);
            this.AddFinishAction(this.WarnIfPreparedPrisonerWasNotAdmitted);
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            string failReason;
            if (!this.validator.CanAssignCarryToPrisonCell(
                this.ShuttleHost as ThingWithComps,
                this.pawn,
                this.Prisoner,
                out failReason,
                this.pawn))
            {
                return false;
            }

            PrisonCellProfile prisonCell;
            if (!this.TryGetCurrentPrisonCell(out prisonCell))
            {
                return false;
            }

            return this.pawn.Reserve(this.Prisoner, this.job, 1, -1, null, errorOnFailed, false) &&
                this.pawn.Reserve(this.ShuttleHost, this.job, Max(1, prisonCell.PrisonerSlots), 1, null, errorOnFailed, false);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.B);
            this.FailOn(() => !this.CanStillCarryToPrisoner());

            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch)
                .FailOnDespawnedNullOrForbidden(TargetIndex.A)
                .FailOnDespawnedNullOrForbidden(TargetIndex.B);

            yield return this.MakePreparePrisonerForCarryToil();

            yield return Toils_Haul.StartCarryThing(TargetIndex.A);

            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.InteractionCell)
                .FailOnDespawnedNullOrForbidden(TargetIndex.B)
                .FailOn(() => !this.CanStillAdmitCarriedPrisoner());

            yield return this.MakeAdmitCarriedPrisonerToil();
        }

        private Toil MakeAdmitCarriedPrisonerToil()
        {
            Toil toil = ToilMaker.MakeToil("AdmitCarriedPrisonerToShuttleCell");
            toil.initAction = delegate
            {
                ThingWithComps shuttleWithComps = this.ShuttleHost as ThingWithComps;
                string failReason;
                if (shuttleWithComps == null ||
                    !this.operationService.TryAdmitCarriedPrisoner(
                        shuttleWithComps,
                        this.pawn,
                        this.Prisoner,
                        out failReason))
                {
                    this.EndJobWith(JobCondition.Incompletable);
                    return;
                }

                this.admittedToPrisonCell = true;
            };
            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            return toil;
        }

        private Toil MakePreparePrisonerForCarryToil()
        {
            Toil toil = ToilMaker.MakeToil("PreparePrisonerForShuttleCellCarry");
            toil.initAction = delegate
            {
                ThingWithComps shuttleWithComps = this.ShuttleHost as ThingWithComps;
                bool changedGuestStatus;
                string failReason;
                if (shuttleWithComps == null ||
                    !this.operationService.TryPreparePrisonerForCarryAdmission(
                        shuttleWithComps,
                        this.pawn,
                        this.Prisoner,
                        out changedGuestStatus,
                        out failReason))
                {
                    this.EndJobWith(JobCondition.Incompletable);
                    return;
                }

                this.preparedGuestStatusChanged =
                    this.preparedGuestStatusChanged || changedGuestStatus;
            };
            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            return toil;
        }

        private bool CanStillCarryToPrisoner()
        {
            string failReason;
            return this.validator.CanContinueCarryOrder(
                this.ShuttleHost as ThingWithComps,
                this.pawn,
                this.Prisoner,
                out failReason);
        }

        private bool CanStillAdmitCarriedPrisoner()
        {
            string failReason;
            return this.validator.CanContinueCarryOrder(
                this.ShuttleHost as ThingWithComps,
                this.pawn,
                this.Prisoner,
                out failReason);
        }

        private bool TryGetCurrentPrisonCell(out PrisonCellProfile prisonCell)
        {
            prisonCell = null;
            ThingWithComps shuttleHost = this.ShuttleHost as ThingWithComps;
            if (shuttleHost == null)
            {
                return false;
            }

            CeleTech.ShuttleExtension.ModularShuttle.Core.CompModularShuttleCore core =
                shuttleHost.TryGetComp<CeleTech.ShuttleExtension.ModularShuttle.Core.CompModularShuttleCore>();
            CeleTech.ShuttleExtension.ModularShuttle.Core.ShuttleProfile profile =
                core != null && core.Controller != null
                    ? core.Controller.GetProfileForRead()
                    : null;
            prisonCell = profile != null ? profile.PrisonCell : null;
            return prisonCell != null && prisonCell.HasPrisonCell && prisonCell.PrisonerSlots > 0;
        }

        private void TryDropCarriedPrisonerOnFailedExit(JobCondition condition)
        {
            Pawn prisoner = this.Prisoner;
            if (this.admittedToPrisonCell ||
                !this.validator.IsCarrierCarryingPrisoner(this.pawn, prisoner))
            {
                return;
            }

            Map map = this.pawn != null ? this.pawn.Map : null;
            if (map == null)
            {
                if (Prefs.DevMode)
                {
                    Log.Warning("[CeleTech Shuttle] Could not drop carried Prison Cell prisoner during job cleanup because carrier has no map. Prisoner remains in carryTracker. condition=" + condition);
                }

                return;
            }

            Thing resultingThing;
            if (!this.pawn.carryTracker.TryDropCarriedThing(
                this.GetCleanupDropCell(map),
                ThingPlaceMode.Near,
                out resultingThing) &&
                Prefs.DevMode)
            {
                Log.Warning("[CeleTech Shuttle] Failed to drop carried Prison Cell prisoner during job cleanup. Prisoner remains in carryTracker. condition=" + condition);
            }
        }

        private void WarnIfPreparedPrisonerWasNotAdmitted(JobCondition condition)
        {
            if (!this.preparedGuestStatusChanged || this.admittedToPrisonCell)
            {
                return;
            }

            Log.Warning(
                "[CeleTech Shuttle] Prisoner guest status was prepared for shuttle Prison Cell admission, " +
                "but the carry job ended before holder admission. This is an accepted P3 recovery limitation; " +
                "the pawn remains a player prisoner for normal recovery. prisoner=" + this.Prisoner +
                ", carrier=" + this.pawn +
                ", shuttle=" + this.ShuttleHost +
                ", condition=" + condition);
        }

        private IntVec3 GetCleanupDropCell(Map map)
        {
            Thing shuttleHost = this.ShuttleHost;
            if (shuttleHost != null)
            {
                IntVec3 interactionCell = shuttleHost.InteractionCell;
                if (interactionCell.IsValid && interactionCell.InBounds(map))
                {
                    return interactionCell;
                }
            }

            return this.pawn != null && this.pawn.Position.IsValid
                ? this.pawn.Position
                : IntVec3.Invalid;
        }

        private static int Max(int a, int b)
        {
            return a > b ? a : b;
        }
    }
}

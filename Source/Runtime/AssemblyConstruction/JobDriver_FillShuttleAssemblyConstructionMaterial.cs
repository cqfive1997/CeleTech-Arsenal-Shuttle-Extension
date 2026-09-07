using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.AssemblyConstruction
{
    public sealed class JobDriver_FillShuttleAssemblyConstructionMaterial : JobDriver
    {
        private Thing Material
        {
            get
            {
                return this.job != null ? this.job.GetTarget(TargetIndex.A).Thing : null;
            }
        }

        private Thing ShuttleHost
        {
            get
            {
                return this.job != null ? this.job.GetTarget(TargetIndex.B).Thing : null;
            }
        }

        public override void Notify_Starting()
        {
            base.Notify_Starting();
            this.AddFinishAction(this.TryDropCarriedMaterialOnFailedExit);
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (!ShuttleAssemblyConstructionHaulUtility.CanFillWithMaterial(
                this.pawn,
                this.ShuttleHost,
                this.Material))
            {
                return false;
            }

            return ShuttleAssemblyConstructionHaulUtility.TryReserveForFillJob(
                this.pawn,
                this.job,
                this.Material,
                this.ShuttleHost,
                errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.B);
            this.FailOn(() => !this.CanStillFillTargetMaterial());

            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch)
                .FailOnDespawnedNullOrForbidden(TargetIndex.A)
                .FailOnDespawnedNullOrForbidden(TargetIndex.B)
                .FailOn(() => !this.CanStillFillTargetMaterial());

            yield return Toils_Haul.StartCarryThing(TargetIndex.A);

            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.InteractionCell)
                .FailOnDespawnedNullOrForbidden(TargetIndex.B)
                .FailOn(() => !this.CanStillFillCarriedMaterial());

            yield return this.MakeStageMaterialToil();
        }

        private Toil MakeStageMaterialToil()
        {
            Toil toil = ToilMaker.MakeToil("FillShuttleAssemblyConstructionMaterial");
            toil.initAction = delegate
            {
                string failureReason;
                if (!ShuttleAssemblyConstructionHaulUtility.TryStageCarriedMaterial(
                    this.pawn,
                    this.ShuttleHost,
                    out failureReason))
                {
                    this.EndJobWith(JobCondition.Incompletable);
                    return;
                }
            };
            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            return toil;
        }

        private bool CanStillFillTargetMaterial()
        {
            return ShuttleAssemblyConstructionHaulUtility.CanFillWithMaterial(
                this.pawn,
                this.ShuttleHost,
                this.Material);
        }

        private bool CanStillFillCarriedMaterial()
        {
            Thing carried = this.pawn != null && this.pawn.carryTracker != null
                ? this.pawn.carryTracker.CarriedThing
                : null;
            return ShuttleAssemblyConstructionHaulUtility.CanFillWithMaterial(
                this.pawn,
                this.ShuttleHost,
                carried);
        }

        private void TryDropCarriedMaterialOnFailedExit(JobCondition condition)
        {
            if (condition == JobCondition.Succeeded ||
                this.pawn == null ||
                this.pawn.carryTracker == null ||
                this.pawn.carryTracker.CarriedThing == null)
            {
                return;
            }

            Thing dropped;
            this.pawn.carryTracker.TryDropCarriedThing(
                this.ShuttleHost != null ? this.ShuttleHost.Position : this.pawn.Position,
                ThingPlaceMode.Near,
                out dropped);
        }
    }
}

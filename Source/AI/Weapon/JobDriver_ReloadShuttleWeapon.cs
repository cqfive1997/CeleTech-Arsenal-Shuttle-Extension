using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.AssemblyConstruction;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon;
using RimWorld;
using Verse;
using Verse.AI;

namespace CeleTech.ShuttleExtension.ModularShuttle.AI.Weapon
{
    public sealed class JobDriver_ReloadShuttleWeapon : JobDriver
    {
        private string moduleInstanceID;
        private int workTicks;

        private Thing ShuttleHost
        {
            get { return this.job != null ? this.job.GetTarget(TargetIndex.A).Thing : null; }
        }

        private Thing AmmoSource
        {
            get { return this.job != null ? this.job.GetTarget(TargetIndex.B).Thing : null; }
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            Thing shuttleHost = this.ShuttleHost;
            Thing ammoSource = this.AmmoSource;
            if (shuttleHost == null ||
                ammoSource == null ||
                this.job == null ||
                this.job.count <= 0 ||
                !this.pawn.Reserve(
                    shuttleHost,
                    this.job,
                    ShuttleWeaponReloadWorkUtility.MaxReloadersPerShuttle,
                    1,
                    null,
                    errorOnFailed,
                    false))
            {
                return false;
            }

            return this.pawn.Reserve(
                ammoSource,
                this.job,
                1,
                this.job.count,
                null,
                errorOnFailed,
                false);
        }

        public override void Notify_Starting()
        {
            base.Notify_Starting();
            this.AddFinishAction(this.ReleaseManualReloadReservationOnExit);
            this.AddFinishAction(this.DropCarriedAmmoOnExit);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.FailOn(() => !this.IsClaimStillActive());

            yield return this.MakeClaimReloadToil();
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch)
                .FailOnDespawnedNullOrForbidden(TargetIndex.B);
            yield return Toils_Haul.StartCarryThing(TargetIndex.B);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell)
                .FailOnDestroyedOrNull(TargetIndex.A);
            yield return this.MakeReloadWorkToil();
            yield return this.MakeCompleteReloadToil();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.moduleInstanceID, "moduleInstanceID");
            Scribe_Values.Look(ref this.workTicks, "workTicks", 0);
        }

        private void ReleaseManualReloadReservationOnExit(JobCondition condition)
        {
            if (condition == JobCondition.Succeeded)
            {
                return;
            }

            ShuttleController controller;
            if (ShuttleWeaponReloadWorkUtility.TryGetController(this.ShuttleHost, out controller))
            {
                controller.ReleaseManualWeaponReloadJob(
                    this.moduleInstanceID,
                    this.pawn,
                    this.job != null ? this.job.loadID : -1);
            }
        }

        private Toil MakeClaimReloadToil()
        {
            Toil toil = ToilMaker.MakeToil("ClaimShuttleWeaponReload");
            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            toil.initAction = delegate
            {
                ShuttleController controller;
                string reason;
                Thing plannedAmmo = this.AmmoSource;
                ShuttleWeaponManualReloadPlan plan;
                if (!ShuttleWeaponReloadWorkUtility.TryGetController(this.ShuttleHost, out controller) ||
                    plannedAmmo == null ||
                    plannedAmmo.Destroyed ||
                    plannedAmmo.def == null ||
                    !controller.TryClaimManualWeaponReloadJob(
                        this.pawn,
                        this.job != null ? this.job.loadID : -1,
                        plannedAmmo.def,
                        out plan,
                        out reason))
                {
                    this.EndJobWith(JobCondition.Incompletable);
                    return;
                }

                this.moduleInstanceID = plan.ModuleInstanceID;
                this.workTicks = plan.WorkTicks;
            };
            return toil;
        }

        private Toil MakeReloadWorkToil()
        {
            Toil toil = ToilMaker.MakeToil("ReloadShuttleWeapon");
            toil.defaultCompleteMode = ToilCompleteMode.Delay;
            toil.defaultDuration = this.workTicks > 0 ? this.workTicks : 60;
            toil.initAction = delegate
            {
                int duration = this.workTicks > 0 ? this.workTicks : 60;
                toil.defaultDuration = duration;
                this.ticksLeftThisToil = duration;
            };
            toil.handlingFacing = true;
            toil.activeSkill = () => SkillDefOf.Crafting;
            toil.tickIntervalAction = delegate(int delta)
            {
                if (this.pawn != null &&
                    this.pawn.rotationTracker != null &&
                    this.ShuttleHost != null)
                {
                    this.pawn.rotationTracker.FaceTarget(this.ShuttleHost);
                }

                ShuttleAssemblyWorkEffectUtility.TryPlayWorkTickEffect(
                    this.ShuttleHost as ThingWithComps,
                    this.pawn,
                    ShuttleAssemblyWorkEffectKind.Install,
                    Find.TickManager != null ? Find.TickManager.TicksGame : -1,
                    60);
            };
            return toil.WithProgressBarToilDelay(TargetIndex.A)
                .FailOnDestroyedOrNull(TargetIndex.A);
        }

        private bool IsClaimStillActive()
        {
            if (string.IsNullOrEmpty(this.moduleInstanceID))
            {
                return true;
            }

            ShuttleController controller;
            return ShuttleWeaponReloadWorkUtility.TryGetController(this.ShuttleHost, out controller) &&
                controller.IsManualWeaponReloadJobActive(
                    this.moduleInstanceID,
                    this.pawn,
                    this.job != null ? this.job.loadID : -1);
        }

        private Toil MakeCompleteReloadToil()
        {
            Toil toil = ToilMaker.MakeToil("CompleteShuttleWeaponReload");
            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            toil.initAction = delegate
            {
                ShuttleController controller;
                string reason;
                if (!ShuttleWeaponReloadWorkUtility.TryGetController(this.ShuttleHost, out controller) ||
                    !controller.TryCompleteManualWeaponReloadJob(
                        this.moduleInstanceID,
                        this.pawn,
                        this.job != null ? this.job.loadID : -1,
                        out reason))
                {
                    this.EndJobWith(JobCondition.Incompletable);
                }
            };
            return toil;
        }

        private void DropCarriedAmmoOnExit(JobCondition condition)
        {
            if (this.pawn == null ||
                this.pawn.carryTracker == null ||
                this.pawn.carryTracker.CarriedThing == null)
            {
                return;
            }

            Thing dropped;
            this.pawn.carryTracker.TryDropCarriedThing(
                this.pawn.Position,
                ThingPlaceMode.Near,
                out dropped);
        }
    }
}
